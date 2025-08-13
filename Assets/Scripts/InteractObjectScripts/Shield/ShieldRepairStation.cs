using System;
using UnityEngine;

public class ShieldRepairStation : MonoBehaviour
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
