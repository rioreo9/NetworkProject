using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System;
using System.IO;

// 設定クラス
public class AutoSaveConfig : ScriptableObject
{
    [SerializeField] private bool isEnabled = true;
    [SerializeField] private float saveInterval = 300f;
    [SerializeField] private float minSaveInterval = 60f;

    public bool IsEnabled
    {
        get => isEnabled;
        set => isEnabled = value;
    }

    public float SaveInterval
    {
        get => saveInterval;
        set => saveInterval = Mathf.Max(value, minSaveInterval);
    }

    public float MinSaveInterval => minSaveInterval;

    private const string ASSET_PATH = "Assets/Editor/AutoSaveConfig.asset";

    public static AutoSaveConfig GetOrCreateSettings()
    {
        AutoSaveConfig settings = AssetDatabase.LoadAssetAtPath<AutoSaveConfig>(ASSET_PATH);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<AutoSaveConfig>();

            try
            {
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
                Debug.LogError($"[AutoSave] 設定ファイルの作成に失敗: {e.Message}");
            }
        }
        return settings;
    }

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
    private static bool hasUnsavedChanges = false;

    public static DateTime LastSaveTime => lastSaveTime;

    static AutoSave()
    {
        config = AutoSaveConfig.GetOrCreateSettings();
        lastSaveTime = DateTime.Now;

        SubscribeToEvents();
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.quitting += OnEditorQuitting;

        // 変更検知用のイベント登録
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.projectChanged += OnProjectChanged;
        Undo.undoRedoPerformed += OnUndoRedoPerformed;

        // ドメインリロード対策
        AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
    }

    private static void SubscribeToEvents()
    {
        if (config.IsEnabled && !isSubscribed)
        {
            EditorApplication.update += Update;
            isSubscribed = true;
            Debug.Log("[AutoSave] 自動保存機能が有効になりました");
        }
        else if (!config.IsEnabled && isSubscribed)
        {
            EditorApplication.update -= Update;
            isSubscribed = false;
            Debug.Log("[AutoSave] 自動保存機能が無効になりました");
        }
    }

    // 変更検知用のイベントハンドラー
    private static void OnHierarchyChanged()
    {
        hasUnsavedChanges = true;
    }

    private static void OnProjectChanged()
    {
        hasUnsavedChanges = true;
    }

    private static void OnUndoRedoPerformed()
    {
        hasUnsavedChanges = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode && config.IsEnabled)
        {
            SaveAll();
            lastSaveTime = DateTime.Now;
        }
    }

    private static void OnEditorQuitting()
    {
        if (config.IsEnabled)
        {
            SaveAll();
            Debug.Log("[AutoSave] エディター終了時に保存しました");
        }
    }

    private static void OnBeforeAssemblyReload()
    {
        if (isSubscribed)
        {
            EditorApplication.update -= Update;
            isSubscribed = false;
        }
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.quitting -= OnEditorQuitting;
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        EditorApplication.projectChanged -= OnProjectChanged;
        Undo.undoRedoPerformed -= OnUndoRedoPerformed;
    }

    static void Update()
    {
        if (!config.IsEnabled || EditorApplication.isPlaying)
            return;

        // 設定された間隔で保存を実行
        if ((DateTime.Now - lastSaveTime).TotalSeconds >= config.SaveInterval)
        {
            // 変更がある場合のみ保存処理を実行
            if (hasUnsavedChanges || HasDirtyScenes())
            {
                SaveAll();
                lastSaveTime = DateTime.Now;
                hasUnsavedChanges = false;
            }
            else
            {
                // 変更がない場合も時間をリセット
                lastSaveTime = DateTime.Now;
            }
        }
    }

    // シーンの変更確認（軽量）
    private static bool HasDirtyScenes()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).isDirty)
            {
                return true;
            }
        }
        return false;
    }

    // 軽量化された保存処理
    static void SaveAll()
    {
        if (EditorApplication.isPlaying)
            return;

        bool sceneSaved = false;

        // シーンの保存
        if (HasDirtyScenes())
        {
            EditorSceneManager.SaveOpenScenes();
            Debug.Log($"[AutoSave] シーン保存: {DateTime.Now:HH:mm:ss}");
            sceneSaved = true;
        }

        // アセットの保存（常に実行して安全に）
        // AssetDatabase.SaveAssetsは内部で変更チェックを行うため軽量
        AssetDatabase.SaveAssets();

        if (!sceneSaved) // シーンが保存されていない場合のみログ出力
        {
            Debug.Log($"[AutoSave] アセット保存: {DateTime.Now:HH:mm:ss}");
        }
    }

    public static void RefreshSettings()
    {
        config = AutoSaveConfig.GetOrCreateSettings();
        SubscribeToEvents();
    }

    public static void ForceSave()
    {
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        lastSaveTime = DateTime.Now;
        hasUnsavedChanges = false;
        Debug.Log($"[AutoSave] 手動保存完了: {DateTime.Now:HH:mm:ss}");
    }
}

