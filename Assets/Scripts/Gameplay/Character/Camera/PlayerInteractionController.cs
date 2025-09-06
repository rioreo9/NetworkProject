using Fusion;

using UnityEngine;

/// <summary>
/// プレイヤーインタラクション制御専用クラス
/// オブジェクトとのインタラクション処理を担当
/// </summary>
public class PlayerInteractionController : NetworkBehaviour
{
    [Header("インタラクト設定")]
    [SerializeField] private LayerMask _interactableLayerMask;
    [SerializeField] private float _interactRange = 5.0f;
    [SerializeField] private PlayerStatus _playerStatus; // プレイヤーステータス

    private readonly RaycastHit[] _raycastResults = new RaycastHit[8]; // GC削減用配列

    private Transform _cameraTransform;

    private PlayerCameraController _cameraController;
    private PlayerToolController _toolManager;
    private CenterReticleUI _centerReticleUI;

    private Collider _currentMarkedCollider = null;

    /// <summary>
    /// 初期化処理
    /// </summary>
    public void Initialize(PlayerCameraController cameraController, PlayerToolController toolManager)
    {
        _cameraController = cameraController;
        _toolManager = toolManager;
        _cameraTransform = _cameraController.GetCameraTransform();

        _centerReticleUI = FindFirstObjectByType<CenterReticleUI>();
    }

    /// <summary>
    /// インタラクション処理
    /// </summary>
    public void HandleInteraction()
    {
        // 現在持っているツールを使用
        if (_toolManager.CurrentTool != null)
        {
            UseCurrentTool();
        }

        // 新しいインタラクション対象を検索
        ProcessInteractionTargets();
    }

    /// <summary>
    /// 現在持っているツールを使用
    /// </summary>
    private void UseCurrentTool()
    {
        if (_cameraTransform == null) return;

        // ツール専用のレイキャスト処理
        if (Physics.Raycast(
            _cameraTransform.position,
            _cameraTransform.forward,
            out RaycastHit hit,
            _interactRange,
            _toolManager.CurrentTool.LayerMask))
        {
            _toolManager.UseTool(hit);
        }
    }

    /// <summary>
    /// インタラクション対象の検索と処理
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
        if (tool.IsInteractable) return false;

        _toolManager.PickUpTool(tool, hit);
        return true;
    }

    /// <summary>
    /// 砲台タイプのインタラクション処理
    /// </summary>
    private bool TryInteractWithGunEmplacement(Collider collider)
    {
        if(!collider.TryGetComponent(out IInteractableControllable interactableControllable)) return false;

        Debug.Log("砲台にアクセス");
        Debug.Log($"IsInteractable: {interactableControllable.IsInteractable}");
        if (interactableControllable.IsInteractable)
        {
            Debug.Log("操作可能");
            interactableControllable.Object?.RequestStateAuthority();
            interactableControllable.AccesObject(Object.InputAuthority, _playerStatus);
        }
        return true;
    }

    public void ProbeInteractionTarget()
    {
        if (_centerReticleUI == null) return;

        if (_cameraTransform == null) return;

        // 距離制限付きレイキャスト（GC削減のため配列を再利用）
        int hitCount = Physics.RaycastNonAlloc(
            _cameraTransform.position,
            _cameraTransform.forward,
            _raycastResults,
            _interactRange,
            _interactableLayerMask
        );

        if (hitCount == 0)
        {
            _centerReticleUI.GetReticleIcon(null);
            _currentMarkedCollider = null;
            return;
        }

        // 最も近いヒットを処理
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _raycastResults[i];
            if (hit.collider == null) continue;
            if (hit.collider == _currentMarkedCollider) continue;
            // インタラクションタイプ別処理

            TryGetMarkFromHit(hit.collider);
        }
    }

    /// <summary>
    /// ヒットからマークを取得
    /// </summary>
    /// <param name="hit"></param>
    private void TryGetMarkFromHit(Collider hit)
    {
        if (hit == null) return;

        if (hit.TryGetComponent<BaseInteractButtonObject>(out BaseInteractButtonObject button))
        {
            _centerReticleUI.GetReticleIcon(button);
            _currentMarkedCollider = hit;
        }
        else if (hit.TryGetComponent<BaseInteractMonitor>(out BaseInteractMonitor cont))
        {
            _centerReticleUI.GetReticleIcon(cont);
            _currentMarkedCollider = hit;
        }
        else if (hit.TryGetComponent<BasePickUpToolObject>(out BasePickUpToolObject tool))
        {
            _centerReticleUI.GetReticleIcon(tool);
            _currentMarkedCollider = hit;
        }
    }
}
