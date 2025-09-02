using Core.Utils;
using Fusion;
using System;
using UnityEngine;

public class InteractiveSensor : BaseInteractButtonObject
{
    [SerializeField, Required]
    private Animator _animator; // スイッチのアニメーター
    [SerializeField]
    private bool _autoClose = true; // プレイヤーが範囲外に出た時に自動で閉じるか
    [SerializeField]
    private float _autoCloseDelay = 2.0f;

    [Networked, OnChangedRender(nameof(DoAction))]
    public bool IsActive { private set; get; }

    [Networked]
    public int PlayersInRange { private set; get; }

    private TickTimer _autoCloseTimer;
    private const string MOVE_ON_PARAM = "isOpen"; // アニメーションパラメータ名



    // OnTriggerEnter/Exitを使用した軽量な実装
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (!other.CompareTag("Player")) return;

        PlayersInRange++;
        
        if (!IsActive)
        {
            ToggleSwitch();
        }
        
        if (_autoCloseTimer.IsRunning)
        {
            _autoCloseTimer = TickTimer.None;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit");

        if (!Object.HasStateAuthority) return;
        if (!other.CompareTag("Player")) return;

        PlayersInRange = Mathf.Max(0, PlayersInRange - 1);
        
        if (_autoClose && PlayersInRange == 0 && IsActive)
        {
            _autoCloseTimer = TickTimer.CreateFromSeconds(Runner, _autoCloseDelay);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        
        // 自動で閉じる処理のみ
        if (_autoCloseTimer.Expired(Runner))
        {
            ToggleSwitch();
            _autoCloseTimer = TickTimer.None;
        }
    }

    /// <summary>
    /// ボタンを押したときに呼び出されるメソッド
    /// </summary>
    public override void PushButton()
    {
        NetworkAuthorityHelper.ExecuteWithAuthority(
            this,
            directAction: ToggleSwitch,
            rpcAction: RPC_RequestToggle
        );
    }

    /// <summary>
    /// サーバーにスイッチ切り替えを要求するRPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestToggle()
    {
        ToggleSwitch();
    }

    /// <summary>
    /// スイッチ状態を切り替える（サーバー側のみ実行）
    /// </summary>
    private void ToggleSwitch()
    {
        // インタラクト可能かチェック
        if (!IsInteractable) return;

        IsActive = !IsActive;

        Debug.Log($"スイッチ状態変更: {IsActive}");
    }

    private void DoAction()
    {
        _animator.SetBool(MOVE_ON_PARAM, IsActive);
    }
}
