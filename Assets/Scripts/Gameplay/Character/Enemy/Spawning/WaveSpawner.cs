using Fusion;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// ウェーブシステムの管理
/// 敵のスポーン・ウェーブ進行・難易度調整
/// </summary>
public class WaveSpawner : NetworkBehaviour
{
    [SerializeField]
    private Transform[] _spawnPoints; // 敵のスポーンポイント

    /// <summary>
    /// 指定されたウェーブデータに基づいて敵をスポーンする
    /// </summary>
    /// <param name="waveData"></param>
    /// <returns></returns>
    public List<BaseEnemy> SpawnEnemy(WaveData waveData)
    {
        if (waveData == null) return null;

        List<BaseEnemy> spawnedEnemies = new List<BaseEnemy>();

        for (int i = 0; i < waveData.EnemyPrefabs.Length; i++)
        {
            BaseEnemy enemyPrefab = Runner.Spawn(waveData.EnemyPrefabs[i], ReturnRandSpawnPoint());

            spawnedEnemies.Add(enemyPrefab);
        }
        
        return spawnedEnemies;
    }

    private Vector3 ReturnRandSpawnPoint()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn points are not set or empty.");
            return Vector3.up;
        }
        int randIndex = Random.Range(0, _spawnPoints.Length);
        Vector3 spawnPoint = _spawnPoints[randIndex].position;

        return spawnPoint;
    }
}
