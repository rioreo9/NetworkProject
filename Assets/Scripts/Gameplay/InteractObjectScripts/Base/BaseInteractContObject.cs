using Fusion;
using System;

public abstract class BaseInteractContObject : NetworkBehaviour
{
    [Networked]
    public bool HasInteractor { get; protected set; } = false;

    public abstract void Access(INoticePlayerInteract status);
}
