using Fusion;
using VContainer;
using UnityEngine;
using VContainer.Unity;

public class EnemyWaveHandler : NetworkBehaviour
{
    private IGameStateNotice _gameStateNotice;

    public override void Spawned()
    {
        // ネットワークオブジェクトがスポーンされた時に手動で依存性注入を実行
        InjectDependencies();
    }

    /// <summary>
    /// VContainerから手動で依存性を注入するメソッド
    /// </summary>
    private void InjectDependencies()
    {
        // LifetimeScopeを検索してコンテナを取得
        LifetimeScope lifetimeScope = FindFirstObjectByType<LifetimeScope>();
        if (lifetimeScope != null)
        {
            // コンテナから依存性を解決
            _gameStateNotice = lifetimeScope.Container.Resolve<IGameStateNotice>();
            Debug.Log("EnemyWaveHandler: 依存性注入が完了しました");
        }
        else
        {
            Debug.LogError("EnemyWaveHandler: LifetimeScopeが見つかりません");
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (_gameStateNotice == null)
        {
            Debug.LogError("IGameStateNotice is not injected into EnemyWaveHandler.");
        }
        else
        {
            Debug.Log("EnemyWaveHandler spawned with IGameStateNotice injected.");
        }
    }
}
