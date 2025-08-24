using Fusion;
using System;
using UnityEngine;

public interface IDamageNotifiable
{
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection);
}

public class DamageHitCheck : NetworkBehaviour, IDamageable
{
    [SerializeField, Required]
    private GameObject _damageNotifiable;

    private IDamageNotifiable _damageNotifiableComponent;

    public override void Spawned()
    {
        // キャッシュ化でパフォーマンス向上
        if (_damageNotifiable != null && _damageNotifiable.TryGetComponent<IDamageNotifiable>(out var notifiable))
        {
            _damageNotifiableComponent = notifiable;
        }
        else
        {
            Debug.LogError($"[DamageHitCheck] IDamageNotifiable component not found on {gameObject.name}");
        }
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        // null チェックとエラーハンドリングの改善
        if (_damageNotifiableComponent == null)
        {
            Debug.LogWarning($"[DamageHitCheck] Damage notifiable component is null on {gameObject.name}");
            return;
        }

        _damageNotifiableComponent.TakeDamage(damage, hitPoint, hitDirection);
    }
}
