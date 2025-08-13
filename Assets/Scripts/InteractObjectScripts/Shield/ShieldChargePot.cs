using Fusion;
using System;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class ShieldChargePot : BaseInteractControlObject, IInteractableTool
{
    [SerializeField]
    private LayerMask _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク
    public LayerMask layerMask => _interactableLayerMask; // インタラクト可能なオブジェクトのレイヤーマスク

    [SerializeField]
    private float _repairAmount = 20f; // シールドの修理量

    private ShieldRepairStation _shieldRepairStation;
    private GameObject _copyObj;

    public override void ControlObject()
    {
        _isInteractable = true;
    }

    public void CheckInteractableObject(RaycastHit hit)
    {
        if (!hit.collider.TryGetComponent<ShieldRepairStation>(out ShieldRepairStation shieldRepair)) return;

        _shieldRepairStation = shieldRepair;

        if (Object.HasStateAuthority)
        {
            UseTool();
        }
        else
        {
            RPC_UseTool();
        }
    }

    public void SetCopyObj(GameObject networkObj)
    {
        _copyObj = networkObj;
    }

    private void UseTool()
    {
        _shieldRepairStation.RepairShield(_repairAmount);
        ConsumptionTool();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_UseTool()
    {
        UseTool();
    }

    private void ConsumptionTool()
    {
        Runner.Despawn(this.Object);
        if (_copyObj != null)
        {
            Destroy(_copyObj);
        }
    }
}
