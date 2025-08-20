# Fusion2 クライアント・ホスト同期通信方式ガイド

この文書は状況別のクライアント・ホスト間伝達方法をMDC（Method-Direction-Case）方式でまとめた実践ガイドです。

## まずはこれ（クイックスタート）

### 基本原則
1. **継続的な状態**: `[Networked]`プロパティを使用
2. **一時的なイベント**: RPCを使用  
3. **即応性重視**: ローカル実行 + リモート確認
4. **信頼性重視**: ホスト権限 + 状態同期

## MDC方式による分類

### Method（メソッド別）

#### 1. [Networked] プロパティ同期
**特徴**: 自動的な状態同期、信頼性が高い、新規参加者にも同期

```csharp
public class Player : NetworkBehaviour
{
    [Networked] public int Health { get; set; }
    [Networked] public Vector3 Position { get; set; }
    [Networked] public bool IsAlive { get; set; }
    
    // 変更時自動実行
    [Networked, OnChangedRender(nameof(OnHealthChanged))]
    public int Health { get; set; }
    
    private void OnHealthChanged()
    {
        UpdateHealthUI(); // UI更新など
    }
}
```

#### 2. RPC（Remote Procedure Call）
**特徴**: 即座のイベント通知、一時的な処理

```csharp
public class WeaponController : NetworkBehaviour
{
    // クライアント → ホスト
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_Attack(Vector3 attackPosition, int damage)
    {
        ProcessAttack(attackPosition, damage);
        RPC_ShowAttackEffect(attackPosition); // 全クライアントへ
    }
    
    // ホスト → 全クライアント
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowAttackEffect(Vector3 position)
    {
        PlayAttackEffect(position);
    }
}
```

#### 3. ハイブリッド同期
**特徴**: ローカル反応 + リモート確認

```csharp
public class ItemPickup : NetworkBehaviour
{
    [Networked] public bool IsCollected { get; set; }
    
    public void OnCollisionLocal(Player player)
    {
        // 即座にローカルエフェクト（反応性向上）
        PlayCollectionEffect();
        
        // ホストに確認要求
        RPC_RequestCollection(player);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestCollection(Player collector)
    {
        if (IsCollected) return; // 既に取得済み
        
        IsCollected = true; // 状態更新（自動同期）
        collector.AddScore(10);
    }
}
```

### Direction（送信方向別）

#### 1. クライアント → ホスト
```csharp
[Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
public void RPC_ClientToHost(string data) { }
```

#### 2. ホスト → 全クライアント
```csharp
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_HostToAll(string data) { }
```

#### 3. ホスト → 特定クライアント
```csharp
[Rpc(RpcSources.StateAuthority, RpcTargets.All)]
public void RPC_HostToSpecific([RpcTarget] PlayerRef target, string data) { }
```

#### 4. 双方向（Shared Mode）
```csharp
[Rpc(RpcSources.All, RpcTargets.All)]
public void RPC_Broadcast(string data) { }
```

### Case（使用状況別）

#### ケース1: プレイヤー移動・状態管理
**方式**: `[Networked]`プロパティ + `FixedUpdateNetwork()`

```csharp
public class PlayerController : NetworkBehaviour
{
    [Networked] public Vector3 NetworkPosition { get; set; }
    [Networked] public int Health { get; set; }
    
    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            // 入力に基づく移動
            Vector3 movement = GetInputMovement();
            NetworkPosition += movement * Runner.DeltaTime;
            transform.position = NetworkPosition;
        }
    }
}
```

#### ケース2: 攻撃・ダメージシステム
**方式**: RPC（即座通知） + `[Networked]`（状態同期）

```csharp
public class CombatSystem : NetworkBehaviour
{
    [Networked] public int Health { get; set; }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_DealDamage(int damage, Vector3 hitPoint)
    {
        Health -= damage; // 状態は自動同期
        RPC_ShowDamageEffect(hitPoint); // エフェクトは即座表示
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ShowDamageEffect(Vector3 position)
    {
        PlayDamageEffect(position);
    }
}
```

