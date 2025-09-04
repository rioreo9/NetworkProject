using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

public class GoogleDriveAudioDownloader : EditorWindow
{
    private const string EditorPrefsKey = "GoogleDrive.ApiKey";

    private string _apiKey = string.Empty;
    private string _fileUrlOrId = string.Empty;
    private string _folderUrlOrId = string.Empty;
    private string _targetFolder = "Assets/Audio";
    private bool _overwrite = false;
    private string _lastMessage = string.Empty;

    [MenuItem("Tools/Audio/Download from Google Drive...")]
    public static void ShowWindow()
    {
        var w = GetWindow<GoogleDriveAudioDownloader>("Google Drive Audio");
        w.minSize = new Vector2(520, 320);
    }

    private void OnEnable()
    {
        _apiKey = EditorPrefs.GetString(EditorPrefsKey, _apiKey);
    }

    private void OnDisable()
    {
        EditorPrefs.SetString(EditorPrefsKey, _apiKey ?? string.Empty);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Google Drive から Audio をダウンロード", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // API キー
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("Google Drive API キー", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("公開フォルダの一覧取得には API キーが必要です。Google Cloud Console で Drive API を有効化し、API Key を作成してください。", MessageType.Info);
            _apiKey = EditorGUILayout.TextField(new GUIContent("API Key"), _apiKey);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("保存", GUILayout.Width(96)))
                {
                    EditorPrefs.SetString(EditorPrefsKey, _apiKey ?? string.Empty);
                    EditorUtility.DisplayDialog("保存", "API Key を保存しました。", "OK");
                }
            }
        }

        EditorGUILayout.Space();

        // 保存先
        using (new EditorGUILayout.HorizontalScope())
        {
            _targetFolder = EditorGUILayout.TextField(new GUIContent("保存先フォルダ"), _targetFolder);
            if (GUILayout.Button("選択", GUILayout.Width(64)))
            {
                var abs = EditorUtility.OpenFolderPanel("保存先フォルダを選択", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    var absNorm = abs.Replace('\\', '/');
                    var dataPath = Application.dataPath.Replace('\\', '/');
                    if (absNorm.StartsWith(dataPath))
                    {
                        _targetFolder = "Assets" + absNorm.Substring(dataPath.Length);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("無効なフォルダ", "プロジェクトの Assets 配下を選択してください。", "OK");
                    }
                }
            }
        }

        _overwrite = EditorGUILayout.ToggleLeft("同名ファイルがあれば上書き", _overwrite);

        EditorGUILayout.Space();

        // 単一ファイル
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("単一ファイルのダウンロード", EditorStyles.boldLabel);
            _fileUrlOrId = EditorGUILayout.TextField(new GUIContent("共有リンク / ファイルID"), _fileUrlOrId);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_fileUrlOrId)))
                {
                    if (GUILayout.Button("ファイルをダウンロード", GUILayout.Height(26), GUILayout.Width(200)))
                    {
                        DownloadSingleFile();
                    }
                }
            }
        }

        // フォルダ一括
        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("フォルダ内を一括ダウンロード", EditorStyles.boldLabel);
            _folderUrlOrId = EditorGUILayout.TextField(new GUIContent("フォルダ共有リンク / フォルダID"), _folderUrlOrId);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_folderUrlOrId)))
                {
                    if (GUILayout.Button("フォルダを一括ダウンロード", GUILayout.Height(28), GUILayout.Width(220)))
                    {
                        DownloadFolderAll();
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(_lastMessage))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_lastMessage, MessageType.Info);
        }
    }

    //================ 単一ファイル ==================
    private void DownloadSingleFile()
    {
        try
        {
            string fileId = ExtractFileId(_fileUrlOrId);
            if (string.IsNullOrEmpty(fileId))
            {
                EditorUtility.DisplayDialog("入力エラー", "有効な共有リンクまたはファイルIDを入力してください。", "OK");
                return;
            }

            EnsureTargetFolder();

            string fileName = null;
            byte[] bytes;

            if (!string.IsNullOrEmpty(_apiKey))
            {
                // Drive API (推奨)
                var meta = GetDriveItem(fileId);
                fileName = meta != null ? meta.name : null;
                var url = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media&key={_apiKey}";
                bytes = DownloadBytes(url, 60);
            }
            else
            {
                // 共有リンクダイレクト (制限あり)
                var url = $"https://drive.google.com/uc?export=download&id={fileId}";
                bytes = DownloadBytes(url, 60);
            }

            if (bytes == null || bytes.Length == 0)
                throw new Exception("ダウンロードに失敗しました。");

            if (string.IsNullOrEmpty(fileName))
                fileName = fileId + ".bin";

            fileName = SanitizeFileName(fileName);

            string saveDir = _targetFolder.TrimEnd('/', '\\');
            string assetPath = saveDir + "/" + fileName;
            string fullPath = Path.Combine(Application.dataPath, assetPath.Replace('\\', '/').Substring("Assets/".Length));

            if (File.Exists(fullPath) && !_overwrite)
            {
                if (!EditorUtility.DisplayDialog("確認", $"同名ファイルが存在します。上書きしますか？\n{assetPath}", "上書き", "キャンセル"))
                    return;
            }

            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(assetPath.Replace('\\', '/'));
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);

            _lastMessage = $"保存しました: {assetPath}";
            Debug.Log("[GoogleDrive] " + _lastMessage);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[GoogleDrive] " + e);
            EditorUtility.DisplayDialog("エラー", e.Message, "OK");
            _lastMessage = e.Message;
        }
    }

    //================ フォルダ一括 ==================
    private void DownloadFolderAll()
    {
        try
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                EditorUtility.DisplayDialog("API Key 必須", "フォルダ一覧の取得には API Key が必要です。", "OK");
                return;
            }

            string folderId = ExtractFolderId(_folderUrlOrId);
            if (string.IsNullOrEmpty(folderId))
            {
                EditorUtility.DisplayDialog("入力エラー", "有効なフォルダ共有リンクまたはフォルダIDを入力してください。", "OK");
                return;
            }

            EnsureTargetFolder();

            var folderMeta = GetDriveItem(folderId);
            string rootName = folderMeta != null ? folderMeta.name : folderId;
            rootName = SanitizeFileName(rootName);

            string rootAssetPath = (_targetFolder.TrimEnd('/', '\\')) + "/" + rootName;
            CreateFolderIfNotExists(rootAssetPath);

            EditorUtility.DisplayProgressBar("Google Drive", "フォルダを列挙中...", 0.05f);
            DownloadFolderRecursive(folderId, rootAssetPath);
            EditorUtility.ClearProgressBar();

            AssetDatabase.Refresh();
            _lastMessage = $"フォルダをダウンロードしました: {rootAssetPath}";
            Debug.Log("[GoogleDrive] " + _lastMessage);
        }
        catch (Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[GoogleDrive] " + e);
            EditorUtility.DisplayDialog("エラー", e.Message, "OK");
            _lastMessage = e.Message;
        }
    }

    private void DownloadFolderRecursive(string folderId, string currentAssetPath)
    {
        // 1) ファイル（非フォルダ）を取得して保存
        foreach (var file in ListChildren(folderId, includeFolders: false))
        {
            string url = $"https://www.googleapis.com/drive/v3/files/{file.id}?alt=media&key={_apiKey}";
            string fileName = SanitizeFileName(file.name);
            string assetPath = currentAssetPath.TrimEnd('/', '\\') + "/" + fileName;
            string fullPath = Path.Combine(Application.dataPath, assetPath.Replace('\\', '/').Substring("Assets/".Length));

            if (File.Exists(fullPath) && !_overwrite)
            {
                // スキップ or 上書き選択
                if (!EditorUtility.DisplayDialog("確認", $"同名ファイルが存在します。上書きしますか？\n{assetPath}", "上書き", "スキップ"))
                    continue;
            }

            EditorUtility.DisplayProgressBar("Google Drive", fileName, 0.4f);
            var bytes = DownloadBytes(url, 120);
            File.WriteAllBytes(fullPath, bytes);
            AssetDatabase.ImportAsset(assetPath.Replace('\\', '/'));
        }

        // 2) サブフォルダを列挙し再帰
        foreach (var sub in ListChildren(folderId, includeFolders: true))
        {
            if (sub.mimeType != "application/vnd.google-apps.folder")
                continue;

            string subDirAssetPath = currentAssetPath.TrimEnd('/', '\\') + "/" + SanitizeFileName(sub.name);
            CreateFolderIfNotExists(subDirAssetPath);

            DownloadFolderRecursive(sub.id, subDirAssetPath);
        }
    }

    private void EnsureTargetFolder()
    {
        if (!AssetDatabase.IsValidFolder(_targetFolder))
        {
            var rel = _targetFolder.Replace('\\', '/');
            if (!rel.StartsWith("Assets/"))
            {
                throw new Exception("保存先は Assets 配下で指定してください。");
            }
            var absDir = Path.Combine(Application.dataPath, rel.Substring("Assets/".Length));
            Directory.CreateDirectory(absDir);
            AssetDatabase.Refresh();
        }
    }

    private void CreateFolderIfNotExists(string assetPath)
    {
        assetPath = assetPath.Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        var parts = assetPath.Split('/');
        if (parts.Length < 2 || parts[0] != "Assets")
            throw new Exception("無効なアセットパス: " + assetPath);

        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    //================ Google Drive API ==================
    [Serializable]
    private class DriveFile
    {
        public string id;
        public string name;
        public string mimeType;
    }

    [Serializable]
    private class DriveFileList
    {
        public DriveFile[] files;
        public string nextPageToken;
    }

    private DriveFile GetDriveItem(string id)
    {
        string url = $"https://www.googleapis.com/drive/v3/files/{id}?fields=id,name,mimeType&key={_apiKey}&supportsAllDrives=true";
        string json = DownloadText(url, 30);
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonUtility.FromJson<DriveFile>(json);
        }
        catch
        {
            return null;
        }
    }

    private IEnumerable<DriveFile> ListChildren(string folderId, bool includeFolders)
    {
        string typeFilter = includeFolders ? "mimeType='application/vnd.google-apps.folder'" : "mimeType!='application/vnd.google-apps.folder'";
        string q = $"'{folderId}' in parents and trashed=false and {typeFilter}";
        string baseUrl = "https://www.googleapis.com/drive/v3/files";

        string pageToken = null;
        do
        {
            string url = baseUrl + "?pageSize=1000" +
                "&fields=files(id,name,mimeType),nextPageToken" +
                "&includeItemsFromAllDrives=true&supportsAllDrives=true" +
                "&q=" + UnityWebRequest.EscapeURL(q) +
                (string.IsNullOrEmpty(pageToken) ? string.Empty : ("&pageToken=" + pageToken)) +
                "&key=" + _apiKey;

            string json = DownloadText(url, 60);
            if (string.IsNullOrEmpty(json)) yield break;

            DriveFileList list = null;
            try
            {
                list = JsonUtility.FromJson<DriveFileList>(json);
            }
            catch
            {
                list = null;
            }

            if (list?.files != null)
            {
                foreach (var f in list.files)
                    yield return f;
            }

            pageToken = list != null ? list.nextPageToken : null;
        }
        while (!string.IsNullOrEmpty(pageToken));
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

    private string DownloadText(string url, int timeoutSec)
    {
        var req = UnityWebRequest.Get(url);
        req.timeout = timeoutSec;
        req.downloadHandler = new DownloadHandlerBuffer();
        var op = req.SendWebRequest();
        float p = 0f;
        while (!op.isDone)
        {
            p = Mathf.Lerp(p, op.progress, 0.35f);
            EditorUtility.DisplayProgressBar("Google Drive", "通信中...", Mathf.Clamp01(0.05f + p * 0.3f));
            System.Threading.Thread.Sleep(30);
        }
        EditorUtility.ClearProgressBar();

        if (req.result != UnityWebRequest.Result.Success)
        {
            var err = req.error;
            req.Dispose();
            throw new Exception("HTTP Error: " + err);
        }

        var text = req.downloadHandler.text;
        req.Dispose();
        return text;
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

    private static string ExtractFolderId(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim();

        var m1 = Regex.Match(input, @"drive\.google\.com\/drive\/folders\/([a-zA-Z0-9\-_]+)");
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


