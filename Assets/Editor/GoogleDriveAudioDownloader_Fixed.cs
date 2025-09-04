using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

public class GoogleDriveAudioDownloader : EditorWindow
{
    private const string EditorPrefsKey = "GoogleDrive.PackageSaveDir";
    private const string EditorPrefsKeyIsRelative = "GoogleDrive.IsRelativePath";
    private const string EditorPrefsKeyLocalFolder = "GoogleDrive.LocalImportFolder"; // deprecated
    private const string EditorPrefsKeyLocalRecursive = "GoogleDrive.LocalImportRecursive"; // deprecated
    private const string EditorPrefsKeySequential = "GoogleDrive.SequentialImport";
    private const double SequentialImportTimeoutSeconds = 12.0;

    private string _fileUrlOrId = string.Empty;
    private string _multiUrls = string.Empty;
    private string _targetFolder = string.Empty;
    private bool _isRelativePath = true;
    private bool _overwrite = false;
    private bool _importInteractive = true;
    private bool _sequentialImport = true;
    private bool _deleteAfterImport = true;
    private string _lastMessage = string.Empty;
    private Vector2 _scrollPosition;

    // (ローカル一括インポート機能は削除)

    // ===== 一括インポート順次処理用 =====
    private readonly Queue<string> _sequentialQueue = new Queue<string>();
    private readonly Queue<string> _sequentialInputQueue = new Queue<string>();
    private bool _isSequentialRunning = false;
    private int _sequentialTotal = 0;
    private int _sequentialImported = 0;
    private bool _awaitingSequentialImport = false;
    private string _currentSequentialFile = null;
    private bool _waitingForUserNext = false;
    private double _sequentialWaitStartTime = 0.0;
    private bool _sequentialManualAdvance = true;

    [MenuItem("Tools/Packages/Import .unitypackage from Drive...")]
    public static void ShowWindow()
    {
        var w = GetWindow<GoogleDriveAudioDownloader>("Google Drive Package");
        w.minSize = new Vector2(650, 450);
    }

    private void OnEnable()
    {
        // 保存先の復元とデフォルト設定
        var saved = EditorPrefs.GetString(EditorPrefsKey, string.Empty);
        _isRelativePath = EditorPrefs.GetBool(EditorPrefsKeyIsRelative, true);
        
        // 一括インポート時は常に順次処理（固定設定）
        _sequentialImport = true;
        _sequentialManualAdvance = true;
        
        if (!string.IsNullOrEmpty(saved))
            _targetFolder = saved;

        if (string.IsNullOrEmpty(_targetFolder))
        {
            // デフォルトは相対パス
            _targetFolder = "DownloadedUnityPackages";
            _isRelativePath = true;
        }

        // ローカル一括インポート関連は削除

        // 順次インポート用イベント購読をセット
        AssetDatabase.importPackageCompleted -= OnPackageCompleted;
        AssetDatabase.importPackageCancelled -= OnPackageCancelled;
        AssetDatabase.importPackageFailed -= OnPackageFailed;
        AssetDatabase.importPackageCompleted += OnPackageCompleted;
        AssetDatabase.importPackageCancelled += OnPackageCancelled;
        AssetDatabase.importPackageFailed += OnPackageFailed;
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(EditorPrefsKey, _targetFolder ?? string.Empty);
        EditorPrefs.SetBool(EditorPrefsKeyIsRelative, _isRelativePath);

        // クリーンアップ
        EditorApplication.update -= ProcessSequentialInteractiveImport;
        EditorApplication.update -= ProcessSequentialQueue;
        AssetDatabase.importPackageCompleted -= OnPackageCompleted;
        AssetDatabase.importPackageCancelled -= OnPackageCancelled;
        AssetDatabase.importPackageFailed -= OnPackageFailed;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Google Drive から UnityPackage をインポート", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox("共有リンク / ファイルID / .unitypackage の直URLを指定できます。APIキーは不要です。", MessageType.Info);

        EditorGUILayout.Space();

        // 保存先設定
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("保存先設定", EditorStyles.boldLabel);
            
            // 相対パス/絶対パス選択
            var newIsRelative = EditorGUILayout.ToggleLeft("プロジェクト相対パスを使用（推奨）", _isRelativePath);
            if (newIsRelative != _isRelativePath)
            {
                _isRelativePath = newIsRelative;
                // パス形式変更時のデフォルト値設定
                if (_isRelativePath)
                {
                    _targetFolder = "DownloadedUnityPackages";
                }
                else
                {
                    var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    _targetFolder = Path.Combine(projectRoot, "DownloadedUnityPackages").Replace('\\', '/');
                }
            }
            
            // パス入力欄
            using (new EditorGUILayout.HorizontalScope())
            {
                var label = _isRelativePath ? "相対パス（プロジェクトルートから）" : "絶対パス";
                _targetFolder = EditorGUILayout.TextField(new GUIContent(label), _targetFolder);
                if (GUILayout.Button("選択", GUILayout.Width(64)))
                {
                    string startPath;
                    if (_isRelativePath)
                    {
                        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                        startPath = string.IsNullOrEmpty(_targetFolder) ? projectRoot : Path.Combine(projectRoot, _targetFolder);
                    }
                    else
                    {
                        startPath = string.IsNullOrEmpty(_targetFolder) ? Application.dataPath : _targetFolder;
                    }
                    
                    var selected = EditorUtility.OpenFolderPanel("保存先フォルダを選択", startPath, "");
                    if (!string.IsNullOrEmpty(selected))
                    {
                        if (_isRelativePath)
                        {
                            // 絶対パスを相対パスに変換
                            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace('\\', '/');
                            var selectedNorm = selected.Replace('\\', '/');
                            if (selectedNorm.StartsWith(projectRoot))
                            {
                                _targetFolder = selectedNorm.Substring(projectRoot.Length).TrimStart('/');
                            }
                            else
                            {
                                EditorUtility.DisplayDialog("警告", "プロジェクトフォルダ内を選択してください。", "OK");
                            }
                        }
                        else
                        {
                            _targetFolder = selected.Replace('\\', '/');
                        }
                    }
                }
            }
            
            // 実際の保存先パス表示
            var actualPath = GetActualSavePath();
            EditorGUILayout.HelpBox($"実際の保存先: {actualPath}", MessageType.Info);
        }

