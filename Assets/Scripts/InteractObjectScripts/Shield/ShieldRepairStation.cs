using System;
using UnityEngine;
using Fusion;

public class ShieldRepairStation : NetworkBehaviour
{
    [SerializeField, Required]
    private ShipShieldDurability _shipShieldDurability;

    public void RepairShield(float repairAmount)
    {
        if (_shipShieldDurability == null) return;
        // シールドの修理処理
        _shipShieldDurability.RepairShield(repairAmount);
    }
}
