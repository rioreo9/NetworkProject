using UnityEngine;

/// <summary>
/// Dead（死亡）状態。
/// 入場時に動作を停止し、その後の更新は行いません。
/// </summary>
public sealed class EnemyDeadState : IEnemyState
{
    private readonly EnemyAIBrainState _brain;
    private readonly BaseEnemy _owner;

    public EnemyDeadState(EnemyAIBrainState brain, BaseEnemy owner)
    {
        _brain = brain;
        _owner = owner;
    }

    /// <summary>
    /// 死亡時は移動・攻撃などの更新を止める。
    /// </summary>
    public void Enter()
    {
        // 死亡時は更新を止める
    }

    /// <summary>
    /// 何もしない（待機）。
    /// </summary>
    public void NetworkUpdate()
    {
        // 何もしない
    }

    /// <summary>
    /// 後始末（現状はなし）。
    /// </summary>
    public void Exit()
    {
    }
}


