using System;
using Fusion;
using UnityEngine;

public class AnimationConductor : NetworkBehaviour
{
    [SerializeField, Required]
    private Animator _animator; // アニメーターコンポーネント4

    [SerializeField, Required]
    private Transform _targetPos;

    private const string WORK_X_PARAM = "WorkX";
    private const string WORK_Y_PARAM = "WorkY";

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerNetworkInput input))
        {
           _animator.SetFloat(WORK_X_PARAM, input.MoveInput.x);
           _animator.SetFloat(WORK_Y_PARAM, input.MoveInput.y);
        }
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!Object.HasInputAuthority) return;

        _animator.SetLookAtWeight(1.0f, 0.8f, 1.0f, 0.0f, 0f);
        _animator.SetLookAtPosition(_targetPos.position);
    }
}
