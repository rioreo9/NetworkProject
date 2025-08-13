using Fusion;
using System;
using UnityEngine;

/// <summary>
/// 敵AIのステートマシン本体。権限側のみで状態更新を行い、
/// 各具体ステート（Idle/Chase/Attack/Dead）へ Enter/Exit/NetworkUpdate を委譲します。
/// 軽量な同期のために列挙値 <see cref="AIState"/> を Networked で公開します。
/// </summary>
public class EnemyAIBrainState : NetworkBehaviour
{
    /// <summary>
    /// クライアント間で同期する軽量な状態の列挙。
    /// アニメーションやデバッグ表示の切り替えに利用します。
    /// </summary>
    public enum AIState
    {
        Idle,    
        Chase,   // 追跡
        Attack,
        Dead
    }

    // 現在の状態（同期用）。UI/デバッグでの可視化に使用可能。
    [Networked] public AIState State { get; private set; }
    // 追跡対象の NetworkObject 参照（必要なら復元用）。ローカル参照は _target を使用。
    [Networked] private NetworkObject TargetRef { get; set; }

    /// <summary>
    /// ステート遷移時の通知イベント（前状態, 次状態）。
    /// 見た目（アニメーション等）の切替えに購読してください。
    /// </summary>
    public event Action<AIState, AIState> OnStateChanged; // prev, next

    // 現在稼働中の具体ステート
    private IEnemyState _current;
    // 各ステートのインスタンス（生成後にキャッシュ）
    public IEnemyState Idle { get; private set; }
    public IEnemyState Chase { get; private set; }
    public IEnemyState Attack { get; private set; }
    public IEnemyState DeadState { get; private set; }

    // 管理対象の敵本体
    private BaseEnemy _owner;
    // ローカルのターゲット Transform（権限側でのみ更新）
    private Transform _target;

    /// <summary>
    /// 現在のターゲット Transform（権限側でのみ有効）。
    /// </summary>
    public Transform CurrentTarget => _target;

    /// <summary>
    /// ターゲットを設定。権限側では <see cref="TargetRef"/> も同期更新します。
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
        if (HasStateAuthority)
        {
            TargetRef = _target != null ? _target.GetComponentInParent<NetworkObject>() : null;
        }
    }

    /// <summary>
    /// ステートマシン初期化。各具体ステートを生成し、初期状態 Idle へ遷移します。
    /// </summary>
    public void Initialize()
    {
        if (_owner == null)
        {
            _owner = GetComponent<BaseEnemy>();
        }

        // ステート生成
        Idle = new EnemyIdleState(this, _owner);
        Chase = new EnemyChaseState(this, _owner);
        Attack = new EnemyAttackState(this, _owner);
        DeadState = new EnemyDeadState(this, _owner);

        // 初期遷移
        TransitionTo(Idle);
    }

    /// <summary>
    /// ステート遷移処理。Exit→Enter の順で呼び、列挙値も同期更新します。
    /// 同一インスタンスへの遷移は無視します。
    /// </summary>
    public void TransitionTo(IEnemyState next)
    {
        if (next == null) return;
        if (_current == next) return;

        var prevEnum = State;
        _current?.Exit();
        _current = next;
        State = MapToEnum(_current);
        _current.Enter();
        OnStateChanged?.Invoke(prevEnum, State);
    }

    /// <summary>
    /// 権限側の Tick 更新。死亡時は Dead に遷移し、それ以外は現ステートの更新を行います。
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // 生存監視
        if (_owner != null && !_owner.IsAlive)
        {
            if (State != AIState.Dead)
            {
                TransitionTo(DeadState);
            }
            return;
        }

        _current?.NetworkUpdate();
    }

    /// <summary>
    /// 水平面のみでターゲット方向へ即時回頭します。
    /// </summary>
    internal static void RotateTowards(Transform self, Vector3 worldTargetDirection)
    {
        worldTargetDirection.y = 0f;
        if (worldTargetDirection.sqrMagnitude < 0.0001f) return;
        worldTargetDirection.Normalize();
        self.rotation = Quaternion.LookRotation(worldTargetDirection);
    }

    /// <summary>
    /// 対象が視界距離内にいるかを判定（距離のみ）。
    /// </summary>
    internal bool IsTargetInVision(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(_owner.transform.position, target.position);
        return distance <= _owner.VisionRange;
    }

    /// <summary>
    /// 対象が攻撃射程内にいるかを判定（距離のみ）。
    /// </summary>
    internal bool IsTargetInAttackRange(Transform target)
    {
        if (target == null) return false;
        float distance = Vector3.Distance(_owner.transform.position, target.position);
        return distance <= _owner.AttackRange;
    }

    /// <summary>
    /// 具体ステートを同期用の列挙値へマッピングします。
    /// </summary>
    private AIState MapToEnum(IEnemyState state)
    {
        if (state == Idle) return AIState.Idle;
        if (state == Chase) return AIState.Chase;
        if (state == Attack) return AIState.Attack;
        return AIState.Dead;
    }
}
