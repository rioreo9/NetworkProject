using System;
using Fusion;
using UnityEngine;

public class AnimationConductor : NetworkBehaviour
{
    [SerializeField,Required]
    private Animator _animator; // アニメーターコンポーネント

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerNetworkInput input))
        {
           _animator.SetFloat("WorkX", input.MoveInput.x);
           _animator.SetFloat("WorkY", input.MoveInput.y);
        }
    }
}
