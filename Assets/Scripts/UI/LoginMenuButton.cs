using System;
using Core.Utils;
using UnityEngine;
using Fusion;
using Unity.Cinemachine;
using R3;
using UnityEngine.UI;

public class LoginMenuButton : NetworkBehaviour
{
    [Networked, OnChangedRender(nameof(UpDateButton))]
    public bool IsLoginButtonEnabled { get; private set; } = true;

    [SerializeField, Required]
    private Button _loginButton;
    [SerializeField, Required]
    private Button _logoutButton;

    [SerializeField]
    private int _loginButtonPriority = 2;

    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera;

    public override void Spawned()
    {
        if (_cinemachineCamera == null || _loginButton == null || _logoutButton == null)
        {
            Debug.LogError("ものが設定されていません");
            return;
        }
        _logoutButton.interactable = false; // 初期状態ではログアウトボタンは無効

        _loginButton?.OnClickAsObservable()
            .Subscribe(_ => OnLoginButtonClicked())
            .AddTo(this);

        _logoutButton?.OnClickAsObservable()
            .Subscribe(_ => OnLogoutButtonClicked())
            .AddTo(this);    
    }

    private void OnLoginButtonClicked()
    {
        if (!IsLoginButtonEnabled) return;

        _cinemachineCamera.Priority = _loginButtonPriority; // ログインメニュー用の優先度を設定
        _logoutButton.interactable = true; // ログアウトボタンを有効化

        NetworkAuthorityHelper.ExecuteWithAuthority(
            this,
            directAction : RequestLogin,
            rpcAction : RPC_RequestLogin
            );
    }
    private void RequestLogin()
    {
        IsLoginButtonEnabled = false;
        _loginButton.interactable = false; // ログインボタンを無効化
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestLogin()
    {
        RequestLogin();
    }

    private void OnLogoutButtonClicked()
    {
        if (IsLoginButtonEnabled) return;

        // ログアウト処理をここに実装
        _cinemachineCamera.Priority = 0; // ログアウト時は優先度をリセット
        _logoutButton.interactable = false; // ログアウトボタンを有効化

        NetworkAuthorityHelper.ExecuteWithAuthority(
           this,
           directAction: RequestLogout,
           rpcAction: RPC_RequestLogout
           );
    }

    private void RequestLogout()
    {
        IsLoginButtonEnabled = true;
        _loginButton.interactable = true; // ログインボタンを再度有効化
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestLogout()
    {
        RequestLogout();
    }

    private void UpDateButton()
    {
        _loginButton.interactable = IsLoginButtonEnabled;
    }

}
