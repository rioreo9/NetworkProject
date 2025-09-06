# Photon Fusion Pirate Adventure - 技術設計解説

この文書は[Photon Fusion Pirate Adventure Tech Design](https://doc.photonengine.com/ja-jp/fusion/current/game-samples/pirate-adventure/tech-design)の技術仕様を日本語でまとめた実装ガイドです。

## 概要

Pirate Adventureサンプルは**Shared Mode**を使用したマルチプレイヤー協力冒険ゲームで、最大4人のプレイヤーが海賊として島を探索し、ボスを倒すゲームです。本サンプルはPhoton Fusion 2の主要機能を実装例として示しています。

## 核心概念

### Shared Mode とは
- **定義**: すべてのクライアントがゲーム状態を共有し、特定のオブジェクトに対してState Authority（状態権限）を持つモード
- **特徴**: 
  - より高いレスポンス性
  - State Authorityの動的な転送が可能
  - クライアント間でのロードバランシング

### State Authority とRPC
State Authorityを持つクライアントのみがネットワークプロパティを変更可能です。

```csharp
// State Authorityチェック例
if (Object.HasStateAuthority)
{
    // ネットワークプロパティの変更が可能
    Health -= damage;
}
else
{
    // RPCを使用してState Authorityに変更を要求
    RPC_TakeDamage(damage);
}
```

## ローカル vs リモートレスポンス設計

### 即座のローカル反応
プレイヤーの体験向上のため、アクションに対してまずローカルで即座に反応を示し、その後ネットワーク同期を行います。

```csharp
// 例：アイテム収集
private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Player"))
    {
        // 1. 即座にローカル効果を表示
        PlayPickupEffect();
        
        // 2. State Authorityに収集を通知
        RPC_Collect(other.GetComponent<NetworkObject>().Id);
    }
}

[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
public void RPC_Collect(NetworkId playerId)
{
    if (HasStateAuthority && !Collected)
    {
        Collected = true;
        // プレイヤーに報酬を送信
        Player.RPC_Reward(rewardAmount);
    }
}
```

## Multi-Peer Mode機能

### Multi-Peer Singleton
複数の`NetworkRunner`が同時に動作する環境での唯一のインスタンス管理。

```csharp
public class MultiPeerSingleton : NetworkBehaviour
{
    private static MultiPeerSingleton _instance;
    
    public static MultiPeerSingleton Instance 
    { 
        get 
        {
            if (_instance == null)
                _instance = FindObjectOfType<MultiPeerSingleton>();
            return _instance;
        } 
    }
}
```

## メインメニューシステム

### リージョン選択
- プレイヤーの地理的位置に基づく最適なサーバー選択
- レイテンシーの最小化

### セッション開始フロー
```csharp
// TaskManager.Delayを使用した非同期処理
public async Task StartGameAsync()
{
    // ロビー作成
    await TaskManager.Delay(100); // フレーム待機
    
    // プレイヤー接続待機
    while (connectedPlayers < minPlayers)
    {
        await TaskManager.Delay(1000); // 1秒待機
    }
    
    // ゲーム開始
    StartGame();
}
```

## プレイヤーシステム

### NetworkRunner Prefab
各プレイヤーに対応する`NetworkRunner`のプレハブ管理：

```csharp
[Networked] public PlayerRef PlayerRef { get; set; }
[Networked] public string PlayerName { get; set; }
[Networked] public int Health { get; set; }
[Networked] public int Money { get; set; }
[Networked] public bool IsDamaged { get; set; }
```

### ダメージビジュアル効果
```csharp
[Networked, OnChangedRender(nameof(OnDamagedChanged))]
public bool IsDamaged { get; set; }

private void OnDamagedChanged()
{
    if (IsDamaged)
    {
        StartCoroutine(FlickerEffect());
    }
}
```

## 衝突検知システム

### 物理ベース検知
`NetworkRunner`の物理シーンを使用した正確な衝突検知：

```csharp
public bool CheckCollision(Vector3 center, float radius)
{
    var hitCount = Runner.GetPhysicsScene().OverlapCapsule(
        center, 
        center + Vector3.up * height, 
        radius, 
        hits, 
        collisionMask
    );
    
    return hitCount > 0;
}
```

### ピックアップシステムの流れ

1. **ローカル検知**: プレイヤーがアイテムに接触
2. **即座の反応**: ローカルでエフェクト・サウンド再生
3. **RPC送信**: State Authorityに収集を通知
4. **権限確認**: State Authorityが収集を承認
5. **報酬付与**: `Player.RPC_Reward`で報酬を送信

```csharp
// ピックアップ実装例
private void Update()
{
    if (Runner.GetPhysicsScene().OverlapCapsule(/* parameters */) > 0)
    {
        foreach (var pickup in detectedPickups)
        {
            // ローカル効果
            pickup.PlayCollectEffect();
            
            // State Authorityに通知
            pickup.RPC_Collect(Object.InputAuthority);
        }
    }
}
```

## スイッチとブリッジシステム

### 協力ベースのメカニズム
複数プレイヤーの協力が必要なギミック：

```csharp
public class CooperativeSwitch : NetworkBehaviour
{
    [Networked] public int PlayersOnSwitch { get; set; }
    [Networked] public bool IsActivated { get; set; }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && HasStateAuthority)
        {
            PlayersOnSwitch++;
            UpdateBridgeState();
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && HasStateAuthority)
        {
            PlayersOnSwitch = Mathf.Max(0, PlayersOnSwitch - 1);
            UpdateBridgeState();
        }
    }
    
    private void UpdateBridgeState()
    {
        IsActivated = PlayersOnSwitch > 0;
    }
}
```

## 敵AIシステム

### サメ（Shark）の行動パターン

1. **索敵フェーズ**: 近くのプレイヤーを検索
2. **追跡フェーズ**: ターゲットプレイヤーを追跡
3. **State Authority転送**: 追跡されるプレイヤーがサメの制御権を取得
4. **攻撃フェーズ**: 射程内での攻撃実行
5. **帰還フェーズ**: ターゲット喪失時の初期位置への復帰

```csharp
public class SharkAI : NetworkBehaviour
{
    [Networked] public Vector3 TargetPosition { get; set; }
    [Networked] public PlayerRef TargetPlayer { get; set; }
    
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;
        
        switch (currentState)
        {
            case AIState.Searching:
                SearchForPlayers();
                break;
            case AIState.Chasing:
                ChaseTarget();
                RequestStateAuthorityTransfer();
                break;
            case AIState.Attacking:
                AttackTarget();
                break;
            case AIState.Returning:
                ReturnToSpawn();
                break;
        }
    }
    
    private void RequestStateAuthorityTransfer()
    {
        if (TargetPlayer.IsValid)
        {
            Object.RequestStateAuthority(TargetPlayer);
        }
    }
}
```

## ボスシステム

### 触手（Tentacle）攻撃システム
```csharp
public class BossTentacle : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnAttackChanged))]
    public int AttackIndex { get; set; }
    
    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority) // SharedMasterModeClientのみ
        {
            // ランダムな攻撃パターンを選択
            if (attackTimer.ExpiredOrNotRunning(Runner))
            {
                AttackIndex = Random.Range(0, attackAnimations.Length);
                attackTimer = TickTimer.CreateFromSeconds(Runner, attackCooldown);
            }
        }
    }
    
    private void OnAttackChanged()
    {
        // ローカルで即座にアニメーション再生
        PlayAttackAnimation(AttackIndex);
    }
}
```

### ボス撃破システム
```csharp
public class Boss : NetworkBehaviour, IBossDeathListener
{
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BossDefeated()
    {
        // 全クライアントに撃破を通知
        var listeners = FindObjectsOfType<MonoBehaviour>()
            .OfType<IBossDeathListener>();
            
        foreach (var listener in listeners)
        {
            listener.OnBossDefeated();
        }
    }
}

public interface IBossDeathListener
{
    void OnBossDefeated();
}
```

## 実装のベストプラクティス

### 1. レスポンシブ性の確保
```csharp
// 悪い例：ネットワーク同期まで待機
public void OnPlayerAction()
{
    RPC_PerformAction();
    // ユーザーは反応を待つ必要がある
}

// 良い例：即座にローカル反応
public void OnPlayerAction()
{
    // 1. 即座にローカル効果
    PlayLocalEffect();
    
    // 2. ネットワーク同期
    RPC_PerformAction();
}
```

### 2. State Authority管理
```csharp
public void RequestControlOfEnemy(NetworkObject enemy)
{
    if (!enemy.HasStateAuthority)
    {
        enemy.RequestStateAuthority(Object.InputAuthority);
    }
}
```

### 3. ネットワーク最適化
```csharp
// 頻繁に変更される値は[Networked]
[Networked] public Vector3 Position { get; set; }

// 稀に変更される値はRPC
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_GameModeChanged(GameMode newMode) { }
```

## まとめ

Pirate Adventureサンプルは以下の重要な概念を実装しています：

1. **Shared Mode**: 動的なState Authority転送によるレスポンシブ性
2. **ローカル優先設計**: 即座の反応 + 後続の同期
3. **協力メカニズム**: 複数プレイヤーの協調が必要なゲームプレイ
4. **スケーラブルなAI**: NavMesh + FSM + Authority転送
5. **ロバストなボスシステム**: 複数フェーズ + RPC通知

これらの設計パターンは、リアルタイム協力マルチプレイヤーゲームの開発において非常に有用な参考実装となります。

## 参考リンク

- [Photon Fusion Pirate Adventure Tech Design](https://doc.photonengine.com/ja-jp/fusion/current/game-samples/pirate-adventure/tech-design)
- [Photon Fusion Documentation](https://doc.photonengine.com/ja-jp/fusion/current)
