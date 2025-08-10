using UnityEngine;
using Fusion;
using R3;

public class ShipShieldSystem : NetworkBehaviour
{
    [SerializeField]
    private GameObject _shieldVisual; // シールドのビジュアルオブジェクト

    [Networked, OnChangedRender(nameof(UpdateShieldActive))]
    public bool IsShieldActive { get; private set; } = false;

    private ReactiveProperty<bool> _shieldActiveRP = new();
    public ReadOnlyReactiveProperty<bool> ShieldActiveRP => _shieldActiveRP;

    /// <summary>
    /// スイッチ状態を切り替える（サーバー側のみ実行）
    /// </summary>
    public void ToggleShield()
    {
        if (!Object.HasStateAuthority)
        {
            return; // サーバー権限がない場合は何もしない
        }
        IsShieldActive = !IsShieldActive;
       
    }

    private void UpdateShieldActive()
    {
        _shieldActiveRP.Value = IsShieldActive;

        /// シールドの状態を更新
        _shieldVisual.SetActive(IsShieldActive);
    }
}
