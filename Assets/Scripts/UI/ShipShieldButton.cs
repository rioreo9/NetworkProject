using UnityEngine;
using Fusion;
using R3;
using UnityEngine.UI;
using VContainer;
using VitalRouter;

public class ShipShieldButton : NetworkBehaviour
{
  
    [SerializeField]
    private Button _shieldButton;

    private ShipShieldSystem _shipShieldSystem;

    [Inject]
    public void InjectDependencies(ShipShieldSystem shieldSystem)
    {
        _shipShieldSystem = shieldSystem;
    }

    public override void Spawned()
    {
        _shieldButton?.OnClickAsObservable()
             .Subscribe(_ => NoticePush())
             .AddTo(this);
    }

    
    private void NoticePush()
    {
        if (Object.HasStateAuthority)
        {
            ToggleShield();
        }
        else
        {
            RPC_RequestToggleShield();
        }
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
