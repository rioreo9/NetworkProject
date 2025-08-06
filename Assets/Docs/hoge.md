# Photon Fusion の Runner について

## `Runner`とは

`Runner`はPhoton Fusionの`NetworkRunner`のことで、ネットワークゲームの中心的な管理オブジェクトです。

### 主な役割

1. **ネットワーク同期の管理**
   - 全クライアント間でのゲーム状態の同期
   - ネットワークオブジェクトの生成・削除管理

2. **時間管理**
   - `Runner.DeltaTime` - ネットワーク同期されたフレーム時間
   - `TickTimer.CreateFromSeconds(Runner, time)` - サーバー基準の正確なタイマー

3. **オブジェクト管理**
   - `Runner.Spawn()` - ネットワークオブジェクトの生成
   - `Runner.Despawn()` - ネットワークオブジェクトの削除

4. **ラグ補償機能**
   - `Runner.LagCompensation.Raycast()` - 高精度な当たり判定

### `BulletMove.cs`での使用例

```csharp
life = TickTimer.CreateFromSeconds(Runner, lifeTime);
```

ここでは、弾丸の寿命タイマーをサーバー時間ベースで作成するために`Runner`を使用しています。

- **`Runner`** = その`NetworkBehaviour`が属する`NetworkRunner`インスタンス
- **目的** = 全クライアントで同じタイミングで弾丸が消滅するよう、ネットワーク同期された時間管理を行う

## P2Pホスト型ゲームでの`Runner`の仕組み

NAT使用のP2Pホスト型ゲームでは、**各クライアント（ホストも含む）にそれぞれ独自の`Runner`が存在**します。

### 1. **各プレイヤーが独自のRunnerを持つ**
```
ホストプレイヤー：NetworkRunner (ホスト)
クライアント1： NetworkRunner (クライアント)
クライアント2： NetworkRunner (クライアント)
```

### 2. **ホストのRunnerが特別な役割を持つ**
- **サーバー権限**: `runner.IsServer == true`
- **ゲーム状態の決定権**: オブジェクトのスポーン・削除
- **権威的な判定**: 当たり判定、ダメージ計算など

### 3. **クライアントのRunnerの役割**
- **入力送信**: プレイヤーの操作をホストに送信
- **状態受信**: ホストからのゲーム状態を受信・表示
- **予測処理**: ラグを軽減するための予測計算

### コード例での確認

```csharp
// GameLauncher.cs より抜粋
if (runner.GameMode == GameMode.Shared && player == runner.LocalPlayer || runner.IsServer)
{
    // 自分自身のアバターをスポーンする
    var spawnedObject = runner.Spawn(_player, spawnPosition, Quaternion.identity, player);
    
    // プレイヤー（PlayerRef）とアバター（spawnedObject）を関連付ける
    runner.SetPlayerObject(player, spawnedObject);
}

void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
{
    if (!runner.IsServer) { return; } // サーバー権限のチェック
    // 退出したプレイヤーのアバターを破棄する
    if (runner.TryGetPlayerObject(player, out var avatar))
    {
        runner.Despawn(avatar);
    }
}
```

### 重要なポイント

**ホスト ≠ Runner** です。

- **ホスト** = サーバー権限を持つプレイヤー（の端末）
- **Runner** = 各プレイヤーの端末で動作するPhoton Fusionのネットワーク管理オブジェクト

P2Pゲームでは、ホストプレイヤーの`Runner`が**特別な権限**を持つだけで、全員が独自の`Runner`を持ってネットワーク処理を行います。

このように、`Runner`はPhoton Fusionでマルチプレイヤーゲームのネットワーク処理を統括する重要なコンポーネントです。