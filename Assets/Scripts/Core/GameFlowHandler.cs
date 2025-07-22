using System;
using R3;
using Fusion;

public enum GameState
{
    WaitingForPlayers,
    Preparation,
    WaveAction,
    WaveComplete,
    UpgradePhase,
    GameOver,
    Victory
}
public class GameFlowHandler : NetworkBehaviour, IGameStateNotice
{
    [Networked]
    public GameState CurrentGameState {private set; get; }

    /// <summary>
    /// ゲームの状態をリアクティブに管理するプロパティ
    /// </summary>
    private ReactiveProperty<GameState> _gameStateRP = new();
    public ReadOnlyReactiveProperty<GameState> GameStateRP => _gameStateRP;

    public override void Spawned()
    {
        UpdateGameState(GameState.WaitingForPlayers);
    }

    public override void FixedUpdateNetwork()
    {
        
    }

    /// <summary>
    /// ゲームの状態を更新するメソッド
    /// </summary>
    /// <param name="state"></param>
    private void UpdateGameState(GameState state)
    {
        if (Object.HasStateAuthority)
        {
            _gameStateRP.Value = state;
            CurrentGameState = state;
        }
    }
}
