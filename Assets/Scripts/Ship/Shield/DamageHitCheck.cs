using Fusion;
using System;
using UnityEngine;

public interface IDamageNotifiable
{
    public void TakeDamage(Single damage, Vector3 hitPoint, Vector3 hitDirection);
}
public class DamageHitCheck : NetworkBehaviour, IDamageable
{
    [SerializeField, Required]
    private GameObject _damageNotifiable; // IDamageNotifiableの参照

    private IDamageNotifiable _damageNotifiableComponent;

    public override void Spawned()
    {
        if (_damageNotifiable.TryGetComponent<IDamageNotifiable>(out IDamageNotifiable notifiable))
        {
            _damageNotifiableComponent = notifiable;
        }
        else
        {
            Debug.LogError("DamageNotifiable component is not found on the assigned GameObject.");
        }
    }

    public void TakeDamage(Single damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        _damageNotifiableComponent?.TakeDamage(damage, hitPoint, hitDirection);
    }
}
