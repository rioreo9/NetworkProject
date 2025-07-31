using Fusion;
using VContainer;
using R3;
using VitalRouter;
using UnityEngine;

public class WaveHandler : NetworkBehaviour
{
    private IGameStateNotice _gameStateNotice;

    private ICommandPublisher _publisher;

   private bool _isWaveActive = false;

    private float _waveNowTime = 0f; // 1 Waveの持続時間

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
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (_isWaveActive)
        {
            _waveNowTime += Runner.DeltaTime;
            Debug.Log($"Wave Time: {_waveNowTime:F2} seconds");
        }

        if (_waveNowTime > 5f)
        {
            _publisher.PublishAsync
                (new GameStateChangeCommand(ChangeStateType.WavePhaseEnd));
            _waveNowTime = 0f; // Waveの時間をリセット
            Debug.Log("Wave Complete");
        }
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
        }
        else
        {
            _isWaveActive = false;
        }
    }
}
