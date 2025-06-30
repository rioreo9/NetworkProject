using Fusion;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// ネットワーク対応プレイヤーアバター
/// オンライン環境でのマルチプレイヤー移動を実現
/// </summary>
public class PlayerAvatar : NetworkBehaviour
{
    private PlayerAvatarView _view; // ビューコンポーネント
    private PlayerMovement _movement; // 移動コンポーネント

    // ローカルプレイヤーのみが使用する変数
    private Camera _playerCamera;
    private bool _isLocalPlayer;

    public override void Spawned()
    {
        // コンポーネント取得
        _view = GetComponent<PlayerAvatarView>();
        _movement = GetComponent<PlayerMovement>();

        // ローカルプレイヤーかどうかを判定
        _isLocalPlayer = Object.HasInputAuthority;
        
        // ローカルプレイヤーの場合のみカメラ設定
        if (_isLocalPlayer)
        {
            _view.MakeCameraTarget(); // カメラターゲットに設定
            Debug.Log("ローカルプレイヤーとしてスポーンされました");
        }
        else
        {
            Debug.Log("リモートプレイヤーとしてスポーンされました");
        }
 
    }
}
