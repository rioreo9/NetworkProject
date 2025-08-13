using Fusion;
using System;
using UnityEngine;

/// <summary>
/// ネットワーク対応の敵キャラクターの基底クラス。
/// すべての敵タイプはこのクラスを継承し、
/// ステータス・AI連携・死亡通知などの共通機能を利用します。
/// </summary>
public abstract class BaseEnemy : NetworkBehaviour
{
    [Header("基本ステータス")]
    [SerializeField] protected float _maxHealth = 100f; // 最大HP
    [SerializeField] protected float _moveSpeed = 3f;   // 移動速度
    [SerializeField] protected float _attackDamage = 10f; // 攻撃力
    [SerializeField] protected float _visionRange = 12f; // 索敵距離
    [SerializeField] protected float _attackRange = 6f;  // 攻撃射程
    [SerializeField] protected LayerMask _targetMask;    // 索敵対象レイヤー

    /// <summary>
    /// 生存フラグ（Networked）。権限側で更新し、全クライアントへ同期。
    /// </summary>
    [Networked] public bool IsAlive { get; set; }

    // AI状態管理
    protected EnemyAIBrainState _enemyAIBrain; // AI行動制御
    protected Transform _targetBattleship; // 旧実装互換用のターゲット参照（現行は Brain が管理）

    // 死亡時のイベント
    public event Action<BaseEnemy, Vector3> OnDeath; // 死亡イベント

    // States から参照される公開プロパティ
    public float MoveSpeed => _moveSpeed;
    public float AttackDamage => _attackDamage;
    public float VisionRange => _visionRange;
    public float AttackRange => _attackRange;
    public LayerMask TargetMask => _targetMask;


    public override void Spawned()
    {
        // ネットワーク生成時の初期化
        IsAlive = true;

        // AI参照を確保して初期化
        if(_enemyAIBrain == null)
        {
            _enemyAIBrain = GetComponent<EnemyAIBrainState>();
        }
        _enemyAIBrain?.Initialize();

        // 派生クラスの初期化
        Initialize();
    }

    /// <summary>敵固有の初期化</summary>
    public abstract void Initialize();
    /// <summary>攻撃実行（弾生成などの本処理はここで行う）</summary>
    public abstract void AttackTarget();

    // AttackState の汎用クールダウン（派生で上書き可能）
    public virtual float GetAttackInterval()
    {
        return 1.0f;
    }

    public void Death()
    {
        IsAlive = false;
        OnDeath?.Invoke(this, transform.position); // 死亡イベントを発火
    }
}
