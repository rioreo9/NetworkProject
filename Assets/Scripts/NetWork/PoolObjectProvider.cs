using System;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PoolObjectProvider : Fusion.Behaviour, INetworkObjectProvider
{
	// 有効にすると、SceneMangerが処理中の場合にプレハブ取得を遅らせる
    [InlineHelp]
    public bool DelayIfSceneManagerIsBusy = true;

	// Inspectorで指定したPrefabのみObjectPool化できる
	[SerializeField]
    [Header("指定したプレハブのみプール化可能")]
	private List<NetworkObject> _pooledWhiteListPrefabs = new List<NetworkObject>();



	/// <summary>
	/// プレハブIDごとに利用可能なNetworkObjectのキューを管理するディクショナリ
	/// オブジェクトプールから再利用可能なオブジェクトを効率的に取得するために使用
	/// </summary>
	private Dictionary<NetworkPrefabId, Queue<NetworkObject>> _pooledObjectsDict = new Dictionary<NetworkPrefabId, Queue<NetworkObject>>();



	/// プール対象と判断したPrefabId を記録：Releaseで使用
	private HashSet<NetworkPrefabId> _pooledPrefabIds = new HashSet<NetworkPrefabId>();
    

    /// <summary>
	/// プールに保持する最大数0以下は解放済みの全オブジェクトをプールします
    /// </summary>
    [SerializeField]
    [Header("プールの最大数、0以下だと上限なし")]
    private int _maxPoolCount = 0;








    /// <summary>
    /// オブジェクトプール用のオブジェクト生成 キューに挿入
    /// 既定では <see cref="NetworkObjectProviderDefault"/>
    /// </summary>
    protected NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab,  NetworkPrefabId contextPrefabId)
    {
        var result = default(NetworkObject);
        
		// プールからオブジェクトを取得を試行
        if (_pooledObjectsDict.TryGetValue(contextPrefabId, out var freeQ))
        {

            // キューに利用可能なオブジェクトがある場合は再利用
            if (freeQ.Count > 0)
            {
                result = freeQ.Dequeue();
                result.gameObject.SetActive(true);
                
                return result;
            }
        }
        else
        {
            // 新しいプレハブID用の空キューを作成
            _pooledObjectsDict.Add(contextPrefabId, new Queue<NetworkObject>());
        }

        // プールが空または未作成の場合は新規インスタンスを生成
        result = Instantiate(prefab);
        
        return result;
    }

    protected void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
		// 該当プレハブの空きキューがない、またはプールが上限数に達している場合、Destroy()
        if (_pooledObjectsDict.TryGetValue(prefabId, out var freeQ) == false || (_maxPoolCount > 0 && freeQ.Count >= _maxPoolCount))
        {
            Destroy(instance.gameObject);
            return;
        }


        // キューに空きがあるためプールに戻し、オブジェクトを非アクティブにする
        freeQ.Enqueue(instance);
		instance.gameObject.SetActive(false);
    }





    /// <summary>
    /// ネットワークオブジェクトのインスタンスを取得します
    /// ホワイトリストに登録されたプレハブはオブジェクトプールから取得し、
    /// それ以外は通常のInstantiateで新規作成します
    /// </summary>
    /// <param name="runner">NetworkRunner</param>
    /// <param name="context">プレハブ取得のコンテキスト情報</param>
    /// <param name="instance">取得されたNetworkObjectインスタンス</param>
    /// <returns>取得結果（Success/Retry/Failed）</returns>
    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context,
        out NetworkObject instance)
    {
        
        instance = null;

        // SceneManagerが処理中の場合は取得を遅延、リトライ
        if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy) {
            return NetworkObjectAcquireResult.Retry;
        }

        // プレハブの読み込みを試行
        NetworkObject prefab;
        try {
            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
        } catch (Exception ex) {
            // エラーの場合 Failed送信
            Log.Error($"Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

		// プレハブが読み込まれていない場合はリトライ
		if (!prefab) {


			// Fusion が即時の読み込みを要求しない限り問題ありません
			// まだ必要であれば次の更新で再度呼ばれます
			return NetworkObjectAcquireResult.Retry;
		}

		// ホワイトリストに登録されているかチェック
		bool shouldPool = _pooledWhiteListPrefabs.Contains(prefab);

		if (shouldPool)
		{
			// プール対象として記録し、オブジェクトプールから取得
			_pooledPrefabIds.Add(context.PrefabId);
			instance = InstantiatePrefab(runner, prefab, context.PrefabId);
		}
		else
		{
			// プール対象外は通常のInstantiateで新規作成
			instance = Instantiate(prefab);
		}
        Assert.Check(instance);


        if (context.DontDestroyOnLoad) {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        } else {
            // NetworkRunnerが管理するシーンに移動
            runner.MoveToRunnerScene(instance.gameObject);
        }

        // Fusionにインスタンスを登録
        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    /// <summary>
    /// ネットワークオブジェクトのインスタンスを解放
    /// プール対象のオブジェクトはプールに戻し、それ以外は破棄
    /// Fusionから呼び出されるオブジェクトの解放処理
    /// </summary>
    /// <param name="runner">NetworkRunner</param>
    /// <param name="context">オブジェクト解放のコンテキスト情報</param>
    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

		// オブジェクトが破棄中でない場合のみ処理を実行
		if (!context.IsBeingDestroyed) {

			// プレハブタイプのオブジェクトかチェック
			if (context.TypeId.IsPrefab) {

				var prefabId = context.TypeId.AsPrefabId;

				// プール対象として登録されているかチェック
				if (_pooledPrefabIds.Contains(prefabId))
				{
					// プール対象の場合はプールに戻すか破棄
					DestroyPrefabInstance(runner, prefabId, instance);
				}
				else
				{
					// プール対象外は即座に破棄
					Destroy(instance.gameObject);
				}
			}
			else
			{
				// プレハブでないオブジェクトは即座に破棄
				Destroy(instance.gameObject);
			}
		}

        // Fusionからインスタンスを除去
        if (context.TypeId.IsPrefab) {
            runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }

    public NetworkPrefabId GetPrefabId(NetworkRunner runner, NetworkObjectGuid prefabGuid)
    {
        return runner.Prefabs.GetId(prefabGuid);
    }

    public void SetMaxPoolCount(int count)
    {
        _maxPoolCount = count;
    }
}
