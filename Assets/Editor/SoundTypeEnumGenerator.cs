using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;

public class SoundTypeEnumGenerator : EditorWindow
{
    private SoundTypeDefinition _soundTypeDefinition;
    private string _audioFolderPath = "Assets/Audio";
    private bool _autoUpdate = true;

    [MenuItem("Tools/Sound Type Enum Generator")]
    public static void ShowWindow()
    {
        GetWindow<SoundTypeEnumGenerator>("サウンドタイプ生成");
    }

    private void OnEnable()
    {
        // 既存のSoundTypeDefinitionを検索
        string[] guids = AssetDatabase.FindAssets("t:SoundTypeDefinition");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _soundTypeDefinition = AssetDatabase.LoadAssetAtPath<SoundTypeDefinition>(path);
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("サウンドタイプ動的生成ツール", EditorStyles.boldLabel);

        EditorGUILayout.Space();

        // SoundTypeDefinitionの設定
        _soundTypeDefinition = (SoundTypeDefinition)EditorGUILayout.ObjectField(
            "Sound Type Definition", _soundTypeDefinition, typeof(SoundTypeDefinition), false);

        if (_soundTypeDefinition == null)
        {
            EditorGUILayout.HelpBox("SoundTypeDefinitionを設定してください", MessageType.Warning);
            if (GUILayout.Button("新規作成"))
            {
                CreateSoundTypeDefinition();
            }
            return;
        }

        EditorGUILayout.Space();

        // オーディオフォルダパス
        _audioFolderPath = EditorGUILayout.TextField("オーディオフォルダパス", _audioFolderPath);

        // 自動更新設定
        _autoUpdate = EditorGUILayout.Toggle("自動更新", _autoUpdate);

        EditorGUILayout.Space();

        // 手動更新ボタン
        if (GUILayout.Button("手動でサウンドタイプを更新"))
        {
            UpdateSoundTypes();
        }

        EditorGUILayout.Space();

        // 現在のサウンドタイプ表示
        if (_soundTypeDefinition.SoundTypeNames.Length > 0)
        {
            GUILayout.Label("現在のサウンドタイプ:", EditorStyles.boldLabel);
            foreach (string typeName in _soundTypeDefinition.SoundTypeNames)
            {
                EditorGUILayout.LabelField($"• {typeName}");
            }
        }

        EditorGUILayout.Space();

        // 注意事項
        EditorGUILayout.HelpBox(
            "注意: このツールはAudioClipファイル名を基にenumを生成します。\n" +
            "ファイル名は有効なC#識別子である必要があります。",
            MessageType.Info);
    }

    private void CreateSoundTypeDefinition()
    {
        _soundTypeDefinition = CreateInstance<SoundTypeDefinition>();
        string path = "Assets/SoundTypeDefinition.asset";
        AssetDatabase.CreateAsset(_soundTypeDefinition, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = _soundTypeDefinition;
    }

    private void UpdateSoundTypes()
    {
        if (_soundTypeDefinition == null) return;

        // オーディオフォルダからAudioClipを検索
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { _audioFolderPath });
        var soundTypes = new System.Collections.Generic.List<string>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            // 有効なC#識別子かチェック
            if (IsValidIdentifier(fileName))
            {
                soundTypes.Add(fileName);
            }
            else
            {
                Debug.LogWarning($"無効なファイル名: {fileName} (パス: {path})");
            }
        }

        // 重複を除去してソート
        soundTypes = soundTypes.Distinct().OrderBy(x => x).ToList();

        // SoundTypeDefinitionを更新
        _soundTypeDefinition.UpdateSoundTypes(soundTypes.ToArray());
        EditorUtility.SetDirty(_soundTypeDefinition);

        // enumファイルを生成
        GenerateEnumFile(soundTypes);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"サウンドタイプを更新しました: {soundTypes.Count}個");
    }

    private void GenerateEnumFile(System.Collections.Generic.List<string> soundTypes)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("// このファイルは自動生成されます。手動編集は避けてください。");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("public enum SoundType");
        sb.AppendLine("{");

        for (int i = 0; i < soundTypes.Count; i++)
        {
            string typeName = soundTypes[i];
            if (i == soundTypes.Count - 1)
                sb.AppendLine($"    {typeName}");
            else
                sb.AppendLine($"    {typeName},");
        }

        sb.AppendLine("}");

        string enumPath = "Assets/Scripts/Sound/SoundType.cs";
        File.WriteAllText(enumPath, sb.ToString());

        Debug.Log($"enumファイルを生成しました: {enumPath}");
    }

    private bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrEmpty(identifier)) return false;

        // 最初の文字は文字またはアンダースコアである必要がある
        if (!char.IsLetter(identifier[0]) && identifier[0] != '_')
            return false;

        // 残りの文字は文字、数字、アンダースコアである必要がある
        for (int i = 1; i < identifier.Length; i++)
        {
            if (!char.IsLetterOrDigit(identifier[i]) && identifier[i] != '_')
                return false;
        }

        // C#予約語は避ける
        string[] reservedWords = {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
            "void", "volatile", "while"
        };

        return !reservedWords.Contains(identifier.ToLower());
    }

    // アセットの変更を監視して自動更新
    [UnityEditor.Callbacks.OnOpenAsset(0)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var instance = GetWindow<SoundTypeEnumGenerator>();
        if (instance != null && instance._autoUpdate)
        {
            // AudioClipが追加/削除された場合に自動更新
            string path = AssetDatabase.GetAssetPath(instanceID);
            if (path.EndsWith(".wav") || path.EndsWith(".mp3") || path.EndsWith(".ogg"))
            {
                instance.UpdateSoundTypes();
                return true;
            }
        }
        return false;
    }
}
