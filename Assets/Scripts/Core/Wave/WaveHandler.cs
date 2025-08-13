using Fusion;
using VContainer;
using R3;
using VitalRouter;
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(WaveSpawner))]
[RequireComponent(typeof(EnemyCoordinator))]

public class WaveHandler : NetworkBehaviour
{
    [SerializeField, Required]
    private WaveConfiguration _waveConfiguration; // ウェーブの設定

    [Networked]
    public int _currentWaveIndex { get; private set; } = 0; // 現在のウェーブインデックス

    private IGameStateNotice _gameStateNotice;

    private ICommandPublisher _publisher;

    /// <summary>
    /// 敵をすべて倒したらを購読しておきたい
    /// </summary>
    private WaveSpawner _waveSpawner;
    private EnemyCoordinator _enemyCoordinator;

    private bool _isWaveActive = false;

    private float _waveNowTime = 0f; // 1 Waveの持続時間
    private float _waveTimeLimit = 0f; // ウェーブの持続時間（秒）

    /// <summary>
    /// VContainerから手動で依存性を注入するメソッド
    /// </summary>
    [Inject]
    public void InjectDependencies(IGameStateNotice notice, ICommandPublisher publisher)
    {
        _gameStateNotice = notice;
        _publisher = publisher;
    }

    public override void Spawned()
    {
        //Stateの変更を購読
        _gameStateNotice.GameStateRP.Subscribe(CheckWave);

        _waveSpawner = gameObject.GetComponent<WaveSpawner>();
        _enemyCoordinator = gameObject.GetComponent<EnemyCoordinator>();

        _enemyCoordinator.OnEnemyAllDeath += ClearWave; // 敵が全て倒されたらウェーブクリアをチェック
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (_isWaveActive)
        {
            _waveNowTime += Runner.DeltaTime;
        }

        if (_waveNowTime > _waveTimeLimit)
        {
            ClearWave();
        }
    }

    /// <summary>
    /// ウェーブをクリアしたら呼ぶメソッド
    /// </summary>
    public void ClearWave()
    {
        _enemyCoordinator.Reset(); // 敵の状態をリセット

        _publisher.PublishAsync
                (new GameStateChangeCommand(ChangeStateType.WavePhaseEnd));
        _waveNowTime = 0f; // Waveの時間をリセット

        _currentWaveIndex++; // 次のウェーブへ進む
    }

    /// <summary>
    /// ゲームの状態をチェックし、Waveがアクティブかどうかを設定するメソッド
    /// </summary>
    /// <param name="state"></param>
    private void CheckWave(GameState state)
    {
        if (state == GameState.WaveAction)
        {
            _isWaveActive = true;
            CheckClear();
        }
        else
        {
            _isWaveActive = false;
        }
    }

    /// <summary>
    /// ウェーブがクリアされたかどうかをチェックするメソッド
    /// </summary>
    private void CheckClear()
    {
        if (_currentWaveIndex >= _waveConfiguration.Waves.Length)
        {
            Debug.Log("All waves cleared! Victory!");
        }
        else
        {
            // 次のウェーブへ進む
            WaveStart();
        }
    }

    /// <summary>
    /// ウェーブを開始するメソッド
    /// </summary>
    private void WaveStart()
    {
        if (!Object.HasStateAuthority) return;
        List<BaseEnemy> spawnedEnemies = new List<BaseEnemy>();

        spawnedEnemies = _waveSpawner.SpawnEnemy(_waveConfiguration.Waves[_currentWaveIndex]);
        _waveTimeLimit = _waveConfiguration.Waves[_currentWaveIndex].WaveDuration;
        _enemyCoordinator.SetWaveTarget(spawnedEnemies);
    }
}
