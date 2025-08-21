using System;
using Fusion;
using UnityEngine;

public class AnimationConductor : NetworkBehaviour
{
    [SerializeField, Required]
    private Animator _animator;

    [SerializeField, Required]
    private Transform _targetPos;

    [SerializeField]
    private float _maxLookAngle = 80f; // 最大視線角度

    [SerializeField]
    private float _moveTransitionSpeed = 5f; // 移動方向の変更スピード

    private float _currentLookAngle = 0f; // 現在の視線角度を保持
    private Vector2 _currentMoveValue = Vector2.zero; // 現在の移動アニメーション値

    private const string WORK_X_PARAM = "WorkX";
    private const string WORK_Y_PARAM = "WorkY";
    private const string Look_Y_PARAM = "LookY";

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerNetworkInput input))
        {
            // 移動入力を徐々に変更
            _currentMoveValue = Vector2.Lerp(_currentMoveValue, input.MoveInput, _moveTransitionSpeed * Runner.DeltaTime);
            
            _animator.SetFloat(WORK_X_PARAM, _currentMoveValue.x);
            _animator.SetFloat(WORK_Y_PARAM, _currentMoveValue.y);
            
            // 視線角度を累積し、制限を適用
            _currentLookAngle -= input.LookInput.y * Runner.DeltaTime; // 感度調整
            _currentLookAngle = Mathf.Clamp(_currentLookAngle, -_maxLookAngle, _maxLookAngle);
            
            // 正規化された値（-1～1）をアニメーターに設定
            float normalizedLookY = _currentLookAngle / _maxLookAngle;
            _animator.SetFloat(Look_Y_PARAM, -normalizedLookY);
        }
    }
}