#### ケース3: チャット・通知システム
**方式**: 純粋RPC（一時的メッセージ）

```csharp
public class ChatSystem : NetworkBehaviour
{
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SendMessage(string message)
    {
        // ホストが全員に配信
        RPC_BroadcastMessage(message, Runner.LocalPlayer);
    }
    
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_BroadcastMessage(string message, PlayerRef sender)
    {
        DisplayChatMessage(sender, message);
    }
}
```

#### ケース4: ゲーム状態管理
**方式**: `[Networked]`プロパティ + 変更検知

```csharp
public class GameManager : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(OnGameStateChanged))]
    public GameState CurrentState { get; set; }
    
    [Networked] public int WaveNumber { get; set; }
    [Networked] public float RemainingTime { get; set; }
    
    private void OnGameStateChanged()
    {
        switch (CurrentState)
        {
            case GameState.Preparation:
                StartPreparationPhase();
                break;
            case GameState.Wave:
                StartWavePhase();
                break;
        }
    }
}
```

#### ケース5: アイテム・インタラクション
**方式**: ハイブリッド（ローカル + リモート確認）

```csharp
public class InteractableObject : NetworkBehaviour
{
    [Networked] public bool IsInUse { get; set; }
    [Networked] public PlayerRef CurrentUser { get; set; }
    
    public void OnInteractLocal(Player player)
    {
        // 即座にローカル反応
        ShowInteractionUI();
        
        // ホストに権限確認
        RPC_RequestInteraction(player);
    }
    
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_RequestInteraction(Player player)
    {
        if (IsInUse) return; // 既に使用中
        
        IsInUse = true;
        CurrentUser = player.Object.InputAuthority;
        
        // 開始通知
        RPC_StartInteraction();
    }
}
```

## パフォーマンス最適化

### 頻度制御
```csharp
public class OptimizedSync : NetworkBehaviour
{
    [Networked] public Vector3 Position { get; set; }
    private TickTimer _syncTimer;
    
    public override void FixedUpdateNetwork()
    {
        // 0.1秒ごとに同期（60FPS = 6tickごと）
        if (_syncTimer.ExpiredOrNotRunning(Runner))
        {
            Position = transform.position;
            _syncTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
        }
    }
}
```

### 条件付き同期
```csharp
public class ConditionalSync : NetworkBehaviour
{
    [Networked] public bool IsMoving { get; set; }
    [Networked] public Vector3 Position { get; set; }
    
    private Vector3 _lastPosition;
    
    public override void FixedUpdateNetwork()
    {
        bool currentlyMoving = Vector3.Distance(transform.position, _lastPosition) > 0.01f;
        
        // 移動状態変化時のみ同期
        if (IsMoving != currentlyMoving)
        {
            IsMoving = currentlyMoving;
        }
        
        // 移動中のみ位置同期
        if (IsMoving)
        {
            Position = transform.position;
        }
        
        _lastPosition = transform.position;
    }
}
```

## トラブルシューティング

### よくある問題と解決法

1. **RPCが呼ばれない**
   - メソッド名に"RPC"がない → 名前に"RPC"を含める
   - 権限がない → `Object.HasInputAuthority`を確認

2. **状態が同期されない**
   - `[Networked]`が抜けている → プロパティに属性追加
   - `FixedUpdateNetwork()`外で変更 → 正しいメソッド内で変更

3. **パフォーマンス低下**
   - 高頻度同期 → 頻度制御やTimer使用
   - 不要な同期 → 条件付き同期に変更

## 参考資料

- [Photon Fusion公式: Remote Procedure Calls](https://doc.photonengine.com/fusion/current/tutorials/host-mode-basics/6-remote-procedure-calls)
- [Photon Fusion公式: Networking Controller Code](https://doc.photonengine.com/fusion/current/concepts-and-patterns/networked-controller-code)
- プロジェクト内の実装例: `Scripts/Character/Enemy/Types/EnemyWakame.cs`

## 更新履歴
- 2024/XX/XX: 初版作成（MDC方式による分類整理） 