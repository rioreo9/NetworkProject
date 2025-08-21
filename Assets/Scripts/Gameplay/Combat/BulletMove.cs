using Fusion;
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

    // キャッシュ用変数（プール再利用時のデフォルト値保存）
    private int _defaultDamage;
    private float _defaultLifeTime = 5.0f; // デフォルト5秒
    private float _defaultSpeed = 20.0f;   // デフォルト速度20
    private LayerMask _defaultTargetLayerMask;

    /// <summary>
    /// NetworkObject初期化（Inspector値のキャッシュ）
    /// </summary>
    private void Awake()
    {
        // Inspector設定値をキャッシュ（プール再利用時のデフォルト値）
        _defaultDamage = _damage;
        _defaultTargetLayerMask = _targetLayerMask;
    }

    /// <summary>
    /// ネットワークオブジェクトがスポーンされた後の初期化
    /// [Networked]プロパティへの安全なアクセスが可能
    /// </summary>
    public override void Spawned()
    {
        // Networkedプロパティの初期化（デフォルト値設定）
        lifeTime = _defaultLifeTime;
        Speed = _defaultSpeed;
        
        // 非Networkedプロパティの初期化
        _damage = _defaultDamage;
        _targetLayerMask = _defaultTargetLayerMask;
    }

    /// <summary>
    /// プール再利用時の初期化（OnEnable代替）
    /// Networkedプロパティには触れない
    /// </summary>
    private void OnEnable()
    {
        // Networkedプロパティ以外のみ初期化
        _damage = _defaultDamage;
        _targetLayerMask = _defaultTargetLayerMask;
    }

    /// <summary>
    /// 弾丸初期化処理
    /// 弾丸生成直後に呼び出され、寿命タイマーを開始
    /// </summary>
    public void Init(Vector3 shotDirection)
    {
        // デフォルト値で初期化
        InitWithParams(shotDirection, _defaultLifeTime, _defaultSpeed);
    }

    /// <summary>
    /// パラメータ指定での弾丸初期化
    /// </summary>
    public void InitWithParams(Vector3 shotDirection, float bulletLifeTime, float bulletSpeed)
    {
        // Networkedプロパティを設定（Spawned後なので安全）
        lifeTime = bulletLifeTime;
        Speed = bulletSpeed;
        
        // サーバーのTickベースで正確な寿命タイマーを作成
        life = TickTimer.CreateFromSeconds(Runner, lifeTime);
        transform.rotation = Quaternion.LookRotation(shotDirection);
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
        if (!hit.GameObject.activeSelf) return;

        if (hit.GameObject.TryGetComponent<IDamageable>(out IDamageable target))
        {
            target.TakeDamage(_damage, hit.Point, transform.forward);
            // ネットワークオブジェクトとして適切に削除
            Runner.Despawn(Object);
        }
    }
}
