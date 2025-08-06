using System;
using UnityEngine;

public class EnemyWakame : BaseEnemy
{
    [SerializeField]
    private LayerMask _targetMask; // 当たり判定対象のレイヤーマスク

    [SerializeField, Required]
    private BulletMove _bulletPrefab; // 弾丸プレハブ

    public override void FixedUpdateNetwork()
    {
        // HasStateAuthority：サーバー もしくは ホストの場合Trueを返す
        // おそらくホスト側のみで処理させようとしている
        if (!HasStateAuthority) return;
        SearchTarget();

        if (_targetBattleship != null)
        {
            DoRotation(_targetBattleship.position - transform.position);
            AttackTarget();
        }
        //transform.position += transform.forward * Runner.DeltaTime;
    }

    public override void Initialize()
    {

    }

    public override void AttackTarget()
    {
        Vector3 targetDirection = (_targetBattleship.position - transform.position).normalized;

        BulletMove bullet = Runner.Spawn(_bulletPrefab, transform.position + targetDirection * 2, Quaternion.identity);
        bullet.Init(targetDirection);
    }

    private void SearchTarget()
    {
        // レイヤーマスクを使用してターゲットを検索
        Collider[] targets = Physics.OverlapSphere(transform.position, 10f, _targetMask);

        foreach (var target in targets)
        {
            // ターゲットに対する処理

            _targetBattleship = target.transform;
        }
    }

    private void DoRotation(Vector3 targetDirection)
    {
        // Y成分を0にしてY軸回転のみにする
        targetDirection.y = 0f;

        // 正規化してからキャラクターの回転を設定
        targetDirection.Normalize();

        transform.rotation = Quaternion.LookRotation(targetDirection);
    }

}
