using Fusion;
using UnityEngine;


/// <summary>
/// ウェーブシステムの管理
/// 敵のスポーン・ウェーブ進行・難易度調整
/// </summary>
public class WaveSpawner : NetworkBehaviour
{
    [SerializeField, Required]
    private GameObject _enemyPrefab; // スポーンする敵のプレハブ

    [SerializeField, Required]
    private Vector3 _spawnPosition; // 敵の初期スポーン位置

    public override void FixedUpdateNetwork()
    {

    }

    public void SpawnEnemy()
    {
        Runner.Spawn(_enemyPrefab, _spawnPosition, Quaternion.identity);
    }

    // ウェーブ開始・敵スポーン・完了判定
    public void StartWave(int waveIndex) { }
   
    private void CheckWaveCompletion() { }

   
}
