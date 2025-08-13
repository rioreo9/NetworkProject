using Fusion;
using System;
using UnityEngine;

public class EnemyAIBrainState : NetworkBehaviour
{
    public enum AIState
    {
        Idle,    
        Chase,   // 追跡
        Attack,
        Dead
    }

    [Networked] public AIState State { get; private set; }
    [Networked] private NetworkObject TargetRef { get; set; }


    public void Initialize()
    {
        State = AIState.Idle;
    }



}
