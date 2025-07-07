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
    [SerializeField,Required]
    private CinemachineCamera _cinemachineCamera; // カメラコンポーネント

    private ISetPlayerInformation _view; // ビューコンポーネント
    private ISetPlayerInformation _movement; // 移動コンポーネント

    // ローカルプレイヤーのみが使用する変数
    private bool _isLocalPlayer;

    public override void Spawned()
    {
        // コンポーネント取得
        _view = GetComponent<PlayerAvatarView>();
        _movement = GetComponent<PlayerMovement>();


        _view?.SetCamera(_cinemachineCamera); // カメラターゲットに設定
        _movement?.SetCamera(_cinemachineCamera); // 移動コンポーネントにカメラを設定
        Debug.Log("ローカルプレイヤーとしてスポーンされました");
    }
}
