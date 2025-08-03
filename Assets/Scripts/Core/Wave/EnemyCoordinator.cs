using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCoordinator : NetworkBehaviour
{
    [Networked] public int AliveEnemyCount { get; private set; }
    [Networked] public int RequiredKillCount { get; private set; }

    private List<BaseEnemy> _enemies = new List<BaseEnemy>();

    public event System.Action OnEnemyAllDeath;

    public override void Spawned()
    {
        // 初期化
        AliveEnemyCount = 0;
        RequiredKillCount = 0;
    }

    /// <summary>
    /// Waveの目標を設定するメソッド
    /// </summary>
    /// <param name="enemies"></param>
    public void SetWaveTarget(List<BaseEnemy> enemies)
    {
        if (!Object.HasStateAuthority || enemies == null) return;
        _enemies = enemies;
        AliveEnemyCount = enemies.Count;

        // 各敵に死亡イベントを登録
        foreach (BaseEnemy enemy in enemies)
        {
            enemy.OnDeath += OnEnemyDeath;
        }
    }

    /// <summary>
    /// ウェーブ終了時のリセットを呼び出すメソッド
    /// </summary>
    public void Reset()
    {
        if (!Object.HasStateAuthority) return;
        AliveEnemyCount = 0;
        RequiredKillCount = 0;

        //オブジェクト削除
        foreach (var enemy in _enemies)
        {
            Runner.Despawn(enemy.Object);
        }

        _enemies.Clear();      
    }

    private void OnEnemyDeath(BaseEnemy enemy, Vector3 position)
    {
        if (!Object.HasStateAuthority) return;
        AliveEnemyCount--;
        RequiredKillCount++;

        // 敵の死亡イベントを解除
        if (enemy != null)
        {
            enemy.OnDeath -= OnEnemyDeath;
            Runner.Despawn(enemy.Object);
        }

        // すべての敵が倒されたらウェーブクリア
        if (AliveEnemyCount <= 0)
        {
            OnEnemyAllDeath?.Invoke();
        }
    }
}
