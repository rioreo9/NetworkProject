using UnityEngine;

public class GunEmplacementController : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 50f; // 回転速度（度/秒）
    [SerializeField] private float minPitch = -30f;     // 垂直回転の最小角度
    [SerializeField] private float maxPitch = 60f;      // 垂直回転の最大角度
    [SerializeField] private float minYaw = -90f;       // 水平回転の最小角度
    [SerializeField] private float maxYaw = 90f;        // 水平回転の最大角度

    [Header("Pivots (任意)")]
    [SerializeField] private Transform yawPivot;   // 水平回転用（通常は台座）
    [SerializeField] private Transform pitchPivot; // 垂直回転用（通常は砲身のピボット）
    [SerializeField] private bool invertYaw = false;   // 必要に応じて反転
    [SerializeField] private bool invertPitch = false; // 必要に応じて反転

    // 初期状態と累積回転の管理
    private Quaternion _initialRotation;
    private Quaternion _yawStartLocalRot;
    private Quaternion _pitchStartLocalRot;

    private float _currentPitch = 0f; // 初期位置からの累積ピッチ
    private float _currentYaw = 0f;   // 初期位置からの累積ヨー
    private bool _isInitialized = false;

    /// <summary>
    /// 砲塔操作開始時の初期化
    /// </summary>
    private void Start()
    {
        _initialRotation = transform.localRotation;

        if (yawPivot == null) yawPivot = transform;
        if (pitchPivot == null) pitchPivot = transform;

        _yawStartLocalRot = yawPivot.localRotation;
        _pitchStartLocalRot = pitchPivot.localRotation;

        _currentPitch = 0f;
        _currentYaw = 0f;
        _isInitialized = true;
    }

    /// <summary>
    /// 砲塔操作終了時のリセット
    /// </summary>
    public void ResetInitialization()
    {
        _currentPitch = 0f;
        _currentYaw = 0f;

        // 視覚的にも初期姿勢へ戻す
        if (yawPivot == pitchPivot)
        {
            transform.localRotation = _initialRotation;
        }
        else
        {
            yawPivot.localRotation = _yawStartLocalRot;
            pitchPivot.localRotation = _pitchStartLocalRot;
        }
    }

    public void DoRotation(PlayerNetworkInput input)
    {
        // 初期化されていない場合は処理しない
        if (!_isInitialized) return;

        // 入力値のチェック
        if (input.LookInput.magnitude < 0.001f) return;

        // 時間に基づいた回転量を計算
        float deltaTime = Time.fixedDeltaTime;

        // 方向反転設定（既存の「下へドラッグで俯角」を維持）
        float yawSign = invertYaw ? -1f : 1f;
        float pitchSign = invertPitch ? 1f : -1f;

        // 入力を回転角度の変化量に変換
        float pitchDelta = pitchSign * input.LookInput.y * rotationSpeed * deltaTime;
        float yawDelta = yawSign * input.LookInput.x * rotationSpeed * deltaTime;

        // 新しい累積角度を計算
        float newPitch = _currentPitch + pitchDelta;
        float newYaw = _currentYaw + yawDelta;

        // 回転制限を適用
        float clampedPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
        float clampedYaw = Mathf.Clamp(newYaw, minYaw, maxYaw);

        // 制限された値を保存
        _currentPitch = clampedPitch;
        _currentYaw = clampedYaw;

        // タレットのローカル軸を基準に回転を適用
        if (yawPivot == pitchPivot)
        {
            // 単一トランスフォームで両軸制御（ローカルUp/Right軸）
            transform.localRotation =
                _initialRotation *
                Quaternion.AngleAxis(_currentYaw, Vector3.up) *
                Quaternion.AngleAxis(_currentPitch, Vector3.right);
        }
        else
        {
            // 分離ピボット（台座=Yaw, 砲身=Pitch）
            yawPivot.localRotation = _yawStartLocalRot * Quaternion.AngleAxis(_currentYaw, Vector3.up);
            pitchPivot.localRotation = _pitchStartLocalRot * Quaternion.AngleAxis(_currentPitch, Vector3.right);
        }

        // 最終結果をログ出力（簡易）
        Vector3 finalPitchEuler = (pitchPivot ?? transform).localEulerAngles;
        Vector3 finalYawEuler = (yawPivot ?? transform).localEulerAngles;
    }

    /// <summary>
    /// 角度を-180～180の範囲に正規化
    /// </summary>
    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
