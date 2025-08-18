Unityプロジェクト内でPhoton Fusionの使い方を把握するため、`Assets/Scripts`配下のC#スクリプト一覧と、Fusion関連の使用箇所（`using Fusion`, `NetworkBehaviour`, `NetworkRunner` など）を一括で洗い出します。続けて、Fusionの初期化やRPCの使われ方をコード全体からセマンティック検索します。
Ran tool
Ran tool
Ran tool
Search files...
Search files...

複数の検索でFusionの使い方（[Networked], [Rpc], FixedUpdateNetwork, Runner.Spawn/Despawn, 権限/入力処理）を一気に洗い出します。
Ran tool
Ran tool
Ran tool
Ran tool
Ran tool

今からFusionの初期化・Spawn/Despawn・[Networked]/[Rpc]/FixedUpdateNetwork・入力処理の実装状況を確認するため、主要スクリプトをまとめて開きます。
Ran tool
Ran tool
Ran tool
Ran tool
Ran tool

今見つけたFusion関連の主要スクリプトの実装を精査します。引き続き他のRPC使用箇所も読み込みます。
Ran tool
Ran tool
Ran tool
Ran tool

Ran tool
Ran tool
Ran tool
Ran tool
Ran tool

主要な残りのFusion関連スクリプトをまとめて開いて評価を続けます。
Ran tool
Ran tool
Ran tool
Ran tool
Ran tool

Ran tool

### 全体評価
- **Fusionの基本パターンは概ね妥当**: `NetworkRunner` の起動、`INetworkRunnerCallbacks` 実装、`[Networked]`/`[Rpc]`/`FixedUpdateNetwork`/`Runner.Spawn/Despawn` の利用など大枠は正しいです。
- **権限(Authority)の扱いはおおむね良好**: 入力はサーバー側で処理、クライアントはRPCで要求という方針が統一されています。
- **Object Pooling の下準備あり**: `PoolObjectProvider` が実装されていますが、Runnerに接続されておらず未使用です。

### 優先して直すべき点（重要度: 高）
- **Object Provider が未適用（プール未使用）**
  - `PoolObjectProvider` を `StartGame` 時の `StartGameArgs.ObjectProvider` に渡す、またはRunnerにアタッチして使用してください。現状だとプーリング処理は一切使われていません。
  - 該当箇所:
```62:100:Scripts/NetWork/GameLauncher.cs
async void StartGame(GameMode mode)
{
    //NetworkRunnerを生成する
    _runner = Instantiate(_networkRunnerPrefab);

    _runner.AddCallbacks(this);

    _runner.ProvideInput = true;

    Debug.Log($"サーバー起動: セッション名 = {_sesionName}, モード = {mode}");

    // ネットワーク用のシーンの設定
    var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
    var sceneInfo = new NetworkSceneInfo();
    if (scene.IsValid)
    {
        sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
    }

    // セッションの参加
    StartGameResult result = await _runner.StartGame(new StartGameArgs()
    {
        GameMode = mode,
        SessionName = _sesionName,
        Scene = sceneInfo,
        SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
    });
```
  - 例（修正案）:
    - `var provider = _runner.gameObject.AddComponent<PoolObjectProvider>();`
    - `await _runner.StartGame(new StartGameArgs { ..., ObjectProvider = provider });`
    - かつ `PoolObjectProvider` のホワイトリストに弾や敵の `NetworkObject` を登録。

- **弾の回転・権限・ラグ補償の使い方**
  - 弾生成時の回転を Spawn 引数で配布する。`Quaternion.identity` でSpawnしてからサーバーのみで回転を変えても、クライアントに初期回転が伝わりません。
  - 弾のLagCompensation Raycast距離が不適切。`Speed` は「速度(単位/秒)」なので、Ray長は `Speed * Runner.DeltaTime` が妥当です。
  - `Runner.Despawn(Object)` は権限側のみで実行。
  - Raycast の `player` 引数は発射元の `PlayerRef` を渡す（弾の `InputAuthority` を所有者に設定すればOK）。
  - 問題箇所:
```79:99:Scripts/Bullet/BulletMove.cs
public override void FixedUpdateNetwork()
{
    // 寿命チェック：期限切れの場合は弾丸を削除
    if (life.Expired(Runner))
    {

        // ネットワークオブジェクトとして適切に削除
        Runner.Despawn(Object);
    }
    else
    {
        // 弾丸移動処理
        transform.position += Speed * transform.forward * Runner.DeltaTime;

        // 当たり判定処理を実行
        HitAction();
    }
}
```
```104:118:Scripts/Bullet/BulletMove.cs
private void HitAction()
{
    bool isHit = Runner.LagCompensation.Raycast
          (transform.position,
          transform.forward,
          Speed,
          Object.InputAuthority,
          out LagCompensatedHit hit,
          _targetLayerMask);

    if (isHit)
    {
        CheckHitTarget(hit);
    }
}
```
  - 例（修正案の要点）
    - Spawn 時に回転と所有者を渡す:
      ```csharp
      Runner.Spawn(_bulletPrefab, pos, Quaternion.LookRotation(dir), Object.InputAuthority);
      ```
    - Ray 長を `Speed * Runner.DeltaTime` にする
    - `FixedUpdateNetwork` と当たり判定は `if (!Object.HasStateAuthority) return;` でサーバーのみ実行
    - Despawn もサーバーのみで

- **プレイヤーの弾生成時の回転とInputAuthority**
  - `Quaternion.identity` → `Quaternion.LookRotation(_armTransform.forward)`
  - `Runner.Spawn` に `Object.InputAuthority` を渡す
  - 該当箇所:
