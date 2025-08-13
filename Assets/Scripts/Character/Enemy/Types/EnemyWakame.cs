using System;
using UnityEngine;
using Fusion;

public class EnemyWakame : BaseEnemy
{
   
    [SerializeField, Required]
    private BulletMove _bulletPrefab; // 弾丸プレハブ
    
    [SerializeField]
    private float _fireRate = 1.0f; // 射撃レート（秒間）
    
    [Networked]
    private TickTimer _fireTimer { get; set; }

    public override void FixedUpdateNetwork()
    {
        // HasStateAuthority：サーバー もしくは ホストの場合Trueを返す
        // おそらくホスト側のみで処理させようとしている
        if (!HasStateAuthority) return;
        _enemyAIBrain.StateMove();

        if (_targetBattleship != null)
        {
            DoRotation(_targetBattleship.position - transform.position);
            
            // タイマーが完了している場合のみ攻撃
            if (_fireTimer.ExpiredOrNotRunning(Runner))
            {
                AttackTarget();
                // 次の射撃までのタイマーを設定
                _fireTimer = TickTimer.CreateFromSeconds(Runner, 1.0f / _fireRate);
            }
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