// 設定ウィンドウ
public class AutoSaveSettingsWindow : EditorWindow
{
    private AutoSaveConfig config;
    private double nextRepaintTime = 0;

    [MenuItem("Tools/Auto Save/Settings")]
    public static void ShowWindow()
    {
        var window = GetWindow<AutoSaveSettingsWindow>("Auto Save Settings");
        window.minSize = new Vector2(300, 250);
    }

    void OnEnable()
    {
        config = AutoSaveConfig.GetOrCreateSettings();
    }

    void OnGUI()
    {
        if (config == null)
        {
            EditorGUILayout.HelpBox("設定の読み込みに失敗しました", MessageType.Error);
            return;
        }

        EditorGUI.BeginChangeCheck();

        EditorGUILayout.LabelField("自動保存設定", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        config.IsEnabled = EditorGUILayout.Toggle("自動保存を有効化", config.IsEnabled);

        EditorGUI.BeginDisabledGroup(!config.IsEnabled);

        float minutes = config.SaveInterval / 60f;
        float newMinutes = EditorGUILayout.Slider("保存間隔 (分)", minutes, 1f, 30f);
        config.SaveInterval = newMinutes * 60f;

        EditorGUI.EndDisabledGroup();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(config);
            AutoSave.RefreshSettings();
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // ステータス表示
        string statusText = $"状態: {(config.IsEnabled ? "有効" : "無効")}\n" +
                           $"保存間隔: {config.SaveInterval:0}秒 ({config.SaveInterval / 60:0.0}分)";

        EditorGUILayout.HelpBox(statusText, MessageType.Info);

        // カウントダウン表示
        if (config.IsEnabled && !EditorApplication.isPlaying)
        {
            var timeSinceLastSave = DateTime.Now - AutoSave.LastSaveTime;
            var timeUntilNextSave = TimeSpan.FromSeconds(config.SaveInterval) - timeSinceLastSave;

            if (timeUntilNextSave.TotalSeconds > 0)
            {
                EditorGUILayout.LabelField($"次回保存まで: {timeUntilNextSave:mm\\:ss}");

                // 1秒に1回だけRepaint
                if (EditorApplication.timeSinceStartup > nextRepaintTime)
                {
                    nextRepaintTime = EditorApplication.timeSinceStartup + 1.0;
                    Repaint();
                }
            }
            else
            {
                EditorGUILayout.LabelField("次回保存まで: まもなく");
            }
        }

        EditorGUILayout.Space(15);

        if (GUILayout.Button("今すぐ保存", GUILayout.Height(30)))
        {
            AutoSave.ForceSave();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("※ 変更がある場合のみ自動保存されます", EditorStyles.miniLabel);
    }

    void OnDestroy()
    {
        if (config != null && EditorUtility.IsDirty(config))
        {
            config.Save();
        }
    }
}
