using System;
using UnityEngine;

public class EnemyWakame : BaseEnemy
{
    [SerializeField, Required]
    private BulletMove _bulletPrefab; // 弾丸プレハブ

    public override void FixedUpdateNetwork()
    {
        // FSMはEnemyAIBrain側で駆動するため、ここでは何もしない
    }

    public override void Initialize()
    {
        // 追加初期化なし（必要ならここに）
    }

    public override void AttackTarget()
    {
        // ターゲットの方向はBrainからの回頭で一致している前提
        Vector3 fireDir = transform.forward;
        BulletMove bullet = Runner.Spawn(_bulletPrefab, transform.position + fireDir * 2f, Quaternion.identity);
        bullet.Init(fireDir.normalized);
    }
}
