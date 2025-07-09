
using Fusion;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーアバターのカメラビュー制御
/// FPS視点でのカメラ回転とネットワーク同期を担当
/// </summary>
public class PlayerAvatarView : NetworkBehaviour
{
    [Header("カメラ参照")]
    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera; // Cinemachineカメラの参照

    [Header("カメラ設定")]
    [SerializeField] private float _mouseSensitivity = 100f; // マウス感度
    [SerializeField] private float _gamepadSensitivity = 50f; // ゲームパッド感度（未使用だが将来対応用）
    [SerializeField] private float _maxLookAngle = 80f; // 上下視点制限角度

    // ネットワーク同期される累積回転角度
    private float _networkedXRotation;

    /// <summary>
    /// カメラの優先度を設定してアクティブ化
    /// ローカルプレイヤーのスポーン時に呼び出される
    /// </summary>
    public void SetCamera()
    {
        _cinemachineCamera.Priority.Value = 100; // 優先度を最高に設定
    }

    /// <summary>
    /// ネットワーク対応の固定更新処理
    /// 入力権限を持つプレイヤーのみカメラ回転を処理
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // 入力権限がない場合は処理をスキップ
        if (!Object.HasInputAuthority) return;

        // ネットワーク入力を取得
        if (!GetInput(out PlayerNetworkInput input)) return;

        // カメラの垂直回転を処理
        ProcessVerticalRotation(input.LookInput.y);
    }

    /// <summary>
    /// カメラの垂直回転（上下視点）を処理
    /// </summary>
    /// <param name="mouseYInput">マウスのY軸入力値</param>
    private void ProcessVerticalRotation(float mouseYInput)
    {
        // マウス入力に感度と時間を適用
        float rotationDelta = mouseYInput * _mouseSensitivity * Runner.DeltaTime;

        // 累積回転角度を更新（Y軸は反転）
        _networkedXRotation -= rotationDelta;

        // 角度制限を適用
        _networkedXRotation = Mathf.Clamp(_networkedXRotation, -_maxLookAngle, _maxLookAngle);
        
        // カメラに回転を適用
        ApplyCameraRotation();
    }

    /// <summary>
    /// 計算された回転をカメラに適用
    /// </summary>
    private void ApplyCameraRotation()
    {
        if (_cinemachineCamera == null) return;

        // カメラのローカル回転を設定（X軸のみ）
        Quaternion targetRotation = Quaternion.Euler(_networkedXRotation, 0f, 0f);
        _cinemachineCamera.transform.localRotation = targetRotation;
    }
}
