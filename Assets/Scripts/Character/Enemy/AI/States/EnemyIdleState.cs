using UnityEngine;

/// <summary>
/// Idle（待機）状態。
/// 周期的に索敵を行い、ターゲットが見つかれば距離に応じて
/// Attack または Chase へ遷移します。
/// </summary>
public sealed class EnemyIdleState : IEnemyState
{
    private readonly EnemyAIBrainState _brain;
    private readonly BaseEnemy _owner;
    private float _reacquireInterval = 0.5f;
    private float _reacquireTimer;

    public EnemyIdleState(EnemyAIBrainState brain, BaseEnemy owner)
    {
        _brain = brain;
        _owner = owner;
    }

    /// <summary>
    /// タイマー初期化のみ。重い処理は行わない。
    /// </summary>
    public void Enter()
    {
        _reacquireTimer = 0f;
    }

    /// <summary>
    /// 権限側のTickで呼ばれる。索敵と遷移判定を行う。
    /// </summary>
    public void NetworkUpdate()
    {
        _reacquireTimer -= _brain.Runner.DeltaTime;
        if (_reacquireTimer <= 0f)
        {
            _reacquireTimer = _reacquireInterval;
            SearchTarget();
        }

        var target = _brain.CurrentTarget;
        if (target == null) return;

        if (_brain.IsTargetInAttackRange(target))
        {
            _brain.TransitionTo(_brain.Attack);
            return;
        }
        if (_brain.IsTargetInVision(target))
        {
            _brain.TransitionTo(_brain.Chase);
        }
    }

    /// <summary>
    /// 待機状態の終了処理（現状はなし）。
    /// </summary>
    public void Exit()
    {
    }

    /// <summary>
    /// 視界内の最も近いターゲットを探索してセットする。
    /// </summary>
    private void SearchTarget()
    {
        Collider[] hits = Physics.OverlapSphere(_owner.transform.position, _owner.VisionRange, _owner.TargetMask);
        float nearestSqr = float.MaxValue;
        Transform nearest = null;
        for (int i = 0; i < hits.Length; i++)
        {
            var t = hits[i].transform;
            float sqr = (t.position - _owner.transform.position).sqrMagnitude;
            if (sqr < nearestSqr)
            {
                nearestSqr = sqr;
                nearest = t;
            }
        }
        _brain.SetTarget(nearest);
    }
}


