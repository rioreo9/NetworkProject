using System;
using UnityEngine;

public interface IInteractableTool
{
    public LayerMask layerMask { get;} // インタラクト可能なオブジェクトのレイヤーマスク

    public void CheckInteractableObject(RaycastHit hit); // インタラクト可能なオブジェクトをチェックするメソッド
}
