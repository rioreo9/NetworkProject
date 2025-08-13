using Fusion;
using System;
using UnityEngine;

/// <summary>
/// ネットワーク対応の敵キャラクターの基底クラス
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

    [Networked] public bool IsAlive { get; set; }

    // AI状態管理
    protected EnemyAIBrainState _enemyAIBrain; // AI行動制御
    protected Transform _targetBattleship; // 戦艦のターゲット

    // 死亡時のイベント
    public event Action<BaseEnemy, Vector3> OnDeath; // 死亡イベント


    public override void Spawned()
    {
        // ネットワーク生成時の初期化
        IsAlive = true;

        // AI参照を確保して初期化
        if(_enemyAIBrain == null)
        {
            _enemyAIBrain = GetComponent<EnemyAIBrainState>();
        }


        // 派生クラスの初期化
        Initialize();
    }

    public abstract void Initialize(); // 敵固有の初期化
    public abstract void AttackTarget(); // 攻撃実行

    public void Death()
    {
        IsAlive = false;
        OnDeath?.Invoke(this, transform.position); // 死亡イベントを発火
    }
}
