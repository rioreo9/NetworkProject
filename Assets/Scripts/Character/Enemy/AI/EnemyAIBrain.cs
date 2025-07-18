using Fusion;
using System;

public class EnemyAIBrain : NetworkBehaviour
{
    enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Flee
    }

    private AIState _currentState;

    // 戦艦追跡・攻撃ロジック
    private void ApproachBattleship() { }
    private void ExecuteAttack() { }
    private void HandleRetreat() { }
}