        _overwrite = EditorGUILayout.ToggleLeft("同名ファイルがあれば上書き", _overwrite);
        _importInteractive = EditorGUILayout.ToggleLeft("インポート時にダイアログを表示", _importInteractive);
        
        // 一括インポート時は常に順次処理（デフォルト動作）
        _sequentialImport = true;
        _sequentialManualAdvance = true;
        _deleteAfterImport = EditorGUILayout.ToggleLeft("インポート後に .unitypackage を削除", _deleteAfterImport);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "注意：Package Manager依存関係の警告が表示される場合があります。\n" +
            "これは正常で、必要なパッケージ（HDRP、URP等）がインストールされていれば問題ありません。", 
            MessageType.Info);

        EditorGUILayout.Space();

        // 単一URL
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("単一URLのダウンロード＆インポート", EditorStyles.boldLabel);
            _fileUrlOrId = EditorGUILayout.TextField(new GUIContent("共有リンク / ファイルID / 直URL(.unitypackage)"), _fileUrlOrId);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_fileUrlOrId)))
                {
                    if (GUILayout.Button("ダウンロードしてインポート", GUILayout.Height(26), GUILayout.Width(220)))
                    {
                        DownloadSingleFile();
                    }
                }
            }
        }

        // 複数URL一括
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("複数URLを一括インポート", EditorStyles.boldLabel);
            
            // ファイル管理ボタン
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("URLリスト管理:", GUILayout.Width(100));
                if (GUILayout.Button("保存", GUILayout.Width(60)))
                {
                    SaveUrlsToFile();
                }
                if (GUILayout.Button("読込", GUILayout.Width(60)))
                {
                    LoadUrlsFromFile();
                }
                if (GUILayout.Button("新規", GUILayout.Width(60)))
                {
                    CreateNewUrlFile();
                }
                GUILayout.FlexibleSpace();
            }
            
            // URLリスト情報表示
            var lineCount = string.IsNullOrEmpty(_multiUrls) ? 0 : _multiUrls.Split('\n').Length;
            var validUrlCount = string.IsNullOrEmpty(_multiUrls) ? 0 : 
                _multiUrls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Count(line => !string.IsNullOrWhiteSpace(line) && !line.Trim().StartsWith("#"));
            
            EditorGUILayout.LabelField($"1行に1つのURL/ID を入力（# でコメント行） - 総行数: {lineCount}, 有効URL: {validUrlCount}");
            
            // スクロール可能なテキストエリア
            using (var scrollView = new EditorGUILayout.ScrollViewScope(_scrollPosition, 
                GUI.skin.box, 
                GUILayout.MinHeight(120), 
                GUILayout.MaxHeight(250)))
            {
                _scrollPosition = scrollView.scrollPosition;
                
                // テキストエリアのスタイル設定
                var textAreaStyle = new GUIStyle(EditorStyles.textArea)
                {
                    wordWrap = false,  // 長いURLの場合は横スクロールを許可
                    fontSize = 11
                };
                
                _multiUrls = EditorGUILayout.TextArea(_multiUrls, textAreaStyle, GUILayout.ExpandHeight(true));
            }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_multiUrls)))
                {
                    if (GUILayout.Button("一括ダウンロードしてインポート", GUILayout.Height(28), GUILayout.Width(240)))
                    {
                        ImportMultiple();
                    }
                }
            }
        }

        // ローカル一括インポートUIは削除

        if (!string.IsNullOrEmpty(_lastMessage))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_lastMessage, MessageType.Info);
        }

        // 手動進行ボタン（順次＋対話＋手動モード時、進行待ち or インポート待ちでも表示）
        if (_importInteractive && _sequentialImport && (_waitingForUserNext || (_sequentialManualAdvance && _awaitingSequentialImport)))
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("次へ", GUILayout.Width(120), GUILayout.Height(28)))
                {
                    // タイムアウトを待たずに次へ進む
                    _waitingForUserNext = false;
                    if (_awaitingSequentialImport)
                    {
                        _awaitingSequentialImport = false;
                        _sequentialWaitStartTime = 0.0;
                        EditorUtility.ClearProgressBar();
                    }
                    Repaint();
                }
                GUILayout.FlexibleSpace();
            }
        }
    }

    // ローカル一括インポート機能は削除しました

    //================ 単一URL ==================
    private void DownloadSingleFile()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_fileUrlOrId))
            {
                EditorUtility.DisplayDialog("入力エラー", "有効な共有リンク/ファイルID/直URL(.unitypackage)を入力してください。", "OK");
                return;
            }

            EnsureTargetFolder();

            string url;
            string fallbackName;
            BuildDownloadUrlAndFallback(_fileUrlOrId, out url, out fallbackName);

            string fullPath = DownloadPackageToFile(url, fallbackName, 600);

            AssetDatabase.ImportPackage(fullPath, _importInteractive);

            if (_deleteAfterImport)
            {
                try 
                { 
                    File.Delete(fullPath); 
                    Debug.Log($"[GoogleDrive] ファイル削除完了: {Path.GetFileName(fullPath)}");
                } 
                catch (Exception e) 
                { 
                    Debug.LogWarning($"[GoogleDrive] ファイル削除失敗: {Path.GetFileName(fullPath)} - {e.Message}");
                }
            }

            _lastMessage = $"インポートしました: {Path.GetFileName(fullPath)}";
            Debug.Log("[GoogleDrive] " + _lastMessage);
        }
        catch (OperationCanceledException)
        {
            // スキップ
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[GoogleDrive] " + e);
            EditorUtility.DisplayDialog("エラー", e.Message, "OK");
            _lastMessage = e.Message;
        }
    }

    //================ URLファイル管理 ==================
    private string GetUrlListDirectory()
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var urlDir = Path.Combine(projectRoot, "PackageUrlLists");
        if (!Directory.Exists(urlDir))
        {
            Directory.CreateDirectory(urlDir);
        }
        return urlDir;
    }

    private void SaveUrlsToFile()
    {
        if (string.IsNullOrWhiteSpace(_multiUrls))
        {
            EditorUtility.DisplayDialog("保存エラー", "保存するURLリストが空です。", "OK");
            return;
        }

        var defaultFileName = "PackageUrls_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        var savePath = EditorUtility.SaveFilePanel(
            "URLリストを保存", 
            GetUrlListDirectory(), 
            defaultFileName, 
            "txt"
        );

        if (!string.IsNullOrEmpty(savePath))
        {
            try
            {
                var content = GenerateUrlFileContent(_multiUrls);
                File.WriteAllText(savePath, content, System.Text.Encoding.UTF8);
                _lastMessage = $"URLリストを保存しました: {Path.GetFileName(savePath)}";
                Debug.Log("[GoogleDrive] " + _lastMessage);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("保存エラー", e.Message, "OK");
                Debug.LogError("[GoogleDrive] " + e);
            }
        }
    }

    private void LoadUrlsFromFile()
    {
        var loadPath = EditorUtility.OpenFilePanel(
            "URLリストを読み込み", 
            GetUrlListDirectory(), 
            "txt"
        );

        if (!string.IsNullOrEmpty(loadPath))
        {
            try
            {
                var content = File.ReadAllText(loadPath, System.Text.Encoding.UTF8);
                _multiUrls = ParseUrlFileContent(content);
                _lastMessage = $"URLリストを読み込みました: {Path.GetFileName(loadPath)}";
                Debug.Log("[GoogleDrive] " + _lastMessage);
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("読み込みエラー", e.Message, "OK");
                Debug.LogError("[GoogleDrive] " + e);
            }
        }
    }

    private void CreateNewUrlFile()
    {
        _multiUrls = GenerateUrlFileTemplate();
        _lastMessage = "新しいURLリストテンプレートを作成しました。";
    }

    private string GenerateUrlFileContent(string urls)
    {
        var lines = new List<string>
        {
            "# Unity Package URL List",
            "# Generated on " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            "# ",
            "# 使用方法:",
            "# - 1行に1つのURL/ファイルIDを記述",
            "# - # で始まる行はコメント（無視されます）",
            "# - 空行も無視されます",
            "# ",
            ""
        };

        var urlLines = urls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in urlLines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                lines.Add(trimmed);
            }
        }

        return string.Join("\n", lines);
    }

    private string ParseUrlFileContent(string content)
    {
        var validUrls = new List<string>();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            // コメント行と空行をスキップ
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#"))
            {
                validUrls.Add(trimmed);
            }
        }

        return string.Join("\n", validUrls);
    }

    private string GenerateUrlFileTemplate()
    {
        return string.Join("\n", new[]
        {
            "# Unity Package URL List",
            "# ",
            "# 使用方法:",
            "# - 1行に1つのURL/ファイルIDを記述",
            "# - # で始まる行はコメント（無視されます）",
            "# - 空行も無視されます",
            "# ",
            "# 例:",
            "# https://drive.google.com/file/d/YOUR_FILE_ID/view",
            "# YOUR_FILE_ID_ONLY",
            "# https://example.com/package.unitypackage",
            "",
            "# ここにURLやファイルIDを追加してください:",
            ""
        });
    }

    //================ 複数URL一括 ==================
    private void ImportMultiple()
    {
        try
        {
            EnsureTargetFolder();

            var allLines = string.IsNullOrWhiteSpace(_multiUrls)
                ? Array.Empty<string>()
                : _multiUrls.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // コメント行と空行を除外
            var validLines = allLines
                .Select(line => line.Trim())
                .Where(line => !string.IsNullOrEmpty(line) && !line.StartsWith("#"))
                .ToArray();

            if (validLines.Length == 0)
            {
                EditorUtility.DisplayDialog("入力エラー", "有効なURL/IDが見つかりません。コメント行（#）以外の行を入力してください。", "OK");
                return;
            }

            // 対話 + 順次 の場合は「1件ずつダウンロード→即インポート」
            if (_importInteractive && _sequentialImport)
            {
                // 完全同期: URLをキューに積んで、1件ずつ「DL→インポート→完了待ち」
                _sequentialInputQueue.Clear();
                foreach (var l in validLines) _sequentialInputQueue.Enqueue(l);
                _sequentialTotal = _sequentialInputQueue.Count;
                _sequentialImported = 0;
                _isSequentialRunning = true;
                _awaitingSequentialImport = false;
                _currentSequentialFile = null;
                EditorApplication.update -= ProcessSequentialInteractiveImport;
                EditorApplication.update += ProcessSequentialInteractiveImport;
                _lastMessage = $"順次インポートを開始します（{_sequentialTotal} 件）";
                Debug.Log("[GoogleDrive] " + _lastMessage);
                return;
            }

            // ダウンロード済みファイルのリスト（非対話 もしくは 非順次）
            var downloadedFiles = new List<string>();

            // まず全ファイルをダウンロード
            for (int i = 0; i < validLines.Length; i++)
            {
                string input = validLines[i];

                try
                {
                    EditorUtility.DisplayProgressBar("Google Drive", $"ダウンロード中 ({i + 1}/{validLines.Length}) {input}", (float)i / validLines.Length);
                    string url;
                    string fallbackName;
                    BuildDownloadUrlAndFallback(input, out url, out fallbackName);

                    string fullPath = DownloadPackageToFile(url, fallbackName, 600);
                    downloadedFiles.Add(fullPath);
                }
                catch (OperationCanceledException)
                {
                    // ユーザーがスキップ
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GoogleDrive] ダウンロード失敗 {input}: {e.Message}");
                }
            }

            // 次にインポート
            if (_importInteractive && _sequentialImport)
            {
                // 対話＋順次: キューに積んで Update で1つずつ ImportPackage ダイアログを出す
                _sequentialQueue.Clear();
                foreach (var f in downloadedFiles) _sequentialQueue.Enqueue(f);
                _sequentialTotal = _sequentialQueue.Count;
                _sequentialImported = 0;
                _isSequentialRunning = true;
                EditorApplication.update -= ProcessSequentialQueue;
                EditorApplication.update += ProcessSequentialQueue;
                _lastMessage = $"順次インポートを開始します（{_sequentialTotal} 件）";
                Debug.Log("[GoogleDrive] " + _lastMessage);
            }
            else
            {
                // 非対話 or 非順次: 従来通りループで実行
                for (int i = 0; i < downloadedFiles.Count; i++)
                {
                    var filePath = downloadedFiles[i];
                    var fileName = Path.GetFileName(filePath);
                    try
                    {
                        if (_importInteractive)
                        {
                            EditorUtility.ClearProgressBar();
                        }
                        else
                        {
                            EditorUtility.DisplayProgressBar("Google Drive", $"インポート中 ({i + 1}/{downloadedFiles.Count}) {fileName}", (float)i / downloadedFiles.Count);
                        }
                        AssetDatabase.ImportPackage(filePath, _importInteractive);
                        AssetDatabase.Refresh();
                        System.Threading.Thread.Sleep(1000);
                        if (_deleteAfterImport) 
                        { 
                            try 
                            { 
                                File.Delete(filePath); 
                                Debug.Log($"[GoogleDrive] ファイル削除完了: {Path.GetFileName(filePath)}");
                            } 
                            catch (Exception e) 
                            { 
                                Debug.LogWarning($"[GoogleDrive] ファイル削除失敗: {Path.GetFileName(filePath)} - {e.Message}");
                            }
                        }
                        Debug.Log($"[GoogleDrive] インポート完了: {fileName}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[GoogleDrive] インポート失敗 {fileName}: {e.Message}");
                    }
                }
                EditorUtility.ClearProgressBar();
                _lastMessage = $"一括インポートが完了しました。成功: {downloadedFiles.Count}個";
                Debug.Log("[GoogleDrive] " + _lastMessage);
            }
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[GoogleDrive] " + e);
            EditorUtility.DisplayDialog("エラー", e.Message, "OK");
            _lastMessage = e.Message;
        }
    }

    private void ProcessSequentialQueue()
    {
        if (!_isSequentialRunning) return;

        // すでにダイアログが開いていそうなフレームはスキップ
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        try
        {
            if (_sequentialQueue.Count == 0)
            {
                // 完了
                _isSequentialRunning = false;
                EditorApplication.update -= ProcessSequentialQueue;
                EditorUtility.ClearProgressBar();
                _lastMessage = $"一括インポートが完了しました。成功: {_sequentialImported}個";
                Debug.Log("[GoogleDrive] " + _lastMessage);
                Repaint();
                return;
            }

            var filePath = _sequentialQueue.Dequeue();
            var fileName = Path.GetFileName(filePath);

            // 対話型ダイアログを確実に出すため、ここでインポート（1フレームに1件）
            EditorUtility.ClearProgressBar();
            AssetDatabase.ImportPackage(filePath, true);
            AssetDatabase.Refresh();
            System.Threading.Thread.Sleep(200);
            if (_deleteAfterImport) 
            { 
                try 
                { 
                    File.Delete(filePath); 
                    Debug.Log($"[GoogleDrive] ファイル削除完了: {Path.GetFileName(filePath)}");
                } 
                catch (Exception e) 
                { 
                    Debug.LogWarning($"[GoogleDrive] ファイル削除失敗: {Path.GetFileName(filePath)} - {e.Message}");
                }
            }
            _sequentialImported++;
            Debug.Log($"[GoogleDrive] インポート完了: {fileName} ({_sequentialImported}/{_sequentialTotal})");
        }
        catch (Exception e)
        {
            Debug.LogError("[GoogleDrive] 順次インポート中のエラー: " + e.Message);
        }
    }

    // 完全同期の対話型順次: URLキューを1件ずつDL→Import→完了イベント待ち
    private void ProcessSequentialInteractiveImport()
    {
        if (!_isSequentialRunning) return;

        // インポート待ち中は次へ進まない
        if (_awaitingSequentialImport)
        {
            // タイムアウト監視（NothingImport対策）
            var elapsed = EditorApplication.timeSinceStartup - _sequentialWaitStartTime;
            if (elapsed > SequentialImportTimeoutSeconds)
            {
                Debug.LogWarning("[GoogleDrive] インポート完了イベント待ちがタイムアウトしました。ユーザー操作待ちに切り替えます。");
                // NothingImportとみなし、ユーザーの「次へ」を待つ
                _awaitingSequentialImport = false;
                _sequentialWaitStartTime = 0.0;
                _waitingForUserNext = true;
                Repaint();
            }
            return;
        }

        try
        {
            if (_sequentialInputQueue.Count == 0)
            {
                _isSequentialRunning = false;
                EditorApplication.update -= ProcessSequentialInteractiveImport;
                EditorUtility.ClearProgressBar();
                _lastMessage = $"一括インポートが完了しました。成功: {_sequentialImported}個";
                Debug.Log("[GoogleDrive] " + _lastMessage);
                Repaint();
                return;
            }

            // 手動モードで「次へ」待ちの間は進まない
            if (_sequentialManualAdvance && _waitingForUserNext)
            {
                return;
            }

            var input = _sequentialInputQueue.Dequeue();

            // ダウンロード
            string url;
            string fallbackName;
            EditorUtility.DisplayProgressBar("Google Drive", $"ダウンロード中 ({_sequentialImported + 1}/{_sequentialTotal}) {input}", (float)_sequentialImported / Math.Max(1, _sequentialTotal));
            BuildDownloadUrlAndFallback(input, out url, out fallbackName);
            var fullPath = DownloadPackageToFile(url, fallbackName, 600);

            // 対話型インポート開始
            EditorUtility.ClearProgressBar();
            _currentSequentialFile = fullPath;
            _awaitingSequentialImport = true;
            _sequentialWaitStartTime = EditorApplication.timeSinceStartup;
            AssetDatabase.ImportPackage(fullPath, true);
        }
        catch (OperationCanceledException)
        {
            EditorUtility.ClearProgressBar();
            _awaitingSequentialImport = false;
            _waitingForUserNext = false;
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[GoogleDrive] 順次DL/インポート中のエラー: " + e.Message);
            _awaitingSequentialImport = false;
            _waitingForUserNext = false;
        }
    }

    private void OnPackageCompleted(string packageName)
    {
        HandlePackageFinish(true);
    }

    private void OnPackageCancelled(string packageName)
    {
        HandlePackageFinish(false);
    }

    private void OnPackageFailed(string packageName, string errorMessage)
    {
        Debug.LogError("[GoogleDrive] Import Failed: " + errorMessage);
        HandlePackageFinish(false);
    }

    private void HandlePackageFinish(bool success)
    {
        if (!_awaitingSequentialImport) return;

        try
        {
            if (success)
            {
                _sequentialImported++;
                Debug.Log($"[GoogleDrive] インポート完了: {Path.GetFileName(_currentSequentialFile)} ({_sequentialImported}/{_sequentialTotal})");
            }
            if (_deleteAfterImport && !string.IsNullOrEmpty(_currentSequentialFile))
            {
                try 
                { 
                    File.Delete(_currentSequentialFile); 
                    Debug.Log($"[GoogleDrive] ファイル削除完了: {Path.GetFileName(_currentSequentialFile)}");
                } 
                catch (Exception e) 
                { 
                    Debug.LogWarning($"[GoogleDrive] ファイル削除失敗: {Path.GetFileName(_currentSequentialFile)} - {e.Message}");
                }
            }
        }
        finally
        {
            _currentSequentialFile = null;
            _awaitingSequentialImport = false;
            _sequentialWaitStartTime = 0.0;
            if (_sequentialManualAdvance)
            {
                // ユーザーの「次へ」操作待ち
                _waitingForUserNext = true;
                Repaint();
            }
        }
    }

    
    private string GetActualSavePath()
    {
        if (string.IsNullOrWhiteSpace(_targetFolder))
        {
            _targetFolder = "DownloadedUnityPackages";
            _isRelativePath = true;
        }
        
        if (_isRelativePath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, _targetFolder).Replace('\\', '/');
        }
        else
        {
            return _targetFolder;
        }
    }

    private void EnsureTargetFolder()
    {
        var actualPath = GetActualSavePath();
        if (!Directory.Exists(actualPath))
        {
            Directory.CreateDirectory(actualPath);
        }
    }

    private void BuildDownloadUrlAndFallback(string input, out string url, out string fallbackFileName)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new Exception("空のURL/IDです。");

        input = input.Trim();
        
        // 直URL（.unitypackageで終わるもの）はそのまま使用
        if (input.StartsWith("http://") || input.StartsWith("https://"))
        {
            if (input.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
            {
                url = input;
                var n = TryGetNameFromUrl(url);
                fallbackFileName = string.IsNullOrEmpty(n) ? "package.unitypackage" : n;
                return;
            }
            
            // Google DriveのURLからファイルIDを抽出
            var fileId = ExtractFileId(input);
            if (!string.IsNullOrEmpty(fileId))
            {
                url = BuildGoogleDriveDirectUrl(fileId);
                fallbackFileName = fileId + ".unitypackage";
                return;
            }
            
            // その他のURLはそのまま試行
            url = input;
            var name = TryGetNameFromUrl(url);
            fallbackFileName = string.IsNullOrEmpty(name) ? "package.unitypackage" : name;
            if (!fallbackFileName.EndsWith(".unitypackage", StringComparison.OrdinalIgnoreCase))
                fallbackFileName += ".unitypackage";
            return;
        }

        // ファイルIDのみの場合
        var id = ExtractFileId(input);
        if (!string.IsNullOrEmpty(id))
        {
            url = BuildGoogleDriveDirectUrl(id);
            fallbackFileName = id + ".unitypackage";
            return;
        }

        throw new Exception("有効な共有リンクまたはファイルIDではありません。");
    }

    private string BuildGoogleDriveDirectUrl(string fileId)
    {
        // 複数の方法を試行するため、最も確実とされるURLを使用
        return $"https://drive.google.com/uc?export=download&id={fileId}&confirm=t";
    }

    private byte[] DownloadWithRetry(string fileId, int timeoutSec)
    {
        var urls = new[]
        {
            $"https://drive.google.com/uc?export=download&id={fileId}&confirm=t",
            $"https://drive.usercontent.google.com/download?id={fileId}&export=download&confirm=t",
            $"https://drive.google.com/u/0/uc?id={fileId}&export=download&confirm=t"
        };

        Exception lastException = null;

        foreach (var url in urls)
        {
            try
            {
                EditorUtility.DisplayProgressBar("Google Drive", $"ダウンロード試行中... ({url})", 0.3f);
                var bytes = DownloadBytes(url, timeoutSec);
                
                if (bytes != null && bytes.Length > 0 && !IsHtmlContent(bytes))
                {
                    return bytes;
                }
            }
            catch (Exception e)
            {
                lastException = e;
                Debug.LogWarning($"[GoogleDrive] URL失敗: {url} - {e.Message}");
            }
        }

        throw lastException ?? new Exception("すべてのダウンロード方法が失敗しました。");
    }

    private string DownloadPackageToFile(string url, string fallbackFileName, int timeoutSec)
    {
        byte[] bytes;
        
        // Google DriveのファイルIDを含むURLの場合は、リトライ機能を使用
        var fileId = ExtractFileId(url);
        if (!string.IsNullOrEmpty(fileId))
        {
            bytes = DownloadWithRetry(fileId, timeoutSec);
        }
        else
        {
            bytes = DownloadBytes(url, timeoutSec);
        }
        
        if (bytes == null || bytes.Length == 0)
            throw new Exception("ダウンロードに失敗しました。");

        // HTMLページやエラーページが返された場合の検証
        if (IsHtmlContent(bytes))
        {
            throw new Exception("HTMLページがダウンロードされました。ファイルが公開されていないか、共有設定を確認してください。");
        }

        // .unitypackageファイルの簡易検証
        if (!IsValidUnityPackage(bytes))
        {
            throw new Exception("ダウンロードしたファイルは有効な.unitypackageファイルではありません。");
        }

        var fileName = SanitizeFileName(string.IsNullOrEmpty(fallbackFileName) ? "package.unitypackage" : fallbackFileName);
        var actualSavePath = GetActualSavePath();
        var fullPath = Path.Combine(actualSavePath, fileName).Replace('\\', '/');

        if (File.Exists(fullPath) && !_overwrite)
        {
            if (!EditorUtility.DisplayDialog("確認", $"同名ファイルが存在します。上書きしますか？\n{fullPath}", "上書き", "スキップ"))
                throw new OperationCanceledException("ユーザーがスキップを選択しました。");
        }

        File.WriteAllBytes(fullPath, bytes);
        return fullPath;
    }

    private bool IsHtmlContent(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 5) return false;
        
        try
        {
            // 最初の数百バイトをテキストとして読み取り、HTMLタグを検索
            var text = System.Text.Encoding.UTF8.GetString(bytes, 0, Math.Min(bytes.Length, 512)).ToLower();
            return text.Contains("<html") || text.Contains("<!doctype") || text.Contains("<head") || text.Contains("<body");
        }
        catch
        {
            return false;
        }
    }

    private bool IsValidUnityPackage(byte[] bytes)
    {
        if (bytes == null || bytes.Length < 4) return false;
        
        // .unitypackageファイルはgzipで圧縮されたtarファイル
        // gzipのマジックナンバー（0x1F, 0x8B）をチェック
        return bytes[0] == 0x1F && bytes[1] == 0x8B;
    }

    private static string TryGetNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var name = Path.GetFileName(uri.AbsolutePath);
            if (string.IsNullOrEmpty(name) || name == "download") return null;
            name = Uri.UnescapeDataString(name);
            return SanitizeFileName(name);
        }
        catch { return null; }
    }

    //================ HTTP Helper ==================
    private byte[] DownloadBytes(string url, int timeoutSec)
    {
        var req = UnityWebRequest.Get(url);
        req.timeout = timeoutSec;
        req.downloadHandler = new DownloadHandlerBuffer();
        var op = req.SendWebRequest();
        float p = 0f;
        while (!op.isDone)
        {
            p = Mathf.Lerp(p, op.progress, 0.35f);
            EditorUtility.DisplayProgressBar("Google Drive", "ダウンロード中...", Mathf.Clamp01(0.2f + p * 0.7f));
            System.Threading.Thread.Sleep(50);
        }
        EditorUtility.ClearProgressBar();

        if (req.result != UnityWebRequest.Result.Success)
        {
            var err = req.error;
            req.Dispose();
            throw new Exception("HTTP Error: " + err);
        }

        var data = req.downloadHandler.data;
        req.Dispose();
        return data;
    }

    //================ Utils ==================
    private static string ExtractFileId(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim();

        var m1 = Regex.Match(input, @"drive\.google\.com\/file\/d\/([a-zA-Z0-9\-_]+)");
        if (m1.Success) return m1.Groups[1].Value;

        var m2 = Regex.Match(input, @"[\?&]id=([a-zA-Z0-9\-_]+)");
        if (m2.Success) return m2.Groups[1].Value;

        if (Regex.IsMatch(input, @"^[a-zA-Z0-9\-_]{10,}$"))
            return input;

        return null;
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
