# Fusion Object Pooling

**翻訳元:** https://doc.photonengine.com/ja-jp/fusion/current/technical-samples/fusion-object-pooling

---

## 概要

このサンプルでは、Fusion独自の**Network Object Pool**の実装を紹介します。これは、ネットワークオブジェクトの生成と破棄を効率的に管理するための仕組みです。

オブジェクトプーリングは、メモリの断片化を最小限に抑え、CPUとガベージコレクタの負荷を軽減するために使用される一般的なデザインパターンです。特に多数のオブジェクトが頻繁に作成・破棄される場合（例：弾丸、エフェクト、敵キャラクターなど）に非常に効果的です。

---

## ダウンロード

このサンプルのZipファイルは、**Fusion SDK 2**に同梱されており、Fusionの製品ページからダウンロードできます。

---

## INetworkObjectProviderインターフェース

Fusionでオブジェクトプーリングを実装するには、`INetworkObjectProvider`インターフェースを実装する必要があります。このインターフェースには以下の2つの主要なメソッドがあります：

### AcquirePrefabInstance()

`NetworkRunner.Spawn()`が呼び出されたときに、プールからオブジェクトを取得するために使用されます。

```csharp
NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, 
    in NetworkPrefabAcquireContext context, out NetworkObject instance)
```

### ReleaseInstance()

`NetworkRunner.Despawn()`が呼び出されたときに、オブジェクトを解放してプールに戻すために使用されます。

```csharp
void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
```

---

## プールの実装

### 基本概念

このサンプルでは、シンプルなディクショナリベースのプールシステムを実装しています：

- **キー**: `NetworkPrefabId` - 各プレハブタイプを識別
- **値**: `Queue<NetworkObject>` - 使用可能なオブジェクトのキュー

### インスタンス取得の流れ

1. **プールの確認**: 指定されたプレハブIDに対応するキューが存在し、使用可能なオブジェクトがあるかチェック
2. **オブジェクトの再利用**: キューにオブジェクトがある場合は取り出してアクティブ化
3. **新規作成**: キューが空の場合は新しいインスタンスを作成

### インスタンス解放の流れ

1. **プールの制限確認**: 最大プール数に達していないかチェック
2. **プールへ返却**: 制限内であればオブジェクトを非アクティブ化してキューに追加
3. **破棄**: プールが満杯の場合は通常通りオブジェクトを破棄

---

## サンプルのテスト方法

1. **シーンの実行**: サンプルシーンを開いて再生
2. **オブジェクトの生成**: 指定されたキーまたはボタンでオブジェクトをスポーン
3. **プールの確認**: シーン内のオブジェクトを観察し、プールされたオブジェクトが再利用されているか確認
4. **視覚的フィードバック**: プールされたオブジェクトは異なる色で表示され、解放済み/再利用可能であることが分かりやすくなっています

また、プレハブタイプごとのプール最大数を`Max Pool Count`フィールドで設定できます。0に設定すると無制限、正の数を設定するとその数までプールされます。例えば10に設定すると、同じタイプのオブジェクトが既に10個プールされている場合、それ以上のオブジェクトは破棄されます。

---

## 完全なコードスニペット

以下は、このサンプルのNetwork Object Providerの完全なコードスニペットです（一部の固有ロジックを除去して、より汎用的で使用可能な形にしています）：

```csharp
public class PoolObjectProvider : Fusion.Behaviour, INetworkObjectProvider
{
    /// <summary>
    /// 有効にすると、シーンマネージャーがビジー状態の場合、プレハブインスタンスの取得を遅延します。
    /// </summary>
    [InlineHelp]
    public bool DelayIfSceneManagerIsBusy = true;
    
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
        
        // プレハブ用の空きキューが見つかり、キューが空でない場合。空きオブジェクトを返します。
        if (_free.TryGetValue(contextPrefabId, out var freeQ))
        {
            if (freeQ.Count > 0)
            {
                result = freeQ.Dequeue();
                result.gameObject.SetActive(true);
                return result;
            }
        }
        else
        {
            _free.Add(contextPrefabId, new Queue<NetworkObject>());
        }
        
        // この時点で空きキューがまだ作成されていないか、空でした。新しいオブジェクトを作成します。
        result = Instantiate(prefab);
        
        return result;
    }

    protected void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        if (_free.TryGetValue(prefabId, out var freeQ) == false)
        {
            // このプレハブ用の空きキューがありません。破棄する必要があります。
            Destroy(instance.gameObject);
            return;
        } 
        else if (_maxPoolCount > 0 && freeQ.Count >= _maxPoolCount)
        {
            // プールは既に定義した最大数のオブジェクトを持っています。破棄する必要があります。
            Destroy(instance.gameObject);
            return;
        }
        
        // 空きキューが見つかりました。キャッシュする必要があります。
        freeQ.Enqueue(instance);

        // オブジェクトを非アクティブにします。
        instance.gameObject.SetActive(false);
    }

    public NetworkObjectAcquireResult AcquirePrefabInstance(NetworkRunner runner, in NetworkPrefabAcquireContext context,
        out NetworkObject instance)
    {
        
        instance = null;

        if (DelayIfSceneManagerIsBusy && runner.SceneManager.IsBusy) {
            return NetworkObjectAcquireResult.Retry;
        }

        NetworkObject prefab;
        try {
            prefab = runner.Prefabs.Load(context.PrefabId, isSynchronous: context.IsSynchronous);
        } catch (Exception ex) {
            Log.Error($"Failed to load prefab: {ex}");
            return NetworkObjectAcquireResult.Failed;
        }

        if (!prefab) {
            // これは問題ありません。Fusionがプレハブを即座にロードする必要がない限り
            // このプレハブのインスタンスがまだ必要な場合、このメソッドは次の更新で再び呼び出されます
            return NetworkObjectAcquireResult.Retry;
        }

        instance = InstantiatePrefab(runner, prefab, context.PrefabId);
        Assert.Check(instance);

        if (context.DontDestroyOnLoad) {
            runner.MakeDontDestroyOnLoad(instance.gameObject);
        } else {
            runner.MoveToRunnerScene(instance.gameObject);
        }

        runner.Prefabs.AddInstance(context.PrefabId);
        return NetworkObjectAcquireResult.Success;
    }

    public void ReleaseInstance(NetworkRunner runner, in NetworkObjectReleaseContext context)
    {
        var instance = context.Object;

        // プレハブのみをプールします。
        if (!context.IsBeingDestroyed) {
            if (context.TypeId.IsPrefab) {
                DestroyPrefabInstance(runner, context.TypeId.AsPrefabId, instance);
            }
            else
            {
                Destroy(instance.gameObject);
            }
        }

        if (context.TypeId.IsPrefab) {
            runner.Prefabs.RemoveInstance(context.TypeId.AsPrefabId);
        }
    }

    public void SetMaxPoolCount(int count)
    {
        _maxPoolCount = count;
    }
}
```

---

## まとめ

このFusion Object Poolingサンプルは、ネットワークゲームにおけるパフォーマンス最適化の重要な側面を示しています。適切に実装されたオブジェクトプーリングシステムは、特に多数のオブジェクトが動的に生成・破棄されるゲームにおいて、大幅なパフォーマンス向上をもたらします。

このサンプルコードを基に、プロジェクトの特定のニーズに合わせてカスタマイズしてください。