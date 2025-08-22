using Core;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkTransform))]

public abstract class BasePickUpToolObject : NetworkBehaviour
{
    [Networked]
    public bool IsInteractable { get; private set; } // インタラクト中かどうか

    [SerializeField]
    protected LayerMask _layerMask;
    public LayerMask layerMask => _layerMask; // インタラクト可能なオブジェクトのレイヤーマスク

    protected Rigidbody _copyObj; // インタラクト可能なオブジェクトのコピー
    protected Collider _copyCollider; // コピーオブジェクトのコライダー

    protected Rigidbody _rigidbody; // Rigidbodyコンポーネント
    protected Collider _collider; // コライダーコンポーネント
    protected NetworkTransform _networkTransform; // ネットワークトランスフォームコンポーネント

    public override void Spawned()
    {
        // コピーオブジェクトを生成するためのGameObjectをInstantiateし、Rigidbodyコンポーネントを取得または追加
        _copyObj = ComponentUtility.GetOrAddComponent<Rigidbody>(Instantiate(gameObject));
        _copyCollider = ComponentUtility.GetOrAddComponent<Collider>(_copyObj.gameObject); // コピーオブジェクトのコライダーを取得または追加
        _copyObj.gameObject.SetActive(false); // コピーオブジェクトを非アクティブにする

        // 初期化処理(依存を作ってあるため)
        _rigidbody = ComponentUtility.GetOrAddComponent<Rigidbody>(this);
        _collider = ComponentUtility.GetOrAddComponent<Collider>(this);
        _networkTransform = ComponentUtility.GetOrAddComponent<NetworkTransform>(this);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetInteractable(bool interactable)
    {
        IsInteractable = interactable; // インタラクト可能状態を設定するメソッド

        ChangeInteractMode(interactable);
    }
    public void LocalItemInteractable(bool interactable)
    {
        if (_copyObj == null) return; // コピーオブジェクトが存在しない場合は何もしない

        if (interactable)
        {
            _copyCollider.enabled = !interactable; // コピーオブジェクトのコライダーを有効化する
            _copyObj.isKinematic = interactable; // コピーオブジェクトのkinematicを設定
            _copyObj.gameObject.SetActive(true); // コピーオブジェクトをアクティブにする
        }
        else
        {
            ConsumptionLocalTool(); // インタラクト終了時にローカルツールを消費する
        }
    }

    public void SetINetItemPosition(Vector3 setItemPos, Transform parent)
    {
        _networkTransform.Teleport(setItemPos);
        transform.SetParent(parent); // ネットワークトランスフォームの親を設定する
    }

    public void SetCopyItemPosition(Vector3 pos, Transform parent)
    {
        _copyObj.transform.position = pos; // コピーオブジェクトの位置を設定する
        _copyObj.transform.SetParent(parent); // コピーオブジェクトの親を設定する
    }

    public abstract bool CheckInteractableObject(RaycastHit hit);// インタラクト可能なオブジェクトをチェックするメソッド

    protected abstract void UseTool(Component component);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    protected virtual void RPC_UseTool(NetworkObject netObj) { }

    protected void ChangeInteractMode(bool interacte)
    {
        _rigidbody.isKinematic = interacte; // インタラクト中の場合はkinematicをtrueにする
        _collider.enabled = !interacte; // インタラクト中はコライダーを無効化する

        if (!interacte)
        {
            transform.position = transform.position + Vector3.up; // ツールの位置を更新する
            transform.SetParent(null); // ツールの親を解除する
        }
    }
    protected void ConsumptionNetTool()
    {
        Runner.Despawn(this.Object);
    }
    protected void ConsumptionLocalTool()
    {
        if (_copyObj != null)
        {
            _copyObj.gameObject.SetActive(false); // コピーオブジェクトを非アクティブにする
        }
    }
}
