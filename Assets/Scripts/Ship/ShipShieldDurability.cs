using System;
using UnityEngine;
using Fusion;
public class ShipShieldDurability :  NetworkBehaviour,  IDamageable
{
    [Networked]
    public float CurrentShieldPoints { get; private set; }

    private IShieldBreakable _shipShieldSystem;

    public override void Spawned()
    {
        gameObject.TryGetComponent<IShieldBreakable>(out IShieldBreakable shieldSystem);
        _shipShieldSystem = shieldSystem;
    }

    public void TakeDamage(Single damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!HasStateAuthority) return;

        CurrentShieldPoints = Mathf.Max(0, CurrentShieldPoints - damage);

        if (CurrentShieldPoints <= 0)
        {
            _shipShieldSystem?.BreakShield();
        }
    }
}
