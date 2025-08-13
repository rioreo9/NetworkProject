using System;
using UnityEngine;
using Fusion;

/// <summary>
/// 既存のワカメ敵（暫定）。
/// 現在は旧実装を残したまま、ステートマシン移行までのつなぎとして動作します。
/// 将来的には <see cref="EnemyShooter"/> へ置き換え予定。
/// </summary>
public class EnemyWakame : BaseEnemy
{
    [SerializeField]
    private LayerMask targetMask; // 当たり判定対象のレイヤーマスク

    [SerializeField, Required]
    private BulletMove _bulletPrefab; // 弾丸プレハブ

    [SerializeField]
    private float _fireRate = 1.0f; // 射撃レート（秒間）

    [Networked]
    private TickTimer _fireTimer { get; set; }

    /// <summary>
    /// 権限側のみでターゲット探索と簡易攻撃を行う旧ロジック。
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // HasStateAuthority：サーバー もしくは ホストの場合Trueを返す
        // おそらくホスト側のみで処理させようとしている
        if (!HasStateAuthority) return;
        SearchTarget();

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

    /// <summary>
    /// 旧実装の初期化（現状は特になし）。
    /// </summary>
    public override void Initialize()
    {

    }

    /// <summary>
    /// 弾を前方へ生成して初速方向を設定（旧ロジック）。
    /// </summary>
    public override void AttackTarget()
    {
        Vector3 targetDirection = (_targetBattleship.position - transform.position).normalized;

        BulletMove bullet = Runner.Spawn(_bulletPrefab, transform.position + targetDirection * 2, Quaternion.identity);
        bullet.Init(targetDirection);
    }

    /// <summary>
    /// OverlapSphere による簡易ターゲット探索（最近傍選択ではない）。
    /// </summary>
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

    /// <summary>
    /// 水平面でのみターゲット方向へ即時回頭。
    /// </summary>
    private void DoRotation(Vector3 targetDirection)
    {
        // Y成分を0にしてY軸回転のみにする
        targetDirection.y = 0f;

        // 正規化してからキャラクターの回転を設定
        targetDirection.Normalize();

        transform.rotation = Quaternion.LookRotation(targetDirection);
    }

}
