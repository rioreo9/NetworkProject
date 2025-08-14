using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolObjectProvider : Fusion.Behaviour, INetworkObjectProvider
{
    /// <summary>
    /// 有効にすると、シーンマネージャーがビジー状態の場合、プロバイダーはプレファブインスタンスの取得を遅延させます。
    /// </summary>
    [InlineHelp]
    public bool _delayIfSceneManagerIsBusy = true;

    /// <summary>
    /// 有効にすると、プロバイダーはオブジェクトがプールに戻る際に無効化します。無効にすると、オブジェクトは有効なままでマテリアルが変更されます（これは視覚化のためのみで、良い実践ではありません）
    /// </summary>
    private bool _disableObjectInPool = true;

    private Dictionary<NetworkPrefabId, Queue<NetworkObject>> _free = new Dictionary<NetworkPrefabId, Queue<NetworkObject>>();

    /// <summary>
    /// プールに保持されるオブジェクトの数。0または負の値は、解放されたすべてのオブジェクトをプールすることを意味します。
    /// </summary>
    private int _maxPoolCount = 0;

    /// ベースの <see cref="NetworkObjectProviderDefault"/> は、デフォルトで単純に新しいゲームオブジェクトをインスタンス化します。
    /// オブジェクトをプールするカスタムロジックを使用するメソッドを作成しましょう。
    protected NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab,
        NetworkPrefabId contextPrefabId)
    {
        var result = default(NetworkObject);

        // プレファブ用の空きキューが見つかり、キューが空でない場合。空きオブジェクトを返します。
        if (_free.TryGetValue(contextPrefabId, out var freeQ))
        {
            if (freeQ.Count > 0)
            {
                result = freeQ.Dequeue();
                result.gameObject.SetActive(true);

                // ----------------------------------------------------------------------
                // Rigidbodyを再び非キネマティックにします。このサンプル専用です。このオブジェクトプロバイダーをコピーする場合は削除してください。
                if (result.gameObject.TryGetComponent<Rigidbody>(out var rigidbody))
                {
                    rigidbody.isKinematic = false;
                    rigidbody.detectCollisions = true;
                }
                // ----------------------------------------------------------------------

                return result;
            }
        }
        else
        {
            _free.Add(contextPrefabId, new Queue<NetworkObject>());
        }

        // -- この時点で空きキューはまだ作成されていないか、空の状態です。新しいオブジェクトを作成します。
        result = Instantiate(prefab);

        return result;
    }

    protected void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        // このプレファブ用の空きキューがない、または プールがすでに定義した最大オブジェクト数に達している場合。破棄されるべきです。
        if (_free.TryGetValue(prefabId, out var freeQ) == false || (_maxPoolCount > 0 && freeQ.Count >= _maxPoolCount))
        {
            Destroy(instance.gameObject);
            return;
        }


        // 空きキューが見つかりました。キャッシュすべきです。
        freeQ.Enqueue(instance);

        if (_disableObjectInPool)
        {
            // オブジェクトを非アクティブにします。
            instance.gameObject.SetActive(false);
        }
        else
        {
            // ----------------------------------------------------------------------
            // オブジェクトを透明でキネマティックにします。このサンプル専用です。このオブジェクトプロバイダーをコピーする場合は削除してください。
            if (instance.gameObject.TryGetComponent<Renderer>(out var renderer))
            {
                var color = renderer.material.color;
                color.a = .25f;
                renderer.material.color = color;
            }

            if (instance.gameObject.TryGetComponent<Rigidbody>(out var rigidbody))
            {
                rigidbody.isKinematic = true;
                rigidbody.detectCollisions = false;
            }
            // ----------------------------------------------------------------------
        }
    }

    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context,
        out NetworkObject instance)
    {

        instance = null;

        if (_delayIfSceneManagerIsBusy && runner.SceneManager.IsBusy)
        {
            return NetworkObjectAcquireResult.Retry;
        }

        NetworkObject prefab;
        try
        {
            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

        if (!prefab)
        {
            // これはFusionがプレファブを即座にロードする必要がない限り問題ありません。
            // このプレファブのインスタンスがまだ必要な場合、このメソッドは次の更新で再度呼び出されます
            return NetworkObjectAcquireResult.Retry;
        }

        instance = InstantiatePrefab(runner, prefab, context.PrefabId);
        Assert.Check(instance);

        if (context.DontDestroyOnLoad)
        {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        }
        else
        {
            runner.MoveToRunnerScene(instance.gameObject);
        }

        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

        // プレファブのみをプールします。
        if (!context.IsBeingDestroyed)
        {
            if (context.TypeId.IsPrefab)
            {
                DestroyPrefabInstance(runner, context.TypeId.AsPrefabId, instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        if (context.TypeId.IsPrefab)
        {
            runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }

    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
    {
        return runner.Prefabs.GetId(prefabGuid);
    }

    private void DisableObjectsAsDefaultReleaseBehaviour(bool value)
    {
        _disableObjectInPool = value;
    }

    public bool SwitchDefaultReleaseBehaviour()
    {
        DisableObjectsAsDefaultReleaseBehaviour(!_disableObjectInPool);

        return _disableObjectInPool;
    }

    public void SetMaxPoolCount(int count)
    {
        _maxPoolCount = count;
    }
}
