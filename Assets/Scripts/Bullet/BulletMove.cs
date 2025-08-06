using Fusion;
using System;
using UnityEngine;

/// <summary>
/// ネットワーク同期対応弾丸移動システム
/// Photon Fusion2のラグ補償機能を使用した高精度な弾丸処理
/// </summary>
public class BulletMove : NetworkBehaviour
{
    [SerializeField]
    private int _damage = 1;

    /// <summary>
    /// 弾丸の寿命タイマー（ネットワーク同期）
    /// TickTimerを使用してサーバータイムベースで正確な時間管理
    /// </summary>
    [Networked] private TickTimer life { get; set; }
    
    /// <summary>
    /// 弾丸の寿命（秒）
    /// 生成時に設定され、ネットワーク上で同期される
    /// </summary>
    [Networked] public float lifeTime { get; set; }
    
    /// <summary>
    /// 弾丸の移動速度（単位/秒）
    /// ネットワーク同期されるため全クライアントで一致
    /// </summary>
    [Networked] public float Speed { get; set; }

    /// <summary>
    /// 当たり判定対象のレイヤーマスク
    /// Inspectorで設定可能、敵・壁・オブジェクトなどを指定
    /// </summary>
    [SerializeField]
    public LayerMask _targetLayerMask;

    /// <summary>
    /// 弾丸初期化処理
    /// 弾丸生成直後に呼び出され、寿命タイマーを開始
    /// </summary>
    public void Init(Vector3 shiotDirection)
    {
        // サーバーのTickベースで正確な寿命タイマーを作成
        life = TickTimer.CreateFromSeconds(Runner, lifeTime);
        transform.rotation = Quaternion.LookRotation(shiotDirection);
    }

    /// <summary>
    /// Photon Fusion用のネットワーク固定更新処理
    /// 物理計算と同期されたタイミングで実行される
    /// 全クライアントで決定的な結果を保証
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        // 寿命チェック：期限切れの場合は弾丸を削除
        if (life.Expired(Runner))
        {
            // ネットワークオブジェクトとして適切に削除
            Runner.Despawn(Object);
        }
        else
        {
            // 弾丸移動処理
            // Runner.DeltaTimeを使用してネットワークタイムベースで移動
            transform.position += Speed * transform.forward * Runner.DeltaTime;

            // 当たり判定処理を実行
            HitAction();
        }
    }

    /// <summary>
    /// 弾丸の当たり判定処理
    /// ラグ補償機能を使用して高精度な当たり判定を実装
    /// 高速弾丸でも確実に判定できるよう継続的レイキャストを使用
    /// </summary>
    private void HitAction()
    {
        bool isHit = Runner.LagCompensation.Raycast
              (transform.position,
              transform.forward,
              Speed,
              Object.InputAuthority,
              out LagCompensatedHit hit,
              _targetLayerMask);

        if (isHit)
        {
            CheckHitTarget(hit);
        }
    }

    private void CheckHitTarget(LagCompensatedHit hit)
    {
        if (hit.GameObject.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(_damage, hit.Point, transform.forward);
            // ネットワークオブジェクトとして適切に削除
            Runner.Despawn(Object);
        }
    }
}
