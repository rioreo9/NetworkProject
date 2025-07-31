using System;
using UnityEngine;
[Serializable]
public class WaveData
{
    [SerializeField] private int _waveIndex; // ウェーブのインデックス
    [SerializeField] private int _enemyCount; // このウェーブでスポーンする敵の数
    [SerializeField] private float _spawnInterval = 1f; // 敵をスポーンする間隔（秒）
    [SerializeField] private BaseEnemy[] _enemyPrefabs; // スポーンする敵のプレハブ
    [SerializeField] private float _waveDuration = 30f; // ウェーブの持続時間（秒）

    public int WaveIndex => _waveIndex;
    public int EnemyCount => _enemyCount;
    public float SpawnInterval => _spawnInterval;
    public BaseEnemy[] EnemyPrefabs => _enemyPrefabs;
    public float WaveDuration => _waveDuration;
}

[CreateAssetMenu(fileName = "WaveConfiguration", menuName = "Game/Wave Configuration")]
public class WaveConfiguration : ScriptableObject
{
    [SerializeField] private WaveData[] _waves; // ウェーブデータの配列

    public WaveData[] Waves => _waves;
}
