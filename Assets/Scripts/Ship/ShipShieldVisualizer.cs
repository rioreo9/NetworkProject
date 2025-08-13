using System;
using UnityEngine;
using R3;
using VContainer;
using UnityEngine.UI;

public class ShipShieldVisualizer : MonoBehaviour
{
    [SerializeField]
    private Text _shieldStatusText;

    [SerializeField]
    private Text _shipHpText;

    [Inject]
    public void InjectDependencies(ShipShieldSystem shieldSystem, ShipShieldDurability shieldDurability)
    {
        shieldSystem.ShieldStateRP
            .Subscribe(UpdateShieldStatus)
            .AddTo(this);

        shieldDurability.ShieldPointsRP
            .Subscribe(UpdateShipHp)
            .AddTo(this);
    }

    private void UpdateShieldStatus(ShieldState shieldState)
    {
        if (_shieldStatusText == null) return;

        switch (shieldState)
        {
            case ShieldState.Active:
                _shieldStatusText.text = "起動中";
                break;
            case ShieldState.Inactive:
                _shieldStatusText.text = "停止中";
                break;
            case ShieldState.Broken:
                _shieldStatusText.text = "故障中";
                break;
        }
        _shieldStatusText.color = shieldState == ShieldState.Broken ? Color.red : Color.green;
    }

    private void UpdateShipHp(float currentHp)
    {
        if (_shipHpText == null) return;

        _shipHpText.text = $"HP: {currentHp:F1}";
        _shipHpText.color = currentHp > 0 ? Color.white : Color.red;
    }
}
