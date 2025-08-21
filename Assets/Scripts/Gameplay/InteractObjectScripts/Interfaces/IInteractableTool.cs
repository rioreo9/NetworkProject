using Fusion;
using System;
using UnityEngine;

public interface IInteractableTool
{
    public LayerMask layerMask { get;} // インタラクト可能なオブジェクトのレイヤーマスク

    public bool CheckInteractableObject(RaycastHit hit); // インタラクト可能なオブジェクトをチェックするメソッド

    public void SetCopyObj(GameObject networkObj);// インタラクト可能なオブジェクトのコピーを設定するメソッド

    public void RPC_SetInteractable(bool interactable); // インタラクト可能状態を設定するRPCメソッド
    public void RPC_SetItemPosition(Vector3 setItemPos); // インタラクト可能オブジェクトの位置を設定するRPCメソッド

    public bool CheckCopyObject();
    public bool CheckInteractable();
}
