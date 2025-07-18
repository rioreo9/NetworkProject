using System;
using Fusion;
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

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public bool IsAlive { get; set; }

    // AI状態管理
    protected Transform _targetBattleship; // 戦艦のターゲット
    protected EnemyAIBrain _enemyAI; // AI行動制御

    public abstract void Initialize(); // 敵固有の初期化
    public abstract void AttackTarget(); // 攻撃実行
}
