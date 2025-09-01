using System;
using UnityEngine;
using Fusion;
using R3;
using VContainer;
using VitalRouter;

public class ShipController : NetworkBehaviour, IDamageNotifiable
{
    /// <summary>
    /// 戦艦のHP
    /// </summary>
    [Networked, OnChangedRender(nameof(UpdateHp))]
    public int ShipHealth { get; private set; } = 100;

    /// <summary>
    /// 戦艦が破壊されたかどうか
    /// </summary>
    [Networked]
    public bool IsDestroyed { get; private set; } = false;

    private ICommandPublisher _publisher;

    /// <summary>
    /// HPのReactiveProperty
    /// </summary>
    private ReactiveProperty<int> _shipHealthRP = new();
    public ReadOnlyReactiveProperty<int> ShipHealthRP => _shipHealthRP;

    [Inject]
    public void InjectDependencies(ICommandPublisher publisher)
    {
        // 必要に応じて依存性を注入
        _publisher = publisher;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!HasStateAuthority) return;

        Debug.Log($"[ShipController] Taking damage: {damage} at point: {hitPoint}");

        ShipHealth = Mathf.Max(0, ShipHealth - (int)damage);

        CheckDestruction();
    }

    /// <summary>
    /// HPの更新
    /// </summary>
    private void UpdateHp()
    {
        _shipHealthRP.Value = ShipHealth;
    }

    /// <summary>
    /// 戦艦の破壊をチェック
    /// </summary>
    private void CheckDestruction()
    {
        if (ShipHealth <= 0 && !IsDestroyed)
        {
            IsDestroyed = true;
            Debug.Log("[ShipController] Ship destroyed!");
            // ここで破壊時の処理を追加（例：エフェクト、ゲームオーバー処理など）

            _publisher.PublishAsync(new GameStateChangeCommand(ChangeStateType.GameOverStart));
        }
    }
}
