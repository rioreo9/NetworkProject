using System;
using Core.Utils;
using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using R3;
using UnityEngine.UI;
using VitalRouter;

/// <summary>
/// ログイン/ログアウトのインタラクトUIを管理し、VitalRouterでページ遷移を発行する。
/// プレイヤーのアクセス開始/終了に応じてUI有効化とCinemachine優先度を制御し、
/// Fusionの権限管理に従ってRPCでリモート状態も同期する。
/// </summary>
public class LoginMenuButton : BaseInteractContObject, IInjectPageRouter
{
    [Header("遷移先")]
    [SerializeField]
    private PageId _targetPageId = PageId.Shop;

    [Header("各ボタン")]
    [SerializeField, Required]
    private Button _loginButton;
    [SerializeField, Required]
    private Button _logoutButton;

    [Header("Cinemachine設定")]
    [SerializeField]
    private int _loginButtonPriority = 2;
    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera;

    private INoticePlayerInteract _playerInteractNotice;
    private ICommandPublisher _publisher;

    /// <summary>
    /// 必要な参照の検証とボタンクリック購読の初期化を行う。
    /// </summary>
    public override void Spawned()
    {
        if (_cinemachineCamera == null || _loginButton == null || _logoutButton == null)
        {
            Debug.LogError("必要なコンポーネント（CinemachineCamera、LoginButton、LogoutButton）が設定されていません");
            return;
        }
        _logoutButton.interactable = false; // 初期状態ではログアウトボタンは無効
        _loginButton.interactable = false; // 初期状態ではログインボタンは無効

        _loginButton?.OnClickAsObservable()
            .Subscribe(_ => OnLoginButtonClicked())
            .AddTo(this);

        _logoutButton?.OnClickAsObservable()
            .Subscribe(_ => OnLogoutButtonClicked())
            .AddTo(this);
    }

    /// <summary>
    /// ページ遷移コマンドを発行する Publisher を設定する。
    /// </summary>
    /// <param name="cmd">コマンド Publisher</param>
    public void SetNavigate(ICommandPublisher cmd)
    {
        _publisher = cmd;
    }

    /// <summary>
    /// プレイヤーのアクセス開始時に呼ばれる。ローカルUIを有効化し、権限に応じてリモート側も更新する。
    /// </summary>
    /// <param name="notice">アクセス通知インターフェース</param>
    public override void Access(INoticePlayerInteract notice)
    {
        if (HasInteractor) return;

        _playerInteractNotice = notice;

        SetAccessStateLocal(true);

        NetworkAuthorityHelper.ExecuteWithAuthority(
             this,
             directAction: RequestAccess,
             rpcAction: RPC_RequestAccess
             );
    }

    /// <summary>
    /// ログインボタン押下時の処理。対象ページへの遷移コマンドを発行する。
    /// </summary>
    private void OnLoginButtonClicked()
    {
        _publisher?.PublishAsync(new NavigateToPageCommand(_targetPageId));
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
        _loginButton.interactable = isAccessActive;// ログインボタンを有効化
        _logoutButton.interactable = isAccessActive; // ログアウトボタンを有効化
        _playerInteractNotice?.RPC_SetControllerInteracting(isAccessActive); // インタラクション状態を設定

        _cinemachineCamera.Priority = isAccessActive ? _loginButtonPriority : 0; // ログイン時は優先度を設定、ログアウト時はリセット
    }

    /// <summary>
    /// リモート（共有）側のアクセス状態を切り替える。
    /// </summary>
    /// <param name="isAccese">有効にするか</param>
    private void SetAccessStateRemote(bool isAccese)
    {
        HasInteractor = isAccese; // インタラクション変更
    }
}
