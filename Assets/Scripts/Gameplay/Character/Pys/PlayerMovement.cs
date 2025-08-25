using Fusion;
using UnityEngine;
using R3;

public class PlayerMovement : NetworkBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float _moveSpeed = 5.0f; // 移動速度
    [SerializeField] private float _runSpeedMultiplier = 1.5f; // 走行時の速度倍率
    //[SerializeField] private float _jumpHeight = 2.0f; // ジャンプ高度
    //[SerializeField] private float _gravity = -9.81f; // 重力加速度

    [Header("物理判定")]
    [SerializeField] private LayerMask _groundLayerMask = 1; // 地面レイヤー
    //[SerializeField] private float _groundCheckDistance = 0.1f; // 地面チェック距離
    [SerializeField] private Vector3 _groundCheckOffset = new Vector3(0, 0.1f, 0); // 地面チェック開始位置オフセット

    [Header("状態データ")]
    [SerializeField, Required]
    private PlayerStatus _playerStatus; // プレイヤーステータス
    private bool _isInteracting = false; // インタラクション中かどうか

    [SerializeField, Required]
    private Transform _armTransform; // アームのGameObject
    [SerializeField, Required]
    private BulletMove _bulletPrefab; // 弾丸プレハブ
    public override void Spawned()
    {
        _playerStatus?.IsControllerInteracting
            .Subscribe(interact => _isInteracting = interact)
            .AddTo(this);
    }

    public override void FixedUpdateNetwork()
    {
        if (_isInteracting) return;

        if (!GetInput(out PlayerNetworkInput input)) return;

        DoMove(input);
        DoRotation(input);

        if (input.AttackPressed.IsSet(MyButtons.Attack))
        {
            ShotBullet();
        }
    }

    private void DoMove(PlayerNetworkInput input)
    {
        if (input.MoveDirection == Vector3.zero) return;
        bool isRunning = input.RunPressed.IsSet(MyButtons.Run);

        float currentSpeed = isRunning ? _moveSpeed * _runSpeedMultiplier : _moveSpeed;

        transform.position += input.MoveDirection * currentSpeed * Runner.DeltaTime;
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

    /// <summary>
    /// TODO: 一時的な実装。後で削除予定。
    /// </summary>
    private void ShotBullet()
    {
        if (!Object.HasStateAuthority)
        {
            // クライアントはサーバーに弾丸発射を要求
            RPC_RequestShotBullet();
            return;
        }

        // 弾丸の発射位置をアームの先端に設定
        Vector3 bulletSpawnPosition = _armTransform.position + _armTransform.forward * 0.5f;
        // 弾丸を生成
        BulletMove bullet = Runner.Spawn(_bulletPrefab, bulletSpawnPosition, Quaternion.identity);

        // 弾丸の初期化
        bullet.Init(_armTransform.forward);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestShotBullet()
    {
        // サーバーに弾丸発射を要求するRPC
        ShotBullet();
    }
}
