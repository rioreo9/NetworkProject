using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCoordinator : NetworkBehaviour
{
    [Networked] public int AliveEnemyCount { get; private set; }
    [Networked] public int RequiredKillCount { get; private set; }

    private List<BaseEnemy> _enemies = new List<BaseEnemy>();

    public override void Spawned()
    {
        // 初期化
        AliveEnemyCount = 0;
        RequiredKillCount = 0;
    }

    /// <summary>
    /// Waveの目標を設定するメソッド
    /// </summary>
    /// <param name="enemy"></param>
    public void SetWaveTarget(List<BaseEnemy> enemy)
    {
        if (!Object.HasStateAuthority || enemy == null) return;
        _enemies = enemy;
        AliveEnemyCount = enemy.Count;
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
}
