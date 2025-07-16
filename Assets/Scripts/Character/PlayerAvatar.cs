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

    [SerializeField][Header("Layers")]
    private int _localPlayerLayer = 8;

    // ローカルプレイヤーのみが使用する変数
    private bool _isLocalPlayer;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Camera.main.cullingMask &= ~(1 << _localPlayerLayer);

            _playerAvatarView.SetCamera(); // ビューコンポーネントにカメラを設定
            _avatarMeshRenderer?.SetLayer(_localPlayerLayer, true);
        }
        else
        {
            _armMeshRenderer?.SetActive(false); // 他のプレイヤーのアバターメッシュを有効化
        }


        Debug.Log("ローカルプレイヤーとしてスポーンされました");
    }
}
