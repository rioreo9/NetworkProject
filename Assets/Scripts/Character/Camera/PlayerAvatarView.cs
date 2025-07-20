
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

    [SerializeField, Required]
    private Transform _followTarget; // Cinemachineカメラの参照

    [Header("カメラ設定")]
  
    [SerializeField] private float _maxLookAngle = 80f; // 上下視点制限角度
    [SerializeField] private bool _invertYAxis = false; // Y軸反転オプション

    [Header("インタラクト設定")]
    [SerializeField] private LayerMask _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク

    public PlayerNetworkInput Input { get; private set; } // ネットワーク入力データ

    /// <summary>
    /// カメラの優先度を設定してアクティブ化
    /// ローカルプレイヤーのスポーン時に呼び出される
    /// </summary>
    public void SetCamera()
    {
        _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        _cinemachineCamera.Target.TrackingTarget = _followTarget; // カメラの追跡ターゲットを設定
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

        //Cursor.lockState = CursorLockMode.Locked; // カーソルを画面中央にロック
        //Cursor.visible = false; // カーソルを非表示にする

        // ネットワーク入力を取得
        if (!GetInput(out PlayerNetworkInput input)) return;

        if (input.InteractPressed.IsSet(MyButtons.Interact))
        {
            ProcessInteractAction();
            Input = input; // 入力データを保存
        }


       //インタラクトする処理をここに書きたい
    }

    private void ProcessInteractAction()
    {
        Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, Mathf.Infinity, _interactableLayerMask);
       
        if (hit.collider != null)
        {
            Debug.Log($"ヒットしたオブジェクト: {hit.collider.name}");
            // ヒットしたオブジェクトがインタラクト可能なオブジェクトかどうかをチェック
            IInteractableButton interactable = hit.collider.GetComponent<IInteractableButton>();
            if (interactable != null)
            {
                // インタラクト可能なオブジェクトが見つかった場合、ボタンを押す
                interactable.PushButton();
            }

            if (hit.collider.TryGetComponent(out GunEmplacementController gunEmplacementController))
            {
                gunEmplacementController.SetPlayerRef(Object.InputAuthority);
            }
        }
    }
}
