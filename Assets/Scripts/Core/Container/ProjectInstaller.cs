using Fusion;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ProjectInstaller : LifetimeScope
{
    [SerializeField, Required]
    private GameFlowHandler _gameFlowHandler;
    [SerializeField, Required]
    private EnemyWaveHandler _enemyWaveHandler;

    protected override void Configure(IContainerBuilder builder)
    {
        // インスタンス登録（シーンに配置されたオブジェクトを使用）
        if (_gameFlowHandler != null)
        {
            builder.RegisterInstance(_gameFlowHandler).As<IGameStateNotice>();
        }

        if (_enemyWaveHandler != null)
        {
            builder.RegisterInstance(_enemyWaveHandler);
        }

        // または、型による自動登録の場合
        // builder.RegisterComponentInHierarchy<GameFlowHandler>().As<IGameStateNotice>();
        // builder.RegisterComponentInHierarchy<EnemyWaveHandler>();
    }
}
