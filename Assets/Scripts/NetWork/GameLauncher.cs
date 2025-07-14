using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using R3;
using UnityEngine.SceneManagement;
using System.Linq;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField, Required]
    private NetworkRunner _networkRunnerPrefab;
    [SerializeField, Required]
    private NetworkPrefabRef _player;
    [SerializeField, Required]
    private InputManager _inputManager;
    [SerializeField, Required]
    private string _sesionName = "Test";

    [SerializeField]
    private bool _shared = false; // シェアードモードを使用するかどうか

    private NetworkRunner _runner;

    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private bool _isRunning;

    private PlayerNetworkInput _networkInput = new PlayerNetworkInput();


    private void Start()
    {
        // インターネット接続状態を確認
        StartCoroutine(CheckInternetConnection());

        if (_shared)
        {
            StartGame(GameMode.Shared);
        }
    }

    /// <summary>
    /// インターネット接続状態を確認する
    /// </summary>
    private IEnumerator CheckInternetConnection()
    {
        // Photonのマスターサーバーへの接続テスト
        using (var www = UnityEngine.Networking.UnityWebRequest.Get("https://www.google.com"))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError($"インターネット接続エラー: {www.error}");
                Debug.LogError("インターネット接続を確認してください");
            }
            else
            {
                Debug.Log("インターネット接続: OK");
            }
        }
    }

    async void StartGame(GameMode mode)
    {
        //NetworkRunnerを生成する
        _runner = Instantiate(_networkRunnerPrefab);

        _runner.AddCallbacks(this);

        _runner.ProvideInput = true;

        Debug.Log($"サーバー起動: セッション名 = {_sesionName}, モード = {mode}");

        // ネットワーク用のシーンの設定
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // セッションの参加
        StartGameResult result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = _sesionName,
            Scene = sceneInfo,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });

        if (result.Ok)
        {
            Debug.Log($"サーバー起動成功: セッション名 = {_runner.SessionInfo?.Name}");
        }
        else
        {
            Debug.LogError($"サーバー起動失敗: {result.ShutdownReason}");
            // エラーの詳細情報を出力
            Debug.LogError($"エラーメッセージ: {result.ErrorMessage}");
        }
    }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // アバターの初期位置を計算する（半径5の円の内部のランダムな点）
        Vector2 rand = UnityEngine.Random.insideUnitCircle * 5f;
        Vector3 spawnPosition = new Vector3(rand.x, 2f, rand.y);

        if (_shared && player == runner.LocalPlayer || runner.IsServer)
        {
            // 自分自身のアバターをスポーンする
            var spawnedObject = runner.Spawn(_player, spawnPosition, Quaternion.identity, player);

            // プレイヤー（PlayerRef）とアバター（spawnedObject）を関連付ける
            runner.SetPlayerObject(player, spawnedObject);

            Debug.Log($"プレイヤー {player} が参加しました。アバターをスポーンしました: {spawnedObject}");
        }
    }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) { return; }
        // 退出したプレイヤーのアバターを破棄する
        if (runner.TryGetPlayerObject(player, out var avatar))
        {
            runner.Despawn(avatar);
        }
    }

    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (_inputManager != null)
        {
            _networkInput = _inputManager.NetworkInput; // InputManagerからネットワーク入力を取得

            _inputManager.UpdateNetWorkInput(); // InputManagerからネットワーク入力を更新
            _inputManager.ResetButtonInputs();

            // ネットワーク入力として設定
            input.Set(_networkInput);
        }
    }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.LogError($"Input Missing - Player: {player}, LocalPlayer: {runner.LocalPlayer}, " +
                      $"ProvideInput: {runner.ProvideInput}, IsClient: {runner.IsClient}, " +
                      $"IsServer: {runner.IsServer}");
    }
    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        // シャットダウン時の詳細情報をログ出力
        Debug.LogWarning($"NetworkRunner シャットダウン: {shutdownReason}");
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
        // サーバー接続成功時のログ
        Debug.Log("サーバーへの接続に成功しました");
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        // サーバーから切断された時の詳細情報をログ出力
        Debug.LogError($"サーバーから切断されました: {reason}");
    }

    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        // 接続失敗時の詳細情報をログ出力
        Debug.LogError($"接続に失敗しました: {reason}");
        Debug.LogError($"リモートアドレス: {remoteAddress}");
    }
    void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }

    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 1000, 200), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 200, 1000, 200), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
        else
        {
            // NetworkRunner状態の詳細表示
            GUI.Label(new Rect(10, 10, 400, 30), $"IsClient: {_runner.IsClient}");
            GUI.Label(new Rect(10, 40, 400, 30), $"IsServer: {_runner.IsServer}");
            GUI.Label(new Rect(10, 70, 400, 30), $"LocalPlayer: {_runner.LocalPlayer}");
            GUI.Label(new Rect(10, 100, 400, 30), $"ProvideInput: {_runner.ProvideInput}");
            GUI.Label(new Rect(10, 130, 400, 30), $"IsRunning: {_runner.IsRunning}");
            GUI.Label(new Rect(10, 160, 400, 30), $"Tick: {_runner.Tick}");
            GUI.Label(new Rect(10, 190, 400, 30), $"ActivePlayers: {_runner.ActivePlayers.Count()}");
        }
    }

}
