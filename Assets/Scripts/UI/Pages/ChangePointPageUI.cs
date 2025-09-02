using System;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class ChangePointPageUI : PageUIBase
{
    [Header("目的地")]
    [SerializeField, Required]
    private Button _wakame;
    [SerializeField, Required]
    private Transform _wakamePoint;

    [SerializeField, Required]
    private Button _seaWeed;
    [SerializeField, Required]
    private Transform _seaWeedPoint;

    [SerializeField, Required]
    private InterfaceReference<IShipWaypointHandler> _shipHandle;

    protected override void Initialize()
    {
        _wakame.OnClickAsObservable()
           .Subscribe(_ => PushWakame())
           .AddTo(this);

        _seaWeed.OnClickAsObservable()
            .Subscribe(_ => PushSeaWeed())
            .AddTo(this);


    }

    private void PushWakame()
    {
        _shipHandle.Value?.SetNextWaypoint(_wakamePoint);
    }
    private void PushSeaWeed()
    {
        _shipHandle.Value?.SetNextWaypoint(_seaWeedPoint);
    }
}
