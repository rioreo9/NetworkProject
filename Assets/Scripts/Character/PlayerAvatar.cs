using Fusion;
using Unity.Cinemachine;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// ネットワーク対応プレイヤーアバター
/// オンライン環境でのマルチプレイヤー移動を実現
/// </summary>
public class PlayerAvatar : NetworkBehaviour
{
    [SerializeField, Required]
    private PlayerAvatarView _playerAvatarView; // プレイヤーアバタービューコンポーネント

    private ISetPlayerInformation _movement; // 移動コンポーネント

    // ローカルプレイヤーのみが使用する変数
    private bool _isLocalPlayer;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            _playerAvatarView.SetCamera(); // ビューコンポーネントにカメラを設定
        }

       
        Debug.Log("ローカルプレイヤーとしてスポーンされました");
    }
}
