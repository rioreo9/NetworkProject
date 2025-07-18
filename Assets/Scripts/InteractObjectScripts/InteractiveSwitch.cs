using Fusion;
using UnityEngine;

public class InteractiveSwitch : BaseInteractButtonObject
{
    [SerializeField, Required]
    private NetworkMecanimAnimator _animator; // スイッチのアニメーター

    [SerializeField, Required]
    private WaveSpawner _waveSpawner; // ウェーブスポーン管理

    [Networked, OnChangedRender(nameof(DoAction))]
    public bool IsActive { private set; get; }

    private const string MOVE_ON_PARAM = "MoveOn"; // アニメーションパラメータ名

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

        _waveSpawner.SpawnEnemy();

        Debug.Log($"スイッチ状態変更: {IsActive}");
    }

    private void DoAction()
    {
        _animator.SetTrigger(MOVE_ON_PARAM);
    }
}
