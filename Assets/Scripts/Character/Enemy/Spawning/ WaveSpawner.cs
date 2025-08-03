using Fusion;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// ウェーブシステムの管理
/// 敵のスポーン・ウェーブ進行・難易度調整
/// </summary>
public class WaveSpawner : NetworkBehaviour
{
    public List<BaseEnemy> SpawnEnemy(WaveData waveData)
    {
        if (waveData == null) return null;

        List<BaseEnemy> spawnedEnemies = new List<BaseEnemy>();

        for (int i = 0; i < waveData.EnemyPrefabs.Length; i++)
        {
            Debug.Log(Runner);

            BaseEnemy enemyPrefab = Runner.Spawn(waveData.EnemyPrefabs[i], Vector3.up);

            spawnedEnemies.Add(enemyPrefab);
            Debug.Log($"Spawned Enemy: {waveData.EnemyPrefabs[i].name}");
        }

        return spawnedEnemies;
    }
}
