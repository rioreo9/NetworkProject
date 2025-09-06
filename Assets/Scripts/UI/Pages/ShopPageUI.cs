using System;
using UnityEngine;
using UnityEngine.UI;
using R3;

public class ShopPageUI : PageUIBase
{
    [Header("ショップの各ボタン")]
    [SerializeField, Required]
    private Button _hello;
    [SerializeField, Required]
    private Button _buy;

    protected override void Initialize()
    {
        _hello.OnClickAsObservable()
           .Subscribe(_ => PushHello())
           .AddTo(this);

        _buy.OnClickAsObservable()
            .Subscribe(_ => PushBuy())
            .AddTo(this);
    }

    private void PushHello()
    {
        Debug.Log("Hello");
    }

    private void PushBuy()
    {
        Debug.Log("Buy");
    }
}
