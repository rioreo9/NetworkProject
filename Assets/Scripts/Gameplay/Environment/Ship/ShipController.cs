using System;
using UnityEngine;
using Fusion;
using R3;
using VContainer;
using VitalRouter;

[RequireComponent(typeof(ShipMovement))]
public class ShipController : NetworkBehaviour, IDamageNotifiable, IShipWaypointHandler
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

   

    /// <summary>
    /// HPのReactiveProperty
    /// </summary>
    private ReactiveProperty<int> _shipHealthRP = new();
    public ReadOnlyReactiveProperty<int> ShipHealthRP => _shipHealthRP;

    private ICommandPublisher _publisher;
    private IGameStateNotice _gameStateNotice;
    private ShipMovement _shipMovement;

    private GameState _currentGameState;

    [Inject]
    public void InjectDependencies(ICommandPublisher publisher, IGameStateNotice stateNotice)
    {
        // 必要に応じて依存性を注入
        _publisher = publisher;
        _gameStateNotice = stateNotice;

        _gameStateNotice.GameStateRP.Subscribe(state =>
        {
            _currentGameState = state;
        }).AddTo(this);
    }

    private void Awake()
    {
        if (this.TryGetComponent(out ShipMovement movement))
        {
            _shipMovement = movement;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority/* || _currentGameState != GameState.WaveAction*/) return;
        _shipMovement?.DoMove();
    }

    public void SetNextWaypoint(Transform nextWaypoint)
    {
        _shipMovement?.RPC_SetWayPoint(nextWaypoint.position);
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection)
    {
        if (!HasStateAuthority) return;

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
            // ここで破壊時の処理を追加（例：エフェクト、ゲームオーバー処理など）

            _publisher?.PublishAsync(new GameStateChangeCommand(ChangeStateType.GameOverStart));
        }
    }
}
