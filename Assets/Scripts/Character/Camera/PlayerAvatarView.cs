
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
    private CinemachineCamera _cinemachineCamera; // Cinemachineカメラの参照
    private Transform _cameraTransform; // カメラのTransform

    [SerializeField, Required]
    private Transform _followTarget; // Cinemachineカメラの追跡ターゲット

    [SerializeField, Required]
    private Transform _localHandParent; // ローカル手の親オブジェクト（武器やアイテムの持ち手）
    [SerializeField, Required]
    private Transform _networkHandParent; // ネットワーク手の親オブジェクト

    [Header("カメラ設定")]
    [SerializeField] private float _mouseSensitivity = 2.0f; // マウス感度
    [SerializeField] private float _maxLookAngle = 80f; // 上下視点制限角度
    [SerializeField] private bool _invertYAxis = false; // Y軸反転オプション

    [Header("インタラクト設定")]
    [SerializeField] private LayerMask _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク
    [SerializeField] private float _interactRange = 5.0f; // インタラクト可能距離

    // カメラ回転状態
    [Networked] private float _verticalRotation { get; set; }
    private float _horizontalRotation;

    // インタラクション管理
    private IInteractableTool _currentTool;
    private readonly RaycastHit[] _raycastResults = new RaycastHit[8]; // GC削減用配列

    /// <summary>
    /// カメラの初期化処理
    /// ローカルプレイヤーのスポーン時に呼び出される
    /// </summary>
    public void SetCamera()
    {
        _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        
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

        // カーソル設定
        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        // 初期回転値を設定
        _horizontalRotation = transform.eulerAngles.y;
        _verticalRotation = 0f;
    }

    /// <summary>
    /// ネットワーク対応の固定更新処理
    /// 入力権限を持つプレイヤーのみカメラ回転とインタラクション処理を実行
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // 入力権限がない場合は処理をスキップ
        if (!Object.HasInputAuthority) return;

        // ネットワーク入力を取得
        if (!GetInput(out PlayerNetworkInput input)) return;

        // カメラ回転処理
        HandleCameraRotation(input);

        // インタラクション処理
        if (input.InteractPressed.IsSet(MyButtons.Interact))
        {
            HandleInteraction();
        }
    }

    /// <summary>
    /// カメラ回転処理
    /// マウス/コントローラー入力に基づいてカメラを回転させる
    /// </summary>
    private void HandleCameraRotation(PlayerNetworkInput input)
    {
        if (_cameraTransform == null) return;

        Vector2 lookInput = input.LookInput;

        // Y軸反転設定を適用
        if (_invertYAxis)
        {
            lookInput.y = -lookInput.y;
        }

        // 水平回転（Y軸）
        _horizontalRotation += lookInput.x * _mouseSensitivity;

        // 垂直回転（X軸）- ネットワーク同期される値
        _verticalRotation = Mathf.Clamp(_verticalRotation - lookInput.y * _mouseSensitivity, -_maxLookAngle, _maxLookAngle);

        // カメラに回転を適用
        _followTarget.rotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);
    }

    /// <summary>
    /// インタラクション処理のメイン処理
    /// ツール使用とインタラクション対象の検出を行う
    /// </summary>
    private void HandleInteraction()
    {
        // 現在持っているツールを使用
        if (_currentTool != null)
        {
            UseTool();
        }

        // 新しいインタラクション対象を検索
        ProcessInteractionTargets();
    }

    /// <summary>
    /// インタラクション対象の検索と処理
    /// 最適化されたレイキャスト処理
    /// </summary>
    private void ProcessInteractionTargets()
    {
        if (_cameraTransform == null) return;

        // 距離制限付きレイキャスト（GC削減のため配列を再利用）
        int hitCount = Physics.RaycastNonAlloc(
            _cameraTransform.position, 
            _cameraTransform.forward, 
            _raycastResults, 
            _interactRange, 
            _interactableLayerMask
        );

        // 最も近いヒットを処理
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastResults[i];
            if (hit.collider == null) continue;

            // インタラクションタイプ別処理
            if (TryInteractWithButton(hit.collider)) break;
            if (TryInteractWithTool(hit)) break;
            if (TryInteractWithGunEmplacement(hit.collider)) break;
        }
    }

    /// <summary>
    /// ボタンタイプのインタラクション処理
    /// </summary>
    private bool TryInteractWithButton(Collider collider)
    {
        if (!collider.TryGetComponent<IInteractableButton>(out IInteractableButton button)) 
            return false;

        if (!button.IsInteractable) return false;

        button.PushButton();
        return true;
    }

    /// <summary>
    /// ツールタイプのインタラクション処理
    /// </summary>
    private bool TryInteractWithTool(RaycastHit hit)
    {
        if (!hit.collider.TryGetComponent<IInteractableTool>(out IInteractableTool tool)) return false;
        if (tool.CheckInteractable()) return false; // ツールがインタラクト可能でない場合は処理を中止

        //元々所持しているツールがある場合は、インタラクト不可状態に設定
        _currentTool?.RPC_SetInteractable(false); // インタラクト不可状態を設定
        _currentTool = tool;
        //現在のツールがインタラクト可能状態に設定
        _currentTool?.RPC_SetInteractable(true); // インタラクト可能状態を設定

        RPC_PickUpItem(hit.collider.GetComponent<NetworkObject>());

        if (_currentTool.CheckCopyObject()) return true;

        CreateToolCopy(hit.collider.gameObject);
        return true;
    }

    /// <summary>
    /// ツールのコピーオブジェクト生成処理
    /// </summary>
    private void CreateToolCopy(GameObject originalTool)
    {
        if (_localHandParent == null || _networkHandParent == null) return;

        GameObject copyObj = Instantiate(originalTool, _networkHandParent.position, Quaternion.identity);
        _currentTool.SetCopyObj(copyObj);

        // ローカルプレイヤー用の位置設定
        copyObj.transform.position = _localHandParent.position;
        copyObj.transform.SetParent(_localHandParent, true);
    }

    /// <summary>
    /// 砲台タイプのインタラクション処理
    /// </summary>
    private bool TryInteractWithGunEmplacement(Collider collider)
    {
        if (!collider.TryGetComponent(out GunEmplacementController gunEmplacementController)) 
            return false;

        gunEmplacementController.Object.RequestStateAuthority();
        gunEmplacementController.SetPlayerRef(Object.InputAuthority);
        return true;
    }

    /// <summary>
    /// アイテムピックアップのネットワークRPC処理
    /// ネットワーク上の他のプレイヤーに対してアイテムの位置を同期
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PickUpItem(NetworkObject networkObject)
    {
        if (networkObject == null || _networkHandParent == null) return;

        var tool = networkObject.GetComponent<IInteractableTool>();
        if (tool != null)
        {
            _currentTool = tool;
        }

        // リモートプレイヤー（他のプレイヤー）用の処理
        networkObject.transform.position = _networkHandParent.position;
        networkObject.transform.SetParent(_networkHandParent, true);
    }

    /// <summary>
    /// 現在持っているツールを使用する処理
    /// 最適化されたレイキャスト処理を使用
    /// </summary>
    private void UseTool()
    {
        if (_currentTool == null || _cameraTransform == null) return;

        // ツール専用のレイキャスト処理（距離制限付き）
        if (Physics.Raycast(
            _cameraTransform.position, 
            _cameraTransform.forward, 
            out RaycastHit hit, 
            _interactRange, 
            _currentTool.layerMask))
        {
            _currentTool.CheckInteractableObject(hit);
        }
    }

    /// <summary>
    /// ツールを手放す処理
    /// </summary>
    public void DropTool()
    {
        _currentTool = null;
    }

    /// <summary>
    /// カメラ感度を動的に変更する処理
    /// </summary>
    public void SetMouseSensitivity(float sensitivity)
    {
        _mouseSensitivity = Mathf.Clamp(sensitivity, 0.1f, 10f);
    }

    /// <summary>
    /// Y軸反転設定を変更する処理
    /// </summary>
    public void SetInvertYAxis(bool invert)
    {
        _invertYAxis = invert;
    }
}
