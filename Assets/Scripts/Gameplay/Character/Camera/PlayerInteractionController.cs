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

    private readonly RaycastHit[] _raycastResults = new RaycastHit[8]; // GC削減用配列

    private PlayerCameraController _cameraController;
    private PlayerToolController _toolManager;

    /// <summary>
    /// 初期化処理
    /// </summary>
    public void Initialize(PlayerCameraController cameraController, PlayerToolController toolManager)
    {
        _cameraController = cameraController;
        _toolManager = toolManager;
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
        Transform cameraTransform = _cameraController.GetCameraTransform();
        if (cameraTransform == null) return;

        // ツール専用のレイキャスト処理
        if (Physics.Raycast(
            cameraTransform.position,
            cameraTransform.forward,
            out RaycastHit hit,
            _interactRange,
            _toolManager.CurrentTool.layerMask))
        {
            _toolManager.UseTool(hit);
        }
    }

    /// <summary>
    /// インタラクション対象の検索と処理
    /// </summary>
    private void ProcessInteractionTargets()
    {
        Transform cameraTransform = _cameraController.GetCameraTransform();
        if (cameraTransform == null) return;

        // 距離制限付きレイキャスト（GC削減のため配列を再利用）
        int hitCount = Physics.RaycastNonAlloc(
            cameraTransform.position,
            cameraTransform.forward,
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
        if (!hit.collider.TryGetComponent<BasePickUpToolObject>(out BasePickUpToolObject tool)) return false;
        if (tool.IsInteractable) return false;

        _toolManager.PickUpTool(tool, hit);
        return true;
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
}
