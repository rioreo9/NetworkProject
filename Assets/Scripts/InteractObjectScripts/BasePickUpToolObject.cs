using System;
using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class BasePickUpToolObject : NetworkBehaviour, IInteractableTool
{
    [Networked, OnChangedRender(nameof(ChangeInteractMode))]
    public bool IsInteractable { get; private set; } // インタラクト中かどうか

    public LayerMask layerMask => _layerMask; // インタラクト可能なオブジェクトのレイヤーマスク

    [SerializeField]
    protected LayerMask _layerMask;

    protected Rigidbody _copyObj; // インタラクト可能なオブジェクトのコピー
    protected Rigidbody _rigidbody; // Rigidbodyコンポーネント

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetInteractable(bool interactable)
    {
        IsInteractable = interactable; // インタラクト可能状態を設定するメソッド

        if (!interactable)
        {
            transform.position = transform.position + transform.forward; // ツールの位置を更新する
            transform.SetParent(null); // ツールの親を解除する
        } 
    }

    /// <summary>
    /// インタラクト可能なオブジェクトのコピー
    /// </summary>
    /// <param name="networkObj"></param>
    public void SetCopyObj(GameObject networkObj)// インタラクト可能なオブジェクトのコピーを設定するメソッド
    {
        if(networkObj.TryGetComponent(out Rigidbody rigidbody))
        {
            _copyObj = rigidbody;
        }  
    }

    /// <summary>
    /// インタラクト可能なオブジェクトのコピーが存在するかどうかをチェックするメソッド
    /// </summary>
    /// <returns></returns>
    public bool CheckCopyObject()
    {
        return _copyObj != null;
    }

    public bool CheckInteractable()
    {
        return IsInteractable; // インタラクト可能状態を返す
    }

    public abstract bool CheckInteractableObject(RaycastHit hit);// インタラクト可能なオブジェクトをチェックするメソッド

    protected abstract void UseTool(Component component);

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    protected virtual void RPC_UseTool(NetworkObject netObj) { }

    protected void ChangeInteractMode()
    {
        if (IsInteractable)
        {
            _rigidbody.isKinematic = IsInteractable; // インタラクト中の場合はkinematicをtrueにする

            if (_copyObj == null) return; // コピーオブジェクトが存在しない場合は何もしない
            _copyObj.isKinematic = IsInteractable; // コピーオブジェクトのkinematicを設定
        }
        else
        {
            _rigidbody.isKinematic = IsInteractable;
           
            ConsumptionLocalTool();
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
            Destroy(_copyObj.gameObject);
        }
    }
}
