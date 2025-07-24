using Fusion;
using R3;
using VitalRouter;
using UnityEngine;

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

public readonly struct GameStateChangeCommand : ICommand
{
    public GameState NewState { get; }
    public GameStateChangeCommand(GameState newState)
    {
        NewState = newState;
    }
}
[Routes]
public partial class GameFlowHandler : NetworkBehaviour, IGameStateNotice
{
    [Networked]
    public GameState CurrentGameState { get; private set; }

    /// <summary>
    /// ゲームの状態をリアクティブに管理するプロパティ
    /// </summary>
    private ReactiveProperty<GameState> _gameStateRP = new();
    public ReadOnlyReactiveProperty<GameState> GameStateRP => _gameStateRP;

    public override void Spawned()
    {
        UpdateGameState(GameState.WaveAction);
    }

    /// <summary>
    /// ゲームの状態を更新するメソッド
    /// </summary>
    /// <param name="state"></param>
    private void UpdateGameState(GameState state)
    {
        _gameStateRP.Value = state;
        CurrentGameState = state;
    }

    public void OnStateChange(GameStateChangeCommand state)
    {
        Debug.Log(state);
        UpdateGameState(state.NewState);
    }
}
