using System;
using Core.Utils;
using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using R3;
using UnityEngine.UI;
using System.Runtime.CompilerServices;

public class LoginMenuButton : BaseInteractContObject
{
    [SerializeField, Required]
    private Button _loginButton;
    [SerializeField, Required]
    private Button _logoutButton;

    [SerializeField]
    private int _loginButtonPriority = 2;

    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera;

    private INoticePlayerInteract _playerInteractNotice;

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

    public override void Access(INoticePlayerInteract notice)
    {
        if (HasInteractor) return;

        _playerInteractNotice = notice;

        SetAcceseStateLocal(true);

        NetworkAuthorityHelper.ExecuteWithAuthority(
             this,
             directAction: RequestAccess,
             rpcAction: RPC_RequestAccess
             );
    }

    private void OnLoginButtonClicked()
    {

    }
    private void RequestAccess()
    {
      SetAcceseStateRemote(true); // リモート側のインタラクション状態を設定
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestAccess()
    {
        RequestAccess();
    }

    private void OnLogoutButtonClicked()
    {
        SetAcceseStateLocal(false);

        NetworkAuthorityHelper.ExecuteWithAuthority(
           this,
           directAction: RequestLogout,
           rpcAction: RPC_RequestLogout
           );
    }

    private void RequestLogout()
    {
        SetAcceseStateRemote(false); // リモート側のインタラクション状態を設定
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestLogout()
    {
        RequestLogout();
    }

    private void SetAcceseStateLocal(bool isAccese)
    {
        _loginButton.interactable = isAccese;// ログインボタンを有効化
        _logoutButton.interactable = isAccese; // ログアウトボタンを有効化
        _playerInteractNotice?.RPC_SetControllerInteracting(isAccese); // インタラクション状態を設定

        _cinemachineCamera.Priority = isAccese ? _loginButtonPriority : 0; // ログイン時は優先度を設定、ログアウト時はリセット
    }

    private void SetAcceseStateRemote(bool isAccese)
    {
        HasInteractor = isAccese; // インタラクション変更
    }
}
