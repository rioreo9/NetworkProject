using UnityEngine;
using Fusion;
using System.Threading.Tasks;

/// <summary>
/// Photon接続のテスト用スクリプト
/// </summary>
public class PhotonConnectionTest : MonoBehaviour
{
    [SerializeField]
    private bool autoConnect = false;
    
    private NetworkRunner _runner;
    
    private void Start()
    {
        if (autoConnect)
        {
            TestConnection();
        }
    }
    
    /// <summary>
    /// GUI上にボタンを表示して手動で接続テストを実行
    /// </summary>
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 300));
        
        if (GUILayout.Button("接続テスト (AutoRegion)", GUILayout.Height(50)))
        {
            TestConnection();
        }
        
        if (GUILayout.Button("接続テスト (日本リージョン)", GUILayout.Height(50)))
        {
            TestConnection("jp");
        }
        
        if (GUILayout.Button("接続テスト (米国リージョン)", GUILayout.Height(50)))
        {
            TestConnection("us");
        }
        
        if (_runner != null && GUILayout.Button("切断", GUILayout.Height(50)))
        {
            _runner.Shutdown();
            _runner = null;
        }
        
        GUILayout.EndArea();
    }
    
    /// <summary>
    /// 接続テストを実行
    /// </summary>
    private async void TestConnection(string region = "")
    {
        if (_runner != null)
        {
            Debug.LogWarning("既に接続中です");
            return;
        }
        
        // NetworkRunnerを作成
        GameObject runnerGO = new GameObject("TestRunner");
        _runner = runnerGO.AddComponent<NetworkRunner>();
        
        Debug.Log($"接続テスト開始... リージョン: {(string.IsNullOrEmpty(region) ? "Auto" : region)}");
        
        // 接続を試行
        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Shared,
            SessionName = "TestSession",
            PlayerCount = 2
        });
        
        if (result.Ok)
        {
            Debug.Log($"<color=green>接続成功!</color> セッション: {_runner.SessionInfo?.Name}, リージョン: {_runner.SessionInfo?.Region}");
        }
        else
        {
            Debug.LogError($"<color=red>接続失敗!</color> 理由: {result.ShutdownReason}");
            
            Debug.LogError($"エラーメッセージ: {result.ErrorMessage}");
            
            // 失敗時はRunnerを削除
            if (_runner != null)
            {
                Destroy(_runner.gameObject);
                _runner = null;
            }
        }
    }
    
    private void OnDestroy()
    {
        if (_runner != null)
        {
            _runner.Shutdown();
        }
    }
} 
