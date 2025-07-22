using Fusion;
using System;
using System.Runtime.InteropServices;

public class EnemyWaveHandler : NetworkBehaviour
{
    [InterfaceType(typeof(IGameStateNotice))]
    private IGameStateNotice _gameStateNotice;
}
