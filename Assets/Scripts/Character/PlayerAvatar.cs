using Fusion;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// ネットワーク対応プレイヤーアバター
/// オンライン環境でのマルチプレイヤー移動を実現
/// </summary>
public class PlayerAvatar : NetworkBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float _moveSpeed = 5.0f; // 移動速度
    [SerializeField] private float _jumpForce = 10.0f; // ジャンプ力
    
    [Header("Network Settings")]
    [SerializeField] private float _interpolationRate = 15.0f; // 補間レート（スムーズな同期用）
    
    // ネットワーク同期される位置と回転
    [Networked] public Vector3 NetworkPosition { get; set; }
    [Networked] public Quaternion NetworkRotation { get; set; }
    
    private NetworkCharacterController _characterController; // ネットワーク対応キャラクターコントローラー
    private PlayerAvatarView _view; // ビューコンポーネント
    
    // ローカルプレイヤーのみが使用する変数
    private Camera _playerCamera;
    private bool _isLocalPlayer;

    public override void Spawned()
    {
        // コンポーネント取得
        _characterController = GetComponent<NetworkCharacterController>();
        _view = GetComponent<PlayerAvatarView>();
        
        // ローカルプレイヤーかどうかを判定
        _isLocalPlayer = Object.HasInputAuthority;
        
        // ローカルプレイヤーの場合のみカメラ設定
        if (_isLocalPlayer)
        {
            _view.MakeCameraTarget(); // カメラターゲットに設定
            _playerCamera = Camera.main;
            Debug.Log("ローカルプレイヤーとしてスポーンされました");
        }
        else
        {
            Debug.Log("リモートプレイヤーとしてスポーンされました");
        }
        
        // 初期位置を同期
        NetworkPosition = transform.position;
        NetworkRotation = transform.rotation;
    }

    /// <summary>
    /// ネットワーク固定フレーム更新（サーバー側で実行）
    /// ここで物理演算や権威のある処理を行う
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // 入力権限を持つプレイヤーのみ処理
        if (!Object.HasInputAuthority) return;
        
        // ネットワーク入力を取得
        if (GetInput<PlayerNetworkInput>(out PlayerNetworkInput input))
        {
            ProcessMovement(input); // 移動処理
            ProcessInteraction(input); // インタラクト処理
        }
        
        // ネットワーク位置を更新
        NetworkPosition = transform.position;
        NetworkRotation = transform.rotation;
    }

    /// <summary>
    /// レンダリング更新（クライアント側で実行）
    /// ビジュアルの補間やエフェクトを処理
    /// </summary>
    public override void Render()
    {
        // リモートプレイヤーの場合、ネットワーク位置に補間
        if (!_isLocalPlayer)
        {
            // スムーズな位置補間
            transform.position = Vector3.Lerp(transform.position, NetworkPosition, 
                Time.deltaTime * _interpolationRate);
            transform.rotation = Quaternion.Lerp(transform.rotation, NetworkRotation, 
                Time.deltaTime * _interpolationRate);
        }
    }

    /// <summary>
    /// 移動とジャンプの処理
    /// </summary>
    /// <param name="input">ネットワーク入力</param>
    private void ProcessMovement(PlayerNetworkInput input)
    {
        // カメラ方向を基準とした移動方向計算
        Vector3 moveDirection = Vector3.zero;
        
        if (_playerCamera != null)
        {
            // カメラの前方向を基準に移動方向を計算
            Vector3 forward = _playerCamera.transform.forward;
            Vector3 right = _playerCamera.transform.right;
            
            // Y軸成分を除去（水平移動のみ）
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            // 入力に基づいて移動方向を決定
            moveDirection = (forward * input.MovementInput.y + right * input.MovementInput.x).normalized;
        }
        else
        {
            // カメラがない場合はワールド座標基準
            moveDirection = new Vector3(input.MovementInput.x, 0f, input.MovementInput.y);
        }
        
        // キャラクター移動
        _characterController.Move(moveDirection * _moveSpeed);
        
        // ジャンプ処理
        if (input.JumpPressed)
        {
            
        }
    }

    /// <summary>
    /// インタラクト処理
    /// </summary>
    /// <param name="input">ネットワーク入力</param>
    private void ProcessInteraction(PlayerNetworkInput input)
    {
        if (!input.InteractPressed) return;
        
        // インタラクト処理（後で実装予定）
        Debug.Log("インタラクトが実行されました（オンライン）");
        
        // RPC呼び出しでエフェクト等を同期可能
        RPC_OnInteract();
    }

    /// <summary>
    /// インタラクト時のエフェクトを全プレイヤーに同期
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_OnInteract()
    {
        // 全プレイヤーでエフェクトやサウンドを再生
        Debug.Log($"プレイヤー {Object.InputAuthority} がインタラクトしました");
        
        // パーティクルエフェクト等をここで再生
        // PlayInteractionEffect();
    }

    /// <summary>
    /// デバッグ用：ネットワーク状態の表示
    /// </summary>
    private void OnGUI()
    {
        if (!_isLocalPlayer) return;
        
        GUILayout.BeginVertical("box");
        GUILayout.Label($"Local Player: {Object.InputAuthority}");
        GUILayout.Label($"Position: {transform.position}");
        GUILayout.Label($"Network Position: {NetworkPosition}");
        GUILayout.Label($"Has Input Authority: {Object.HasInputAuthority}");
        GUILayout.EndVertical();
    }
}
