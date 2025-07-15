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

    [SerializeField, Required]
    private GameObject _avatarMeshRenderer; // アバターメッシュレンダラー
    [SerializeField, Required]
    private GameObject _armMeshRenderer; // アバターのTransformコンポーネント

    private ISetPlayerInformation _movement; // 移動コンポーネント

    // ローカルプレイヤーのみが使用する変数
    private bool _isLocalPlayer;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            _playerAvatarView.SetCamera(); // ビューコンポーネントにカメラを設定
            _avatarMeshRenderer?.SetActive(false); // ローカルプレイヤーのアバターメッシュを有効化
        }
        else
        {
            _armMeshRenderer?.SetActive(false); // 他のプレイヤーのアバターメッシュを有効化
        }


        Debug.Log("ローカルプレイヤーとしてスポーンされました");
    }
}
