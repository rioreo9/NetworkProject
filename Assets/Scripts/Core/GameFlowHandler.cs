using Fusion;
using R3;
using VitalRouter;
using UnityEngine;

public enum GameState
{
    WaitingForPlayers,
    Preparation,
    WaveAction,
    UpgradePhase,
    GameOver,
    Victory
}

public enum ChangeStateType
{
    WaitingForPlayersEnd,
    PreparationEnd,
    WavePhaseEnd,
    WavePhaseComplete,
    UpgradePhaseEnd,  
}

public readonly struct GameStateChangeCommand : ICommand
{
    public ChangeStateType NewState { get; }
    public GameStateChangeCommand(ChangeStateType newState)
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
        UpdateGameState(GameState.Preparation);
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

    /// <summary>
    /// ゲームの状態変更コマンドを受け取り、状態を更新するメソッド
    /// </summary>
    /// <param name="state"></param>
    public void OnStateChange(GameStateChangeCommand state)
    {
        Debug.Log(state);
        switch (state.NewState)
        {
            case ChangeStateType.PreparationEnd:
                UpdateGameState(GameState.WaveAction);
                break;
            case ChangeStateType.WavePhaseEnd:
                UpdateGameState(GameState.UpgradePhase);
                break;
            case ChangeStateType.UpgradePhaseEnd:
                UpdateGameState(GameState.WaveAction);
                break;
        }
    }
}
