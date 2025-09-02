using Fusion;
using UnityEngine;

public class GunEmplacementController : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 50f; // 回転速度（度/秒）
    [SerializeField] private float minPitch = -30f; // 垂直回転の最小角度
    [SerializeField] private float maxPitch = 60f; // 垂直回転の最大角度
    [SerializeField] private float minYaw = -90f; // 水平回転の最小角度
    [SerializeField] private float maxYaw = 90f; // 水平回転の最大角度

    // 初期状態と累積回転の管理
    private Quaternion _initialRotation;
    private float _currentPitch = 0f; // 初期位置からの累積ピッチ
    private float _currentYaw = 0f;   // 初期位置からの累積ヨー
    private bool _isInitialized = false;

    /// <summary>
    /// 砲塔操作開始時の初期化
    /// </summary>
    private void Start()
    {
        _initialRotation = transform.localRotation;
        _currentPitch = 0f;
        _currentYaw = 0f;
        _isInitialized = true;

        Debug.Log($"砲塔初期化 - 初期回転: {_initialRotation.eulerAngles}");
    }

    /// <summary>
    /// 砲塔操作終了時のリセット
    /// </summary>
    public void ResetInitialization()
    {
        _currentPitch = 0f;
        _currentYaw = 0f;
    }

    public void DoRotation(PlayerNetworkInput input)
    {
        // 初期化されていない場合は処理しない
        if (!_isInitialized) return;

        // 入力値のチェック
        if (input.LookInput.magnitude < 0.001f) return;

        // 時間に基づいた回転量を計算
        float deltaTime = Time.fixedDeltaTime;

        // 入力を回転角度の変化量に変換
        float pitchDelta = -input.LookInput.y * rotationSpeed * deltaTime;
        float yawDelta = input.LookInput.x * rotationSpeed * deltaTime;

        // 新しい累積角度を計算
        float newPitch = _currentPitch + pitchDelta;
        float newYaw = _currentYaw + yawDelta;

        // 詳細なデバッグ情報（制限前）
        Debug.Log($"制限前 - Input: {input.LookInput:F3}, Delta: P={pitchDelta:F3}/Y={yawDelta:F3}, Current: P={_currentPitch:F2}/Y={_currentYaw:F2}, New: P={newPitch:F2}/Y={newYaw:F2}");

        // 回転制限を適用
        float clampedPitch = Mathf.Clamp(newPitch, minPitch, maxPitch);
        float clampedYaw = Mathf.Clamp(newYaw, minYaw, maxYaw);

        // 制限が適用されたかをログ出力
        if (Mathf.Abs(clampedPitch - newPitch) > 0.001f)
        {
            Debug.LogWarning($"ピッチ制限適用: {newPitch:F2} → {clampedPitch:F2} (範囲: {minPitch}～{maxPitch})");
        }
        if (Mathf.Abs(clampedYaw - newYaw) > 0.001f)
        {
            Debug.LogWarning($"ヨー制限適用: {newYaw:F2} → {clampedYaw:F2} (範囲: {minYaw}～{maxYaw})");
        }

        // 制限された値を保存
        _currentPitch = clampedPitch;
        _currentYaw = clampedYaw;

        // **修正**: より確実な回転制限の適用方法
        // オイラー角での直接制御に変更
        Vector3 currentEuler = _initialRotation.eulerAngles;
        
        // 初期回転に相対的な回転を追加
        Vector3 targetEuler = new Vector3(
            currentEuler.x + _currentPitch,
            currentEuler.y + _currentYaw,
            currentEuler.z
        );

        // 角度の正規化（-180～180の範囲に調整）
        targetEuler.x = NormalizeAngle(targetEuler.x);
        targetEuler.y = NormalizeAngle(targetEuler.y);

        // 回転を適用
        transform.localRotation = Quaternion.Euler(targetEuler);

        // 最終結果をログ出力
        Vector3 finalEuler = transform.localRotation.eulerAngles;
        float displayPitch = NormalizeAngle(finalEuler.x);
        float displayYaw = NormalizeAngle(finalEuler.y);
        
        Debug.Log($"累積角度: P={_currentPitch:F2}/Y={_currentYaw:F2}, 実際の表示角度: P={displayPitch:F2}/Y={displayYaw:F2}");
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
