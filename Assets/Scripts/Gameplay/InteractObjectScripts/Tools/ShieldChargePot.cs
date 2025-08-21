using Fusion;
using System;
using UnityEngine;

public class ShieldChargePot : BasePickUpToolObject, IInteractableTool
{
    [SerializeField]
    private float _repairAmount = 20f; // シールドの修理量

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

        if (gameObject.TryGetComponent(out NetworkTransform targetNetworkTransform))
        {
            _networkTransform = targetNetworkTransform;
        }
    }

    public override bool CheckInteractableObject(RaycastHit hit)
    {
        if (!hit.collider.TryGetComponent<NetworkObject>(out NetworkObject networkObject)) return false;
        if (!hit.collider.TryGetComponent<ShieldRepairStation>(out ShieldRepairStation shieldRepair)) return false;

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

        return true;
    }

    protected override void UseTool(Component component)
    {
        if (component is ShieldRepairStation repairStation)
        {
            repairStation.RepairShield(_repairAmount);
            ConsumptionNetTool();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    protected override void RPC_UseTool(NetworkObject shieldRepair)
    {
        if (shieldRepair.TryGetComponent<ShieldRepairStation>(out ShieldRepairStation repairStation))
        {
            UseTool(repairStation);
        }
    }

}
