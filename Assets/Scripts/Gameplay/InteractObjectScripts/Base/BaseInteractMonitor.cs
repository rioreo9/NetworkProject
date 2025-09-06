using Fusion;
using System;

public abstract class BaseInteractMonitor : NetworkBehaviour, IInteractableControllable
{
    [Networked]
    public bool IsInteractable { get; protected set; } = true;

    public abstract void AccesObject(PlayerRef player, INoticePlayerInteract status);
}
