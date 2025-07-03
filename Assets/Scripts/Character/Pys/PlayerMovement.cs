using Fusion;
using System;
using Unity.Cinemachine;
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

    private PlayerMove _playerMove; // プレイヤー移動コンポーネント
    private RotationMove _rotationMove; // プレイヤー回転コンポーネント
    private PlayerJump _playerJump; // プレイヤージャンプコンポーネント

    public override void Spawned()
    {
        
    }

    public override void FixedUpdateNetwork()
    {
        

        // ローカルプレイヤーのみ処理を実行
        if (!Object.HasInputAuthority) return;

        Debug.Log(Id + "move");

        // 入力取得
        if (GetInput<PlayerNetworkInput>(out PlayerNetworkInput input))
        {
            _playerMove?.DoMove(input.MovementInput, _moveSpeed, Runner.DeltaTime);
            _playerJump?.DoJump(input);
        }

        // キャラクターの回転処理
        _rotationMove?.DoRotation(); 
     
    }

    public void SetCamera(CinemachineCamera camera)
    {
        //ここでキャラクターの状態を購読させる
        _playerMove = new PlayerMove(transform, camera); // プレイヤー移動コンポーネントの初期化
        _rotationMove = new RotationMove(transform, camera); // プレイヤー回転コンポーネントの初期化
        _playerJump = new();
    }
}
