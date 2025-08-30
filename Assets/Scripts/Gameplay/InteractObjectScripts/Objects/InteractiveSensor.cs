using Core.Utils;
using Fusion;
using System;
using UnityEngine;

public class InteractiveSensor : BaseInteractButtonObject
{
    [SerializeField, Required]
    private NetworkMecanimAnimator _animator; // スイッチのアニメーター
    [SerializeField]
    private LayerMask _playerLayer = 1; // 地面レイヤー
    [SerializeField]
    private Vector3 _checkSnesorRange = new Vector3(0.3f, 0.1f, 0.3f);
    [SerializeField]
    private bool _autoClose = true; // プレイヤーが範囲外に出た時に自動で閉じるか

    private readonly Collider[] _detectedPlayers = new Collider[4]; // 検知されたプレイヤー配列

    [Networked, OnChangedRender(nameof(DoAction))]
    public bool IsActive { private set; get; }

    private const string MOVE_ON_PARAM = "MoveOn"; // アニメーションパラメータ名



    public override void FixedUpdateNetwork()
    {
       Physics.BoxCastNonAlloc(
            transform.position,
            _checkSnesorRange,
            Vector3.forward,
            Array.Empty<RaycastHit>(),
            Quaternion.identity,
            0.1f,
            _playerLayer
        );
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
        _animator.SetTrigger(MOVE_ON_PARAM);
    }
}
