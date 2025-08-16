
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

    [SerializeField, Required]
    private Transform _localHandParent; // 手の親オブジェクト（武器やアイテムの持ち手）
    [SerializeField, Required]
    private Transform _networkHandParent;

    [Header("カメラ設定")]

    [SerializeField] private float _maxLookAngle = 80f; // 上下視点制限角度
    [SerializeField] private bool _invertYAxis = false; // Y軸反転オプション

    private Transform _cameraTransform; // カメラのTransform

    [Header("インタラクト設定")]
    [SerializeField] private LayerMask _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク

    private IInteractableTool _interactableControllable;
    private RaycastHit _hit; // レイキャスト用のヒット情報

    /// <summary>
    /// カメラの優先度を設定してアクティブ化
    /// ローカルプレイヤーのスポーン時に呼び出される
    /// </summary>
    public void SetCamera()
    {
        _cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        _cinemachineCamera.Target.TrackingTarget = _followTarget; // カメラの追跡ターゲットを設定

        _cameraTransform = Camera.main.transform; // メインカメラのTransformを取得
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
            UseTool();
            ProcessInteractAction();
        }


        //インタラクトする処理をここに書きたい
    }

    private void ProcessInteractAction()
    {
        Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, Mathf.Infinity, _interactableLayerMask);

        if (hit.collider != null)
        {
            // ヒットしたオブジェクトがインタラクト可能なオブジェクトかどうかをチェック
            if (hit.collider.TryGetComponent<IInteractableButton>(out IInteractableButton button))
            {
                // インタラクト可能なオブジェクトが見つかった場合、ボタンを押す
                button.PushButton();
            }

            if (hit.collider.TryGetComponent<IInteractableTool>(out IInteractableTool interactable))
            {
                _hit = hit; // ヒット情報を保存
                _interactableControllable = interactable; // インタラクト可能なオブジェクトを保存
                RPC_PickUpItem(hit.collider.GetComponent<NetworkObject>());

                if (_interactableControllable.CheckCopyObject()) return;
               
                GameObject obj = Instantiate(hit.collider.gameObject, _networkHandParent.position, Quaternion.identity);
                _interactableControllable.SetCopyObj(obj); // インタラクト可能なオブジェクトのコピーを設定

                // ローカルプレイヤー（入力権限を持つプレイヤー）用の処理
                obj.transform.position = _localHandParent.position; // 手の位置にオブジェクトを移動
                
                obj.transform.SetParent(_localHandParent, true); // ローカル手の親オブジェクトに設定
            }

            if (hit.collider.TryGetComponent(out GunEmplacementController gunEmplacementController))
            {
                gunEmplacementController.Object.RequestStateAuthority();
                gunEmplacementController.SetPlayerRef(Object.InputAuthority);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_PickUpItem(NetworkObject networkObject)
    {
        _interactableControllable = networkObject.GetComponent<IInteractableTool>();

        // リモートプレイヤー（他のプレイヤー）用の処理
        networkObject.transform.position = _networkHandParent.position; // 手の位置にオブジェクトを移動
        networkObject.transform.SetParent(_networkHandParent, true); // ネットワーク手の親オブジェクトに設定
    }

    private void UseTool()
    {
        if (_interactableControllable == null) return;

        Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, Mathf.Infinity, _interactableControllable.layerMask);
        if (hit.collider == null) return;

        _interactableControllable.CheckInteractableObject(hit); // インタラクト可能なオブジェクトをチェック
    }
}
