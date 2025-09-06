using Fusion;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// プレイヤーカメラ制御専用クラス
/// FPS視点でのカメラ回転とネットワーク同期を担当
/// </summary>
public class PlayerCameraController : NetworkBehaviour
{
    // カメラ回転状態（ネットワーク同期）
    [Networked] public float VerticalRotation { get; private set; }

    private CinemachineCamera _cinemachineCamera;
    private Transform _cameraTransform;

    [SerializeField, Required]
    private Transform _followTarget;

    [Header("カメラ設定")]
    [SerializeField] private float _mouseSensitivity = 2.0f;
    [SerializeField] private float _maxLookAngle = 80f;
    [SerializeField] private bool _invertYAxis = false;

    private float _horizontalRotation;

    /// <summary>
    /// カメラの初期化処理
    /// </summary>
    public void InitializeCamera()
    {
        _cinemachineCamera = GameObject.FindGameObjectWithTag("Camera")
            ?.GetComponent<CinemachineCamera>();


        if (_cinemachineCamera == null)
        {
            Debug.LogError("CinemachineCamera が見つかりません");
            return;
        }

        if (_followTarget == null)
        {
            Debug.LogError("Follow Target が設定されていません");
            return;
        }

        _cinemachineCamera.Target.TrackingTarget = _followTarget;
        _cameraTransform = Camera.main?.transform;

        if (_cameraTransform == null)
        {
            Debug.LogError("メインカメラが見つかりません");
            return;
        }

        // 初期回転値を設定
        _horizontalRotation = transform.eulerAngles.y;
        VerticalRotation = 0f;
    }

    /// <summary>
    /// カメラ回転処理
    /// </summary>
    public void HandleCameraRotation(Vector2 lookInput)
    {
        if (_cameraTransform == null) return;

        // Y軸反転設定を適用
        if (_invertYAxis)
        {
            lookInput.y = -lookInput.y;
        }

        // 水平回転（Y軸）
        _horizontalRotation += lookInput.x * _mouseSensitivity;

        // 垂直回転（X軸）- ネットワーク同期される値
        VerticalRotation = Mathf.Clamp(VerticalRotation - lookInput.y * _mouseSensitivity, -_maxLookAngle, _maxLookAngle);

        // カメラに回転を適用
        _followTarget.rotation = Quaternion.Euler(VerticalRotation, _horizontalRotation, 0f);
    }

    /// <summary>
    /// カメラ感度を動的に変更
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        _mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }

    /// <summary>
    /// Y軸反転設定を変更
    /// </summary>
    public void SetInvertYAxis(bool invert)
    {
        _invertYAxis = invert;
    }

    /// <summary>
    /// カメラのTransformを取得
    /// </summary>
    public Transform GetCameraTransform() => _cameraTransform;
}
