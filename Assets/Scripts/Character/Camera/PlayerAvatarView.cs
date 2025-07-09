
using Fusion;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーアバターのカメラビュー制御
/// FPS視点でのカメラ回転とネットワーク同期を担当
/// カメラのみで上下左右の視点制御を実現
/// </summary>
public class PlayerAvatarView : NetworkBehaviour
{
    [Header("カメラ参照")]
    [SerializeField, Required]
    private CinemachineCamera _cinemachineCamera; // Cinemachineカメラの参照

    [Header("カメラ設定")]
    [SerializeField] private float _mouseSensitivity = 100f; // マウス感度
    [SerializeField] private float _gamepadSensitivity = 50f; // ゲームパッド感度（将来対応用）
    [SerializeField] private float _maxLookAngle = 80f; // 上下視点制限角度
    [SerializeField] private bool _invertYAxis = false; // Y軸反転オプション

    // ネットワーク同期される累積回転角度
    private float _networkedXRotation;
    private float _networkedYRotation;

    /// <summary>
    /// カメラの優先度を設定してアクティブ化
    /// ローカルプレイヤーのスポーン時に呼び出される
    /// </summary>
    public void SetCamera()
    {
        _cinemachineCamera.Priority.Value = 100; // 優先度を最高に設定
        Debug.Log("FPSカメラがアクティブ化されました");
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

        // カメラの上下左右回転を処理
        ProcessCameraRotation(input.LookInput);
    }

    /// <summary>
    /// レンダリング更新処理
    /// ネットワーク同期されたカメラ回転を全クライアントに適用
    /// </summary>
    public override void Render()
    {
        // すべてのクライアントでカメラ回転を適用
        ApplyCameraRotation();
    }

    /// <summary>
    /// カメラの上下左右回転を統合処理
    /// マウス入力に基づいてカメラの視点を制御
    /// </summary>
    /// <param name="lookInput">マウス入力ベクトル（x=左右, y=上下）</param>
    private void ProcessCameraRotation(Vector2 lookInput)
    {
        // 入力がない場合は処理をスキップ
        if (lookInput.magnitude < 0.001f) return;

        // 上下回転（ピッチ）を処理
        ProcessVerticalRotation(lookInput.y);

        // 左右回転（ヨー）を処理
        ProcessHorizontalRotation(lookInput.x);
    }

    /// <summary>
    /// カメラの垂直回転（上下視点）を処理
    /// X軸回転でピッチを制御
    /// </summary>
    /// <param name="mouseYInput">マウスのY軸入力値</param>
    private void ProcessVerticalRotation(float mouseYInput)
    {
        // マウス入力に感度と時間を適用
        float rotationDelta = mouseYInput * _mouseSensitivity * Runner.DeltaTime;

        // Y軸反転設定を適用
        if (!_invertYAxis)
        {
            rotationDelta = -rotationDelta; // デフォルトでは反転
        }

        // 累積回転角度を更新
        _networkedXRotation += rotationDelta;

        // 角度制限を適用（上下視点の制限）
        _networkedXRotation = Mathf.Clamp(_networkedXRotation, -_maxLookAngle, _maxLookAngle);
    }

    /// <summary>
    /// カメラの水平回転（左右視点）を処理
    /// Y軸回転でヨーを制御
    /// </summary>
    /// <param name="mouseXInput">マウスのX軸入力値</param>
    private void ProcessHorizontalRotation(float mouseXInput)
    {
        // マウス入力に感度と時間を適用
        float rotationDelta = mouseXInput * _mouseSensitivity * Runner.DeltaTime;

        // 累積回転角度を更新（Y軸回転）
        _networkedYRotation += rotationDelta;

        // Y軸回転は360度循環するため制限なし
        // 必要に応じて0-360度の範囲に正規化
        _networkedYRotation = NormalizeAngle(_networkedYRotation);
    }

    /// <summary>
    /// 計算された回転をカメラに適用
    /// 上下左右の回転を統合してカメラに設定
    /// </summary>
    private void ApplyCameraRotation()
    {
        if (_cinemachineCamera == null) return;

        // カメラの回転を設定（X軸=上下, Y軸=左右）
        Quaternion targetRotation = Quaternion.Euler(_networkedXRotation, _networkedYRotation, 0f);
        _cinemachineCamera.transform.localRotation = targetRotation;
    }

    /// <summary>
    /// 角度を0-360度の範囲に正規化
    /// Y軸回転の無限回転に対応
    /// </summary>
    /// <param name="angle">正規化する角度</param>
    /// <returns>正規化された角度</returns>
    private float NormalizeAngle(float angle)
    {
        while (angle > 360f) angle -= 360f;
        while (angle < 0f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// マウス感度を動的に変更
    /// 設定画面などから呼び出される
    /// </summary>
    /// <param name="sensitivity">新しい感度値</param>
    public void SetMouseSensitivity(float sensitivity)
    {
        _mouseSensitivity = Mathf.Max(0.1f, sensitivity); // 最小値を設定
        Debug.Log($"マウス感度が{_mouseSensitivity}に設定されました");
    }

    /// <summary>
    /// Y軸反転設定を変更
    /// プレイヤーの好みに応じて上下操作を反転
    /// </summary>
    /// <param name="invert">反転するかどうか</param>
    public void SetInvertYAxis(bool invert)
    {
        _invertYAxis = invert;
        Debug.Log($"Y軸反転設定: {(_invertYAxis ? "ON" : "OFF")}");
    }
}
