using System;
using R3;

/// <summary>
/// ゲームの状態を通知するインターフェース
/// </summary>
public interface IGameStateNotice
{
    public ReadOnlyReactiveProperty<GameState> GameStateRP { get; }
}
