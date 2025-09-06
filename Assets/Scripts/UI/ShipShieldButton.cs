using UnityEngine;
using Fusion;
using VContainer;
using Core.Utils;

public class ShipShieldButton : BaseInteractButtonObject
{

    private ShipShieldSystem _shipShieldSystem;

    [Inject]
    public void InjectDependencies(ShipShieldSystem shieldSystem)
    {
        _shipShieldSystem = shieldSystem;
    }

    public override void PushButton()
    {
        NetworkAuthorityHelper.ExecuteWithAuthority(
             this,
             directAction: ToggleShield,
             rpcAction: RPC_RequestToggleShield
             );
    }

    /// <summary>
    /// スイッチ状態を切り替える（サーバー側のみ実行）
    /// </summary>
    private void ToggleShield() 
    {
        _shipShieldSystem?.ToggleShield();
    }

    /// <summary>
    /// サーバーにシールドの切り替えを要求するRPC
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestToggleShield()
    {
        ToggleShield();
    }
}
