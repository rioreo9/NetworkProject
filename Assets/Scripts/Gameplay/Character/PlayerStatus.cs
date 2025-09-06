using System;
using Fusion;
using UnityEngine;
using R3;

public class PlayerStatus : NetworkBehaviour, INoticePlayerInteract
{
    [Networked, OnChangedRender(nameof(OnIsInteractingChanged))]
    public bool IsInteracting { get; private set; } = false;

    private ReactiveProperty<bool> _isControllerInteracting = new(false);
    public ReadOnlyReactiveProperty<bool> IsControllerInteracting => _isControllerInteracting;

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetControllerInteracting(bool isInteracting)
    { 
            _isControllerInteracting.Value = isInteracting;
    }

    private void OnIsInteractingChanged()
    {
        _isControllerInteracting.Value = IsInteracting;
    }
}

public interface INoticePlayerInteract
{
    public void RPC_SetControllerInteracting(bool isInteracting);
}


