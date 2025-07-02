using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;
using R3;

public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
{
    [SerializeField, Required]
    private NetworkRunner networkRunnerPrefab;
    [SerializeField, Required]
    private NetworkPrefabRef _player;
    [SerializeField, Required]
    private InputManager _inputManager;

    private NetworkRunner _runner;

    private PlayerNetworkInput _networkInput = new PlayerNetworkInput();


    private async void Start()
    {
        // インターネット接続状態を確認
        StartCoroutine(CheckInternetConnection());

        await DoConnectNet();
    }

    private void Update()
    {
        _networkInput = _inputManager.NetworkInput; // InputManagerからネットワーク入力を取得

        _networkInput.RunPressed = Input.GetKeyDown(KeyCode.LeftShift); // 走行ボタンの入力を取得
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

    private async Task DoConnectNet()
    {
        // NetworkRunnerを生成する
        _runner = Instantiate(networkRunnerPrefab);
        // 共有モードのセッションに参加する
        _runner.AddCallbacks(this);

        StartGameResult result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            // セッション名を明示的に指定（ランダムな部屋に参加）
            SessionName = "1",
            // プレイヤー数を指定
            PlayerCount = 4,
            // カスタムロビー名（オプション）
            CustomLobbyName = "DefaultLobby",
         
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
        // セッションへ参加したプレイヤーが自分自身かどうかを判定する
        if (player == runner.LocalPlayer)
        {
            // アバターの初期位置を計算する（半径5の円の内部のランダムな点）
            Vector2 rand = UnityEngine.Random.insideUnitCircle * 5f;
            Vector3 spawnPosition = new Vector3(rand.x, 2f, rand.y);
            // 自分自身のアバターをスポーンする
            var spawnedObject =  runner.Spawn(_player, spawnPosition, Quaternion.identity, player);
        }
    }
    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (_inputManager != null)
        {
            // ネットワーク入力として設定
            input.Set(_networkInput);
        }
    }
    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
        Debug.LogWarning($"プレイヤー {player} の入力が不足しています");
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

}
