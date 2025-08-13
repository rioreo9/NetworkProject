using System;
using UnityEngine;
using Fusion;
using R3;
using VContainer;
public class ShipShieldDurability : NetworkBehaviour, IDamageable
{
    [Networked, OnChangedRender(nameof(UpdateHp))]
    public float CurrentShieldPoints { get; private set; }

    private IShieldBreakable _shipShieldSystem;

    private ReactiveProperty<float> _shieldPointsRP = new();
    public ReadOnlyReactiveProperty<float> ShieldPointsRP => _shieldPointsRP;

    [Inject]
    public void InjectDependencies(ShipShieldSystem shieldSystem)
    {
        _shipShieldSystem = shieldSystem;
    }

    public override void Spawned()
    {
        _shieldPointsRP.Value = CurrentShieldPoints;
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

    public void RepairShield(float repairAmount)
    {
        if (!HasStateAuthority) return;
        CurrentShieldPoints = Mathf.Min(CurrentShieldPoints + repairAmount, 100f); // Assuming 100 is the max shield points
        UpdateHp();
    }

    private void UpdateHp()
    {
        _shieldPointsRP.Value = CurrentShieldPoints;
    }
}
