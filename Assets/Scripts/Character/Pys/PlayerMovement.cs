using Fusion;
using System;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour, ISetPlayerInformation
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

    [SerializeField] private Transform _cameraHandle; // 回転速度

    private CinemachineCamera _camera; // カメラの参照
    private float _networkedYRotation; // ネットワーク同期されるX軸回転角度

    private PlayerMove _playerMove; // プレイヤー移動コンポーネント
    private RotationMove _rotationMove; // プレイヤー回転コンポーネント
    private PlayerJump _playerJump; // プレイヤージャンプコンポーネント

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            _camera = FindFirstObjectByType<CinemachineCamera>();
        }
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
        // マウス入力に感度と時間を適用
        float rotationDelta = input.LookInput.x * Runner.DeltaTime;

        // 累積回転角度を更新（Y軸回転）
        _networkedYRotation += rotationDelta;

        Quaternion bodyRotation = Quaternion.Euler(0f, _networkedYRotation, 0f);
        transform.rotation = bodyRotation;

        Debug.Log($"Y軸回転: {_networkedYRotation}度");
    }

    public void SetCamera(CinemachineCamera camera)
    {
        //_camera = camera;
    }
}
