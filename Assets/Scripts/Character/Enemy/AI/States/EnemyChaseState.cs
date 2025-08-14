using UnityEngine;

/// <summary>
/// Chase（追跡）状態。
/// ターゲット方向へ回頭し、前進します。射程に入れば Attack へ、
/// 視界から外れれば Idle へ戻ります。
/// </summary>
public sealed class EnemyChaseState : IEnemyState
{
    private readonly EnemyAIBrainState _brain;
    private readonly BaseEnemy _owner;

    public EnemyChaseState(EnemyAIBrainState brain, BaseEnemy owner)
    {
        _brain = brain;
        _owner = owner;
    }

    /// <summary>
    /// 追跡開始時の軽量な初期化（現状はなし）。
    /// </summary>
    public void Enter()
    {
    }

    /// <summary>
    /// 追跡ロジックの更新と遷移判定。
    /// </summary>
    public void NetworkUpdate()
    {
        Transform target = _brain.CurrentTarget;
        if (target == null)
        {
            _brain.TransitionTo(_brain.Idle);
            return;
        }

        Vector3 dir = target.position - _owner.transform.position;
        EnemyAIBrainState.RotateTowards(_owner.transform, dir);
        _owner.transform.position += _owner.transform.forward * _owner.MoveSpeed * _brain.Runner.DeltaTime;

        if (_brain.IsTargetInAttackRange(target))
        {
            _brain.TransitionTo(_brain.Attack);
            return;
        }
        if (!_brain.IsTargetInVision(target))
        {
            _brain.TransitionTo(_brain.Idle);
        }


    }

    /// <summary>
    /// 追跡終了時の後始末（現状はなし）。
    /// </summary>
    public void Exit()
    {

    }
}