```69:85:Scripts/Character/Pys/PlayerMovement.cs
private void ShotBullet()
{
    if (!Object.HasStateAuthority)
    {
        // クライアントはサーバーに弾丸発射を要求
        RPC_RequestShotBullet();
        return;
    }

    // 弾丸の発射位置をアームの先端に設定
    Vector3 bulletSpawnPosition = _armTransform.position + _armTransform.forward * 0.5f;
    // 弾丸を生成
    BulletMove bullet = Runner.Spawn(_bulletPrefab, bulletSpawnPosition, Quaternion.identity);

    // 弾丸の初期化
    bullet.Init(_armTransform.forward);
}
```

- **弾の初期化と[Networked]プロパティの書き込みタイミング**
  - `OnEnable/Start` で `[Networked]` プロパティ（`lifeTime`, `Speed`）を書き換える現在の実装は、非権限側でも走るため危険です。これらは「全クライアントで同値の定数」で良いなら `[SerializeField]` のままにし、ネットワーク化を外してください。タイマーだけ `[Networked] TickTimer life` があれば十分です。

- **Wakameの弾生成も回転をSpawnに渡す**
```60:66:Scripts/Character/Enemy/Types/EnemyWakame.cs
public override void AttackTarget()
{
    Vector3 targetDirection = (_targetBattleship.position - transform.position).normalized;

    BulletMove bullet = Runner.Spawn(_bulletPrefab, transform.position + targetDirection * 2, Quaternion.identity);
    bullet.Init(targetDirection);
}
```
  - こちらも `Quaternion.LookRotation(targetDirection)` を渡す。`Init` はタイマー開始のみで回転操作は不要に。

### 中〜小改善
- **初期GameStateの設定はRPCではなく権限側で直接設定**
```45:58:Scripts/Core/GameFlowHandler.cs
public override void Spawned()
{
    RPC_SetGameState(GameState.Preparation);
}

[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
private void RPC_SetGameState(GameState state)
{
    CurrentGameState = state;
}
```
  - 起動時はサーバーで直接 `CurrentGameState = GameState.Preparation;` の方が無駄なRPCを減らせます。

- **EnemyCoordinator の二重Despawn防止**
  - `Reset()` と `OnEnemyDeath()` の両方で `Runner.Despawn` しています。既にDespawn済みの個体へ呼ばないよう、`enemy && enemy.Object && enemy.Object.IsValid` をチェック。

- **`WaveSpawner.cs` のパス名に先頭スペース**
  - `Scripts/Character/Enemy/Spawning/ WaveSpawner.cs` のようにフォルダ名/ファイル名にスペースが入っています。将来の自動処理やツール連携が壊れやすいのでリネーム推奨。

- **`ShipShieldDurability` の初期HP未設定**
```6:18:Scripts/Ship/ShipShieldDurability.cs
[Networked, OnChangedRender(nameof(UpdateHp))]
public float CurrentShieldPoints { get; private set; }

...

public override void Spawned()
{
    _shieldPointsRP.Value = CurrentShieldPoints;
}
```
  - `Spawned()`（権限側）で `CurrentShieldPoints` を最大値に初期化するフィールドを追加してください（例: `_maxShieldPoints`）。

- **`NetworkSceneManagerDefault` の付与先**
  - 現在は `GameLauncher` の `gameObject.AddComponent<NetworkSceneManagerDefault>()` を StartGameArgs に渡しています。Runner本体に付けるパターンの方が一般的（Runnerの生存期間に一致するため）です。

- **命名など**
  - `_sesionName` → `_sessionName`（スペル）
  - UI系（`ShipShieldButton`）は `NetworkBehaviour` である必要は必須ではありません。必要最小限のネット処理だけ分離すると責務がクリアになります。

### 参考（改善後の弾生成イメージ）
```csharp
// PlayerMovement.ShotBullet（権限側）
var pos = _armTransform.position + _armTransform.forward * 0.5f;
var rot = Quaternion.LookRotation(_armTransform.forward);
var bullet = Runner.Spawn(_bulletPrefab, pos, rot, Object.InputAuthority);
// bullet.Init は TickTimer開始のみ（回転はSpawnで配布済み）
bullet.Init(_armTransform.forward);
```
```csharp
// BulletMove（サーバーのみ実行）
public override void FixedUpdateNetwork() {
    if (!Object.HasStateAuthority) return;

    if (life.Expired(Runner)) {
        Runner.Despawn(Object);
        return;
    }

    var delta = Speed * Runner.DeltaTime;
    transform.position += transform.forward * delta;

    if (Runner.LagCompensation.Raycast(transform.position, transform.forward, delta, Object.InputAuthority, out var hit, _targetLayerMask)) {
        if (hit.GameObject.TryGetComponent<IDamageable>(out var target)) {
            target.TakeDamage(_damage, hit.Point, transform.forward);
            Runner.Despawn(Object);
        }
    }
}
```

### まとめ
- **プーリングの適用**: `PoolObjectProvider` をRunnerに接続して `ObjectProvider` に設定。
- **弾の整合性**: Spawn時に回転＋所有者を渡し、Ray長は `Speed * DeltaTime`、処理とDespawnはサーバー限定、`[Networked]` の乱用を削減。
- **初期状態とイベント**: 初期`GameState`は権限側で直接設定、二重Despawnを防止。
- **小修正**: パス名の先頭スペース除去、シールド初期値追加、`NetworkSceneManagerDefault` の付与先の見直し、命名など。

これらを適用すると、ラグ補償の正確性・権限整合性・初期化の堅牢性・性能（プール）が改善され、Fusionのベストプラクティスにより近づきます。