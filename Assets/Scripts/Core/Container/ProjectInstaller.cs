using UnityEngine;
using VContainer;
using VContainer.Unity;
using VitalRouter.VContainer;

public class ProjectInstaller : LifetimeScope
{
    [SerializeField, Required]
    private GameFlowHandler _gameFlowHandler;
    [SerializeField, Required]
    private EnemyWaveHandler _enemyWaveHandler;

    protected override void Configure(IContainerBuilder builder)
    {
        // Vital RouterをDIコンテナに登録
        builder.RegisterVitalRouter(routing =>
        {
            //購読側（一元的にまとめるクラスが望ましい）
            routing.Map(_gameFlowHandler).As<IGameStateNotice>();
        });


        // インスタンス登録（シーンに配置されたオブジェクトを使用）

        if (_enemyWaveHandler != null)
        {
            builder.RegisterComponent(_enemyWaveHandler);
        }

    }
}
