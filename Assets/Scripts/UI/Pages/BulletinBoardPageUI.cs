using Core.Utils;
using Fusion;
using UnityEngine.UI;
using Unity.Cinemachine;
using UnityEngine;
using VitalRouter;
using R3;

public class BulletinBoardPageUI : BaseInteractMonitor
{
    [Header("戻る")]
    [SerializeField,Required]
    private Button _backButton;

    [Header("Cinemachine設定")]
    [SerializeField]
    private int _loginButtonPriority = 2;
    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera;

    private INoticePlayerInteract _playerInteractNotice;

    /// <summary>
    /// 必要な参照の検証とボタンクリック購読の初期化を行う。
    /// </summary>
    public override void Spawned()
    {
        _backButton?.OnClickAsObservable()
            .Subscribe(_ => OnLogoutButtonClicked())
            .AddTo(this);
    }

    /// <summary>
    /// プレイヤーのアクセス開始時に呼ばれる。ローカルUIを有効化し、権限に応じてリモート側も更新する。
    /// </summary>
    /// <param name="notice">アクセス通知インターフェース</param>
    public override void AccesObject(PlayerRef player, INoticePlayerInteract notice)
    {
        if (!IsInteractable) return;

        _playerInteractNotice = notice;

        SetAccessStateLocal(true);

        NetworkAuthorityHelper.ExecuteWithAuthority(
             this,
             directAction: RequestAccess,
             rpcAction: RPC_RequestAccess
             );
    }
    /// <summary>
    /// アクセス要求の実処理（StateAuthorityで実行）。
    /// </summary>
    private void RequestAccess()
    {
        SetAccessStateRemote(true); // リモート側のインタラクション状態を設定
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    /// <summary>
    /// アクセス要求のRPC。StateAuthorityで実処理を実行する。
    /// </summary>
    private void RPC_RequestAccess()
    {
        RequestAccess();
    }

    /// <summary>
    /// ログアウトボタン押下時の処理。ローカル無効化および権限に応じたリモート反映。
    /// </summary>
    private void OnLogoutButtonClicked()
    {
        SetAccessStateLocal(false);

        NetworkAuthorityHelper.ExecuteWithAuthority(
           this,
           directAction: RequestLogout,
           rpcAction: RPC_RequestLogout
           );
    }

    /// <summary>
    /// ログアウト要求の実処理（StateAuthorityで実行）。
    /// </summary>
    private void RequestLogout()
    {
        SetAccessStateRemote(false); // リモート側のインタラクション状態を設定
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    /// <summary>
    /// ログアウト要求のRPC。StateAuthorityで実処理を実行する。
    /// </summary>
    private void RPC_RequestLogout()
    {
        RequestLogout();
    }

    /// <summary>
    /// ローカル側のアクセス状態を切り替える（UI有効化とカメラ優先度）。
    /// </summary>
    /// <param name="isAccessActive">有効にするか</param>
    private void SetAccessStateLocal(bool isAccessActive)
    {
        _backButton.interactable = isAccessActive;// ログインボタンを有効化
        _playerInteractNotice?.RPC_SetControllerInteracting(isAccessActive); // インタラクション状態を設定

        _cinemachineCamera.Priority = isAccessActive ? _loginButtonPriority : 0; // ログイン時は優先度を設定、ログアウト時はリセット
    }

    /// <summary>
    /// リモート（共有）側のアクセス状態を切り替える。
    /// </summary>
    /// <param name="isAccese">有効にするか</param>
    private void SetAccessStateRemote(bool isAccese)
    {
        IsInteractable = !isAccese; // インタラクション変更
    }
}
