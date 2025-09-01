using UnityEngine;
using Fusion;
using R3;

public interface IShieldBreakable
{
    public void BreakShield();
    public void RepairShield();
}

public enum ShieldState
{
    Active,
    Inactive,
    Broken
}

public class ShipShieldSystem : NetworkBehaviour, IShieldBreakable
{
    [SerializeField,Required]
    private GameObject _shieldVisual; // シールドのビジュアルオブジェクト

    [Networked, OnChangedRender(nameof(UpdateShieldActive))]
    public ShieldState CurrentShieldState { get; private set; } = ShieldState.Inactive;

    private ReactiveProperty<ShieldState> _shieldStateRP = new();
    public ReadOnlyReactiveProperty<ShieldState> ShieldStateRP => _shieldStateRP;

    /// <summary>
    /// スイッチ状態を切り替える（サーバー側のみ実行）
    /// </summary>
    public void ToggleShield()
    {
        if (!Object.HasStateAuthority || CurrentShieldState == ShieldState.Broken)
        {
            RPC_ToggleShield();
        }
        else
        {
            CurrentShieldState = CurrentShieldState ==
            ShieldState.Active ? ShieldState.Inactive : ShieldState.Active;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ToggleShield()
    {
        ToggleShield();
    }

    public void BreakShield()
    {
        CurrentShieldState = ShieldState.Broken;
    }

    public void RepairShield()
    {
        if (!Object.HasStateAuthority || CurrentShieldState != ShieldState.Broken)
        {
            return; // サーバー権限がないか、シールドが壊れていない場合は何もしない
        }
        CurrentShieldState = ShieldState.Inactive; // シールドを修理して非アクティブ状態に戻す
    }

    private void UpdateShieldActive()
    {
        switch (CurrentShieldState)
        {
            case ShieldState.Active:
                _shieldVisual.SetActive(true);
                break;
            case ShieldState.Inactive:
                _shieldVisual.SetActive(false);
                break;
            case ShieldState.Broken:
                _shieldVisual.SetActive(false);
                break;
        }

        _shieldStateRP.Value = CurrentShieldState;
    }
}
