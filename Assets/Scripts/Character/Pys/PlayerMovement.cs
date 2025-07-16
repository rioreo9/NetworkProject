using Fusion;
using System;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float _moveSpeed = 5.0f; // 移動速度
    //[SerializeField] private float _runSpeedMultiplier = 1.5f; // 走行時の速度倍率
    //[SerializeField] private float _jumpHeight = 2.0f; // ジャンプ高度
    //[SerializeField] private float _gravity = -9.81f; // 重力加速度

    [Header("物理判定")]
    [SerializeField] private LayerMask _groundLayerMask = 1; // 地面レイヤー
    //[SerializeField] private float _groundCheckDistance = 0.1f; // 地面チェック距離
    [SerializeField] private Vector3 _groundCheckOffset = new Vector3(0, 0.1f, 0); // 地面チェック開始位置オフセット

    [SerializeField, Required]
    private Transform _armTransform; // アームのGameObject
    [SerializeField, Required]
    private Transform _bodyTransfom;

    public override void Spawned()
    {
       
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerNetworkInput input))
        {
            DoMove(input);
            DoRotation(input);
        }

    }

    private void DoMove(PlayerNetworkInput input)
    {
        if (input.MoveDirection == Vector3.zero) return;

        transform.position += input.MoveDirection * _moveSpeed * Runner.DeltaTime;
    }

    private void DoRotation(PlayerNetworkInput input)
    {
        Vector3 cameraDirection = input.CameraForwardDirection;
        Vector3 armDirection = input.CameraForwardDirection;

        // Y成分を0にしてY軸回転のみにする
        cameraDirection.y = 0f;

        // 正規化してからキャラクターの回転を設定
        cameraDirection.Normalize();

        transform.rotation = Quaternion.LookRotation(cameraDirection);

        // アームの回転をカメラの方向に完全に合わせる（左右回転 + 上下回転）
        armDirection.Normalize();

        _armTransform.rotation = Quaternion.LookRotation(armDirection);
    }
}
