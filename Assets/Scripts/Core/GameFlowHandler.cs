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
    [Networked, OnChangedRender(nameof(UpdateGameState))]
    public GameState CurrentGameState { get; private set; }

    /// <summary>
    /// ゲームの状態をリアクティブに管理するプロパティ
    /// </summary>
    private ReactiveProperty<GameState> _gameStateRP = new();
    public ReadOnlyReactiveProperty<GameState> GameStateRP => _gameStateRP;

    public override void Spawned()
    {
        RPC_SetGameState(GameState.Preparation);
    }

    /// <summary>
    /// ゲームの状態を更新するメソッド
    /// </summary>
    /// <param name="state"></param>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetGameState(GameState state)
    {
        CurrentGameState = state;
    }

    private void UpdateGameState()
    {
        _gameStateRP.Value = CurrentGameState;
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
                RPC_SetGameState(GameState.WaveAction);
                break;
            case ChangeStateType.WavePhaseEnd:
                RPC_SetGameState(GameState.UpgradePhase);
                break;
            case ChangeStateType.UpgradePhaseEnd:
                RPC_SetGameState(GameState.WaveAction);
                break;
        }
    }
}
