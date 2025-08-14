using UnityEngine;

/// <summary>
/// 直線射撃を行うベース敵タイプ。
/// ステートマシンの AttackState から呼ばれる想定で、
/// <see cref="AttackTarget"/> 内で弾プレハブを Spawn します。
/// </summary>
public class EnemyShooter : BaseEnemy
{
    [SerializeField, Required]
    private BulletMove _bulletPrefab;

    [SerializeField]
    private float _fireRate = 1.0f;

    /// <summary>
    /// 射撃タイプの初期化（現状は特になし）。
    /// </summary>
    public override void Initialize()
    {
    }

    /// <summary>
    /// 現在ターゲットへ向けて弾を生成し、初速方向を設定します。
    /// </summary>
    public override void AttackTarget()
    {
        Transform target = _enemyAIBrain != null ? _enemyAIBrain.CurrentTarget : _targetBattleship;
        if (target == null) return;

        Vector3 dir = (target.position - transform.position).normalized;
        BulletMove bullet = Runner.Spawn(_bulletPrefab, transform.position + dir * 2f, Quaternion.LookRotation(dir));
        bullet.Init(dir);
    }

    /// <summary>
    /// 射撃間隔（秒）。_fireRate が 0 以下の場合は基底の値を使用。
    /// </summary>
    public override float GetAttackInterval()
    {
        if (_fireRate <= 0f)
        {
            return base.GetAttackInterval();
        }
        return 1.0f / _fireRate;
    }
}


