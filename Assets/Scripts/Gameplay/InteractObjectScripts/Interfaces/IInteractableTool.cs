using Fusion;
using System;
using UnityEngine;

public interface IInteractableTool
{
    public LayerMask LayerMask { get; }

    public bool IsInteractable { get; }

    public void RPC_SetInteractable(bool interactable);

    public void LocalItemInteractable(bool interactable);

    public bool CheckInteractableObject(RaycastHit hit);

    public void SetCopyItemPosition(Vector3 pos, Transform parent);
}
