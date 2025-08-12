using System;
using UnityEngine;
using Fusion;
using R3;
public class ShipShieldDurability : NetworkBehaviour, IDamageable
{
    [Networked, OnChangedRender(nameof(UpdateHp))]
    public float CurrentShieldPoints { get; private set; }

    private IShieldBreakable _shipShieldSystem;

    private ReactiveProperty<float> _shieldPointsRP = new();
    public ReadOnlyReactiveProperty<float> ShieldPointsRP => _shieldPointsRP;

    public override void Spawned()
    {
        if (!gameObject.TryGetComponent<IShieldBreakable>(out IShieldBreakable shieldSystem))
        {
            Debug.LogWarning($"{nameof(ShipShieldDurability)}: IShieldBreakable component not found on {gameObject.name}.");
        }

        _shipShieldSystem = shieldSystem;

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

    private void UpdateHp()
    {
        _shieldPointsRP.Value = CurrentShieldPoints;
    }
}
