using Fusion;
using UnityEngine;

public class InteractiveSwitch : BaseInteractButtonObject
{
    [SerializeField, Required]
    private NetworkMecanimAnimator _animator; // スイッチのアニメーター

    [Networked, OnChangedRender(nameof(DoAction))]
    public bool IsActive { private set; get; }

    /// ChangeDetectorを使用して、アニメーションの更新を検出する
    private ChangeDetector _changeDetector;

    /// <summary>
    /// ボタンを押したときに呼び出されるメソッド
    /// </summary>
    public override void PushButton()
    {
        // 権限チェック：サーバーまたは入力権限を持つクライアント
        if (Object.HasStateAuthority)
        {
            // サーバーは直接変更可能
            ToggleSwitch();
        }
        else
        {
            // クライアントはRPCでリクエスト
            RPC_RequestToggle();
        }
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
        _animator.SetTrigger("MoveOn");
    }
}
