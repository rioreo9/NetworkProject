using System;
using Fusion;
using UnityEngine;
using UnityEngine.Splines;

public interface IDamageable
{
    void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection);
}

public class NetworkedDamageable : NetworkBehaviour, IDamageable
{
    [Networked]
    public float CurrentHealth { get; private set; }

    [SerializeField, Required]
    private BaseEnemy _baseEnemy; // BaseEnemyの参照

    [SerializeField]
    private float _maxHealth = 100f;

    public override void Spawned()
    {
        CurrentHealth = _maxHealth; // 初期化時に最大HPを設定
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!HasStateAuthority) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            _baseEnemy?.Death();
        }
    }
}
