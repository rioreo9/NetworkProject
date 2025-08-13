using Fusion;
using UnityEngine;

/// <summary>
/// Attack（攻撃）状態。
/// ターゲットへ回頭し、攻撃クールダウンに応じて <see cref="BaseEnemy.AttackTarget"/> を呼びます。
/// 射程外になった場合は Chase へ遷移します。
/// </summary>
public sealed class EnemyAttackState : IEnemyState
{
    private readonly EnemyAIBrainState _brain;
    private readonly BaseEnemy _owner;
    private TickTimer _attackCooldown;

    public EnemyAttackState(EnemyAIBrainState brain, BaseEnemy owner)
    {
        _brain = brain;
        _owner = owner;
    }

    /// <summary>
    /// 攻撃クールダウンの初期化のみを行う。
    /// </summary>
    public void Enter()
    {
        _attackCooldown = TickTimer.CreateFromSeconds(_brain.Runner, _owner.GetAttackInterval());
    }

    /// <summary>
    /// 攻撃実行と射程外判定による遷移を行う。
    /// </summary>
    public void NetworkUpdate()
    {
        var target = _brain.CurrentTarget;
        if (target == null)
        {
            _brain.TransitionTo(_brain.Idle);
            return;
        }

        Vector3 dir = target.position - _owner.transform.position;
        EnemyAIBrainState.RotateTowards(_owner.transform, dir);

        if (_attackCooldown.ExpiredOrNotRunning(_brain.Runner))
        {
            _owner.AttackTarget();
            _attackCooldown = TickTimer.CreateFromSeconds(_brain.Runner, _owner.GetAttackInterval());
        }

        if (!_brain.IsTargetInAttackRange(target))
        {
            _brain.TransitionTo(_brain.Chase);
        }
    }

    /// <summary>
    /// 攻撃状態の終了処理（現状はなし）。
    /// </summary>
    public void Exit()
    {
    }
}


