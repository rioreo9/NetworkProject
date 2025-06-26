using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Linq;

// エディター拡張のクラス
public class AutoSaveConfig : ScriptableObject
{
    public bool isEnabled = true;
    public float saveInterval = 300f; // デフォルトを5分に変更（短すぎると頻繁な保存でパフォーマンスに影響）
    public float minSaveInterval = 60f; // 最小保存間隔を1分に設定

    // 設定アセットのパス
    private const string ASSET_PATH = "Assets/Editor/AutoSaveConfig.asset";

    // 設定の読み込み
    public static AutoSaveConfig GetOrCreateSettings()
    {
        AutoSaveConfig settings = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(ASSET_PATH);
        if (settings == null)
        {
            // 設定ファイルが存在しない場合は新規作成
            settings = ScriptableObject.CreateInstance<AutoSaveConfig>();

            try
            {
                // ディレクトリが存在しない場合は作成
                string directoryPath = Path.GetDirectoryName(ASSET_PATH);
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                AssetDatabase.CreateAsset(settings, ASSET_PATH);
                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutoSave] 設定ファイルの作成に失敗しました: {e.Message}");
                return settings; // 失敗してもインスタンスは返す
            }
        }
        return settings;
    }

    // 設定の保存
    public void Save()
    {
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }
}

[InitializeOnLoad]
public class AutoSave
{
    private static AutoSaveConfig config;
    private static DateTime lastSaveTime;
    private static bool isSubscribed = false;

    // コンストラクタ（エディター起動時に実行）
    static AutoSave()
    {
        // 設定の読み込み
        config = AutoSaveConfig.GetOrCreateSettings();
        lastSaveTime = DateTime.Now;

        // イベント登録
        SubscribeToEvents();

        // プレイモードの監視
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void SubscribeToEvents()
    {
        if (config.isEnabled && !isSubscribed)
        {
            EditorApplication.update += Update;
            isSubscribed = true;
            Debug.Log("[AutoSave] 自動保存機能が有効になりました");
        }
        else if (!config.isEnabled && isSubscribed)
        {
            EditorApplication.update -= Update;
            isSubscribed = false;
            Debug.Log("[AutoSave] 自動保存機能が無効になりました");
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // プレイモード終了時に保存
            if (config.isEnabled)
            {
                SaveAll();
                lastSaveTime = DateTime.Now;
            }
        }
    }

    static void Update()
    {
        if (!config.isEnabled)
            return;

        // 設定された間隔で保存を実行
        if ((DateTime.Now - lastSaveTime).TotalSeconds >= config.saveInterval)
        {
            SaveAll();
            lastSaveTime = DateTime.Now;
        }
    }

    // シーンとアセットの保存を実行
    static void SaveAll()
    {
        if (EditorApplication.isPlaying)
            return;

        bool anySceneDirty = false;
        bool anyAssetDirty = false;

        // シーンの変更を確認
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                anySceneDirty = true;
                break;
            }
        }

        // アセットの変更を確認
        string[] dirtyAssets = AssetDatabase.GetAllAssetPaths()
            .Where(path => EditorUtility.IsDirty(AssetDatabase.LoadMainAssetAtPath(path)))
            .ToArray();
        anyAssetDirty = dirtyAssets.Length > 0;

        // 変更があるシーンのみ保存
        if (anySceneDirty)
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[AutoSave] シーンを自動保存しました: {DateTime.Now}");
        }

        // 変更があるアセットのみ保存
        if (anyAssetDirty)
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"[AutoSave] アセットを自動保存しました: {DateTime.Now}");
        }
    }

    // 設定更新時に呼び出すメソッド
    public static void RefreshSettings()
    {
        config = AutoSaveConfig.GetOrCreateSettings();
        SubscribeToEvents();
    }
}

// 設定用のエディターウィンドウ
public class AutoSaveSettingsWindow : EditorWindow
{
    private AutoSaveConfig config;

    [MenuItem("Tools/Auto Save/Settings")]
    public static void ShowWindow()
    {
        GetWindow<AutoSaveSettingsWindow>("Auto Save Settings");
    }

    void OnEnable()
    {
        // ウィンドウが開かれた時に設定を読み込む
        config = AutoSaveConfig.GetOrCreateSettings();
    }

    void OnGUI()
    {
        if (config == null)
        {
            config = AutoSaveConfig.GetOrCreateSettings();
        }

        EditorGUI.BeginChangeCheck();

        config.isEnabled = EditorGUILayout.Toggle("自動保存を有効化", config.isEnabled);

        // 最小値を制限したスライダーで保存間隔を設定
        float newInterval = EditorGUILayout.Slider("保存間隔 (秒)", config.saveInterval,
                                                 config.minSaveInterval, 1800f);

        // 値が変わっていたら更新
        if (newInterval != config.saveInterval)
        {
            config.saveInterval = newInterval;
        }

        if (EditorGUI.EndChangeCheck())
        {
            // 値が変更された場合は設定を保存
            config.Save();
            AutoSave.RefreshSettings();
        }

        // 情報表示
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorGUILayout.HelpBox($"現在の設定:\n・自動保存: {(config.isEnabled ? "有効" : "無効")}\n・保存間隔: {config.saveInterval:0.0}秒 ({config.saveInterval / 60:0.0}分)",
                              MessageType.Info);

        EditorGUILayout.Space(10);
        if (GUILayout.Button("今すぐ保存"))
        {
            EditorSceneManager.SaveOpenScenes();
            AssetDatabase.SaveAssets();
            Debug.Log($"[AutoSave] プロジェクトを手動で保存しました: {DateTime.Now}");
        }
    }

    void OnDestroy()
    {
        // ウィンドウが閉じられた時に設定が変更されていればマークして保存
        if (config != null && EditorUtility.IsDirty(config))
        {
            config.Save();
        }
    }
}
