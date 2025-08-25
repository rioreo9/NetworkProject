using UnityEngine;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

public class ProjectInstaller : LifetimeScope
{
    [SerializeField, Required]
    private GameFlowHandler _gameFlowHandler;
    [SerializeField, Required]
    private WaveHandler _enemyWaveHandler;
    //[SerializeField, Required]
    //private UIPageRouter _uiPageRouter;

    protected override void Configure(IContainerBuilder builder)
    {
        // Vital RouterをDIコンテナに登録
        builder.RegisterVitalRouter(routing =>
        {
            //購読側（一元的にまとめるクラスが望ましい）
            routing.Map(_gameFlowHandler).As<IGameStateNotice>();
            //routing.Map(_uiPageRouter);
        });


        // インスタンス登録（シーンに配置されたオブジェクトを使用）

        if (_enemyWaveHandler != null)
        {
            builder.RegisterComponent(_enemyWaveHandler);
        }
        builder.RegisterComponentInHierarchy<UpgradePhaseEndButton>();
        builder.RegisterComponentInHierarchy<PreparationEndButton>();

        builder.RegisterComponentInHierarchy<ShipShieldButton>();
        builder.RegisterComponentInHierarchy<ShipShieldSystem>();
        builder.RegisterComponentInHierarchy<ShipShieldDurability>();
        builder.RegisterComponentInHierarchy<ShipShieldVisualizer>();

        //builder.RegisterComponentInHierarchy<UIPageRouter>();
        //builder.RegisterComponentInHierarchy<LoginMenuButton>();
    }
}
