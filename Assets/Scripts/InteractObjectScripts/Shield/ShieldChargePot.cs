using Fusion;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]

public class ShieldChargePot : BaseInteractControlObject, IInteractableTool
{
    [SerializeField]
    private LayerMask _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク
    public LayerMask layerMask => _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク

    [SerializeField]
    private float _repairAmount = 20f; // シールドの修理量

    private GameObject _copyObj;
    private Rigidbody _rigidbody;

    public override void Spawned()
    {
        if (gameObject.TryGetComponent<Rigidbody>(out Rigidbody rigidbody))
        {
            _rigidbody = rigidbody;
        }
        else
        {
            _rigidbody = gameObject.AddComponent<Rigidbody>();
        }
    }

    public override void ControlObject()
    {
        _isInteractable = true;
    }

    public void CheckInteractableObject(RaycastHit hit)
    {
        if (!hit.collider.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) return;
        if (!hit.collider.TryGetComponent<ShieldRepairStation>(out ShieldRepairStation shieldRepair)) return;

        ConsumptionLocalTool();

        if (Object.HasStateAuthority)
        {
            UseTool(shieldRepair);
        }
        else
        {
            // クライアント側でのRPC呼び出し
            RPC_UseTool(networkObject);
        }
    }

    public void SetCopyObj(GameObject networkObj)
    {
        _copyObj = networkObj;
    }

    public bool CheckCopyObject()
    {
        return _copyObj != null;
    }

    private void UseTool(ShieldRepairStation repairStation)
    {
        repairStation.RepairShield(_repairAmount);
        ConsumptionNetTool();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UseTool(NetworkObject shieldRepair)
    {
        if (shieldRepair.TryGetComponent<ShieldRepairStation>(out ShieldRepairStation repairStation))
        {
            UseTool(repairStation);
        }
    }

    private void ConsumptionLocalTool()
    {
        if (_copyObj != null)
        {
            Destroy(_copyObj);
        }
    }

    private void ConsumptionNetTool()
    {
        Runner.Despawn(this.Object);
    }
}
