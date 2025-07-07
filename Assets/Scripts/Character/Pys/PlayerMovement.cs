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

    public float MoveSpeed => _moveSpeed; // 移動速度プロパティ

    [Header("物理判定")]
    [SerializeField] private LayerMask _groundLayerMask = 1; // 地面レイヤー
    //[SerializeField] private float _groundCheckDistance = 0.1f; // 地面チェック距離
    [SerializeField] private Vector3 _groundCheckOffset = new Vector3(0, 0.1f, 0); // 地面チェック開始位置オフセット

    private PlayerMove _playerMove; // プレイヤー移動コンポーネント
    private RotationMove _rotationMove; // プレイヤー回転コンポーネント
    private PlayerJump _playerJump; // プレイヤージャンプコンポーネント

    public override void Spawned()
    {
      
    }

    public override void FixedUpdateNetwork()
    {
        // キャラクターの回転処理
        _rotationMove?.DoRotation();

    }

    public void SetCamera(CinemachineCamera camera)
    {
        _rotationMove = new RotationMove(transform, camera); // プレイヤー回転コンポーネントの初期化
        _playerJump = new();
    }
}
