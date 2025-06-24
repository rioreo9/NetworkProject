using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// サブクラスを生成するための拡張機能
/// </summary>
public class SubClassGenerator : EditorWindow
{
    private string _templateModeName = "Template";
    private string _subClassModeName = "SubClass";
    private int _selectModeIndex;
    private string[] _modeOptions;
    private string[] _baseClassOptions;//基底リスト
    private string[] _interfaceOptions;//インターフェースリスト
    private int _selectedClassIndex;//選択された基底の番号
    private int _selectedMask;//選択されたインターフェースの番号
    private string _newScriptName = "NewClass";//生成するスクリプトの名前(defaultはNewClass)

    private HashSet<string> _usings = new();//usingのリスト

    private bool _isExplicit = false; // 明示的実装のフラグ


    private string _createPath = string.Empty;//生成する位置を設定する場合

    private string[] suggestions = new string[0];
    private bool showSuggestions = false;
    private Vector2 scrollPos;
    #region temp
    private string[] _templateFiles; // テンプレートファイルのリスト
    private int _selectedTemplateIndex = 0; // 選択中のテンプレートインデックス
    #endregion
    [MenuItem("Tools/Sub Class Generator")]
    public static void ShowWindow()
    {
        GetWindow<SubClassGenerator>("Sub Class Generator");
    }

    private void OnEnable()
    {
        RefreshTypeLists();
    }

    /// <summary>
    /// 基底とインターフェースのリストのリフレッシュ
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RefreshTypeLists()
    {
        SetModeOptions();
        LoadAbstractClasses();
        LoadInterfaces();
        LoadTemplateFiles();
        
    }
    /// <summary>
    /// モードの定義
    /// </summary>
    private void SetModeOptions()
    {
        _modeOptions = new string[] { _subClassModeName, _templateModeName };
    }

    /// <summary>
    /// 基底クラスの読み込み
    /// </summary>
    private void LoadAbstractClasses()
    {
        Assembly assembly = GetAssemblyCS();

        if (assembly == null)
        {
            Debug.LogWarning("Assembly-CSharpが見つかりませんでした。");
            _baseClassOptions = new[] { "None" };
            return;
        }

        _baseClassOptions = assembly.GetTypes()
            .Where(type => type.IsClass && type.IsAbstract && type.IsVisible && !type.IsSealed)
            .Select(type => type.FullName)
            .Prepend("None")
            .ToArray();
    }

    /// <summary>
    /// インターフェースの読み込み
    /// </summary>
    private void LoadInterfaces()
    {
        Assembly assembly = GetAssemblyCS();

        if (assembly == null)
        {
            Debug.LogWarning("Assembly-CSharpが見つかりませんでした。");
            _interfaceOptions = Array.Empty<string>();
            return;
        }

        _interfaceOptions = assembly.GetTypes()
            .Where(type => type.IsInterface)
            .Select(type => type.FullName)
            .ToArray();

        _selectedMask = 0;
    }

    private void OnGUI()
    {
        _newScriptName = EditorGUILayout.TextField("Script Name", _newScriptName);
        //パス指定のフィールド
        //_createPath = EditorGUILayout.TextField("Creat Path", _createPath);
        PathCreaterGUI();
        _selectModeIndex = EditorGUILayout.Popup("Select Mode", _selectModeIndex, _modeOptions);
        switch (_selectModeIndex)
        {
            case 0:
                {
                    SubClassGenerateGUI();
                    if (GUILayout.Button("Create Script"))
                    {
                        CreateSubScript(_baseClassOptions[_selectedClassIndex], _interfaceOptions, _newScriptName);
                    }
                    break;
                }
            case 1:
                {
                    TemplateGUI();
                    
                    break;
                }


        }

        
        
        if (GUILayout.Button("Reload Classes"))
        {
            RefreshTypeLists();
        }
    }
    #region Sub
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SubClassGenerateGUI()
    {
        if (_baseClassOptions.Length > 0)
        {
            _selectedClassIndex = EditorGUILayout.Popup("Select Base Class", _selectedClassIndex, _baseClassOptions);
        }
        else
        {
            EditorGUILayout.LabelField("プロジェクト内に基底クラスが見つかりませんでした。");
        }

        if (_interfaceOptions.Length > 0)
        {
            _selectedMask = EditorGUILayout.MaskField("Select Interface", _selectedMask, _interfaceOptions);

            //インターフェースの明示的実装のフラグ
            _isExplicit = EditorGUILayout.Toggle("Explicit", _isExplicit);

        }
        else
        {
            EditorGUILayout.LabelField("プロジェクト内にインターフェースが見つかりませんでした。");
        }
    }

    /// <summary>
    /// csファイルの生成
    /// </summary>
    /// <param name="baseClassName"></param>
    /// <param name="interfaceNames"></param>
    /// <param name="scriptName"></param>
    private void CreateSubScript(string baseClassName, string[] interfaceNames, string scriptName)
    {


        //選択されたインターフェースの取得
        List<string> selectedInterfaces = interfaceNames
           .Where((_, index) => (_selectedMask & (1 << index)) != 0)
           .ToList();

        //継承部分の生成
        string inheritance = CreateInheritanceSentence(baseClassName, selectedInterfaces.ToArray());
        //アセンブリの取得
        Assembly assembly = GetAssemblyCS();

        //プロパティ部分の用意
        string propertyStub = string.Empty;

        //プロパティ部分の生成
        propertyStub += CreateClassPropertyStub(assembly, baseClassName);
        propertyStub += CreateInterfacePropertyStub(assembly, selectedInterfaces.ToArray());

        //メソッド部分の用意
        string methodStubs = string.Empty;

        //メソッド部分の生成
        methodStubs += CreateClassMethodStubs(assembly, baseClassName);
        methodStubs += CreateInterfaceMethodStubs(assembly, selectedInterfaces.ToArray());

        //usingの登録
        RegisterClassUsings(assembly, baseClassName);
        RegisterInterfaceUsings(assembly, selectedInterfaces.ToArray());

        //using部分の用意
        string usingStatements = string.Empty;

        //using部分の生成
        usingStatements = CreateUsingStatement(_usings);

        //スクリプトの内容を生成
        string scriptContent = $"using System;\n" +
                               $"{usingStatements}\n" +
                               $"public class {scriptName}{inheritance}\n" +
                               "{\n" +
                               propertyStub +
                               methodStubs +
                               "}";
        //ファイルを生成する位置の取得
        string scriptPath = CreatePath(scriptName);
        //ファイルを出力
        File.WriteAllText(scriptPath, scriptContent);
        Debug.Log("スクリプトが生成されました: " + scriptPath);
        //リフレッシュ
        _usings.Clear();
        AssetDatabase.Refresh();
    }


    /// <summary>
    /// 継承部分の文章を生成
    /// </summary>
    /// <param name="baseClassName"></param>
    /// <param name="selectedInterfaces"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateInheritanceSentence(string baseClassName, string[] selectedInterfaces)
    {


        List<string> inheritanceList = new List<string>();

        if (baseClassName != "None")
        {
            inheritanceList.Add(baseClassName);
        }
        inheritanceList.AddRange(selectedInterfaces);

        string inheritance = string.Empty;
        if (inheritanceList.Count > 0)
        {
            inheritance = $" : {string.Join(", ", inheritanceList)}";
        }
        return inheritance;
    }

    #region CreateUsingSentence

    /// <summary>
    /// 渡された名前のクラスのUsingを取得・登録
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="baseClassName"></param>
    private void RegisterClassUsings(Assembly assembly, string baseClassName)
    {
        string sentence = string.Empty;
        if (baseClassName == "None")
        {
            return;
        }
        //Typeの生成
        Type baseClassType = assembly?.GetType(baseClassName);

        if (baseClassType == null)
        {
            return;
        }
        RegisterUsingSentence(baseClassType);
        //プロパティの取得
        IEnumerable<PropertyInfo> properties = baseClassType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                                        .Where(property => property.GetGetMethod(true)?.IsAbstract == true || property.GetSetMethod(true)?.IsAbstract == true);

        //プロパティのusingを登録
        foreach (PropertyInfo property in properties)
        {
            RegisterPropertyUsings(property);
        }

        //メソッドの取得
        IEnumerable<MethodInfo> methods = GetAbstractClassMethods(baseClassType);

        foreach (MethodInfo method in methods)
        {
            RegisterMethodUsings(method);
        }
    }
    /// <summary>
    /// 渡されたインターフェースの名前群のusingを登録
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="selectInterfaces"></param>
    private void RegisterInterfaceUsings(Assembly assembly, string[] selectInterfaces)
    {
        foreach (string interfaceName in selectInterfaces)
        {
            Type interfaceType = assembly?.GetType(interfaceName);

            if (interfaceType == null)
            {
                continue;
            }
            foreach (PropertyInfo property in interfaceType.GetProperties())
            {
                RegisterPropertyUsings(property);
            }
            foreach (MethodInfo method in GetInterfaceMethods(interfaceType))
            {
                RegisterMethodUsings(method);
            }
        }

    }


    /// <summary>
    /// propertyのusingを登録
    /// </summary>
    /// <param name="info"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RegisterPropertyUsings(PropertyInfo info)
    {
        RegisterUsingSentence(info.PropertyType);
    }
    /// <summary>
    /// methodのusingを登録
    /// </summary>
    /// <param name="info"></param>
    private void RegisterMethodUsings(MethodInfo info)
    {
        RegisterUsingSentence(info.ReturnType);
        foreach (ParameterInfo parameter in info.GetParameters())
        {
            RegisterUsingSentence(parameter.ParameterType);
        }
    }

    /// <summary>
    /// _usingにtypeのusingを登録
    /// </summary>
    /// <param name="type"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RegisterUsingSentence(Type type)
    {
        if (type.Namespace == null)
        {
            return;
        }
        if (type.Namespace == "System")
        {
            return;
        }
        _usings.Add($"using {type.Namespace};");
    }
    /// <summary>
    /// usingの部分の生成
    /// </summary>
    /// <param name="usings"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateUsingStatement(HashSet<string> usings)
    {
        string sentence = string.Empty;
        sentence = string.Join("\n", _usings);
        return sentence;
    }
    #endregion CreateUsingSentence
    #region getModifier
    /// <summary>
    /// メソッドのアクセス修飾子を取得＋変換
    /// </summary>
    /// <param name="method"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string GetMethodAccessModifier(MethodInfo method)
    {
        if (method.IsPublic)
        {
            return "public";
        }
        if (method.IsPrivate)
        {
            return "private";
        }
        if (method.IsFamily) // protected
        {
            return "protected";
        }
        if (method.IsAssembly) // internal
        {
            return "internal";
        }
        if (method.IsFamilyOrAssembly) // protected internal
        {
            return "protected internal";
        }
        if (method.IsFamilyAndAssembly) // private protected
        {
            return "private protected";
        }

        return "public"; // デフォルトは public（通常、ここには到達しない）
    }

    /// <summary>
    /// プロパティ全体のアクセス修飾子を決定
    /// </summary>
    private string GetPropertyAccessModifier(PropertyInfo prop)
    {
        MethodInfo getter = prop.GetGetMethod(true);
        MethodInfo setter = prop.GetSetMethod(true);
        bool hasGetter = getter != null;
        string getAccess = string.Empty;
        if (hasGetter)
        {
            getAccess = GetMethodAccessModifier(getter);
        }
        bool hasSetter = setter != null;
        string setAccess = string.Empty;
        if (hasSetter)
        {
            setAccess = GetMethodAccessModifier(setter);
        }


        // 最も広いアクセス修飾子を適用
        return GetWidestAccessModifier(getAccess, setAccess);
    }

    /// <summary>
    /// 最も広いアクセス修飾子を決定
    /// </summary>
    static string GetWidestAccessModifier(string access1, string access2)
    {
        string[] order = { "private", "private protected", "protected", "internal", "protected internal", "public" };

        string widest = "private"; // デフォルトは private
        foreach (string level in order)
        {
            if (access1 == level || access2 == level)
            {
                widest = level;
            }
        }
        return widest;
    }
    #endregion getModifier
    #region CreateMethodStub

    /// <summary>
    /// abstractMethodの文章を出力
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="baseClassName"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateClassMethodStubs(Assembly assembly, string baseClassName)
    {
        string sentence = string.Empty;
        if (baseClassName != "None")
        {


            Type baseClassType = assembly?.GetType(baseClassName);

            if (baseClassType == null)
            {
                return sentence;
            }

            IEnumerable<string> abstractMethods = GetAbstractClassMethods(baseClassType).Select(mInfo => CreateClassMethodSentence(mInfo, true));
            sentence += string.Join("\n", abstractMethods);
        }
        return sentence;
    }

    /// <summary>
    /// interfaceのメソッドの文章を出力
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="selectedInterfaces"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateInterfaceMethodStubs(Assembly assembly, string[] selectedInterfaces)
    {
        string sentence = string.Empty;
        foreach (string interfaceName in selectedInterfaces)
        {


            Type interfaceType = assembly?.GetType(interfaceName);

            if (interfaceType == null)
            {
                continue;
            }
            IEnumerable<string> interfaceMethods = GetInterfaceMethods(interfaceType)
                  .Select(mInfo => CreateInterfaceMethodSentence(mInfo, _isExplicit, interfaceName));

            sentence += string.Join("\n", interfaceMethods);
        }
        return sentence;
    }
    #endregion CreateMethodStub
    #region CreatePropertyStub

    /// <summary>
    /// クラスのプロパティを取得
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="baseClassName"></param>
    /// <returns></returns>
    [method: MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateClassPropertyStub(Assembly assembly, string baseClassName)
    {
        string sentence = string.Empty;
        if (baseClassName == "None")
        {
            return string.Empty;
        }
        Type baseClassType = assembly?.GetType(baseClassName);

        if (baseClassType == null)
        {
            return string.Empty;
        }
        // 抽象プロパティの取得・生成
        IEnumerable<string> abstractProperties = GetAbstractClassProperties(baseClassType)
                                                 .Select(property => CreatePropertyStub(property, true));
        sentence += string.Join("\n", abstractProperties);
        return sentence + "\n";
    }

    /// <summary>
    /// interfaceのプロパティを取得
    /// </summary>
    /// <param name="assembly"></param>
    /// <param name="selectedInterfaces"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateInterfacePropertyStub(Assembly assembly, string[] selectedInterfaces)
    {
        string sentence = string.Empty;
        foreach (string interfaceName in selectedInterfaces)
        {


            Type interfaceType = assembly?.GetType(interfaceName);

            if (interfaceType == null)
            {
                continue;
            }
            IEnumerable<string> interfacePropertyStubs = GetInterfaceProperties(interfaceType)
                                                         .Select(property => CreatePropertyStub(property, false));
            sentence += string.Join("\n", interfacePropertyStubs);
        }
        return sentence + "\n";
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreatePropertyStub(PropertyInfo property, bool isOverride)
    {
        string overrideSentence = string.Empty;
        if (isOverride)
        {
            overrideSentence = "override";
        }
        string modifier = string.Empty;
        modifier = GetPropertyAccessModifier(property);
        string getPropertySentence = string.Empty;
        bool hasGetter = property.GetGetMethod(true) != null;
        if (hasGetter)
        {
            getPropertySentence = "get => throw new NotImplementedException(); ";
        }

        string setPropertySentence = string.Empty;
        bool hasSetter = property.GetSetMethod(true) != null;
        if (hasSetter)
        {
            setPropertySentence = "set => throw new NotImplementedException(); ";
        }

        return $"    {modifier} {overrideSentence} {ConvertType(property.PropertyType)} {property.Name} {{ " +
               getPropertySentence +
               setPropertySentence +
               "}";
    }
    #endregion CreatePropertyStub
    #region CreateMethodSentence
    /// <summary>
    /// メソッドの情報を返すよ
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateClassMethodSentence(MethodInfo info, bool isOverride)
    {
        string overrideSetence = string.Empty;
        if (isOverride)
        {
            overrideSetence = "override";
        }
        string modifiere = GetMethodAccessModifier(info);
        return $"    {modifiere} {overrideSetence} {ConvertType(info.ReturnType)} {info.Name}({string.Join(", ", info.GetParameters().Select(p => ConvertType(p.ParameterType) + " " + p.Name))})\n    {{\n        throw new NotImplementedException();\n    }}\n";
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreateInterfaceMethodSentence(MethodInfo info, bool isExplicit, string interfaceName)
    {
        string nameSetence = string.Empty;
        string modifiere = string.Empty;
        if (isExplicit)
        {
            nameSetence = $"{interfaceName}.";
        }
        else
        {
            modifiere = $"{GetMethodAccessModifier(info)} ";
        }
        return $"    {modifiere}{ConvertType(info.ReturnType)} {nameSetence}{info.Name}({string.Join(", ", info.GetParameters().Select(p => ConvertType(p.ParameterType) + " " + p.Name))})\n    {{\n        throw new NotImplementedException();\n    }}\n";

    }
    /// <summary>
    /// 特殊な変換の必要がある場合,変換して返す
    /// </summary>
    /// <param name="convertType"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string ConvertType(Type convertType)
    {
        if (convertType == typeof(void)) return "void";
        if (convertType == typeof(object)) return "object";
        if (convertType == typeof(string)) return "string";
        if (convertType == typeof(UnityEngine.Object)) return "UnityEngine.Object";
        if (convertType == typeof(Int32)) return "int";
        if (convertType == typeof(Boolean)) return "bool";


        if (convertType.IsGenericType) // ジェネリック型の場合
        {
            string baseName = convertType.Name.Split('`')[0]; // `List` のように `List<T>` から `List` 部分を取得            
            string genericArgs = string.Join(", ", convertType.GetGenericArguments().Select(ConvertType));// 再帰的に型変換
            return $"{baseName}<{genericArgs}>";
        }

        return convertType.Name;
    }
    #endregion CreateMethodSentence
    #region GetMethods
    /// <summary>
    /// 基底クラスのメソッドを取得
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<MethodInfo> GetAbstractClassMethods(Type type)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                            .Where(mInfo => mInfo.IsAbstract && !mInfo.IsSpecialName);
    }
    /// <summary>
    /// インターフェースのメソッドを取得
    /// </summary>
    /// <param name="interfaceType"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<MethodInfo> GetInterfaceMethods(Type interfaceType)
    {
        return interfaceType.GetMethods()
                    .Where(mInfo => !mInfo.IsSpecialName);
    }


    #endregion GetMethods
    #region GetProperties
    /// <summary>
    /// 基底クラスのメソッドを取得
    /// </summary>
    /// <param name="baseClassType"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private IEnumerable<PropertyInfo> GetAbstractClassProperties(Type baseClassType)
    {
        return baseClassType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                    .Where(property => property.GetGetMethod(true)?.IsAbstract == true || property.GetSetMethod(true)?.IsAbstract == true);
    }

    private IEnumerable<PropertyInfo> GetInterfaceProperties(Type interfaceType)
    {
        return interfaceType.GetProperties()
                 .Where(property => property.GetGetMethod(true)?.IsAbstract == true || property.GetSetMethod(true)?.IsAbstract == true);


    }
    #endregion GetProperties
    #endregion Sub
    #region Template
    private void LoadTemplateFiles()
    {
        // Templateフォルダのパスを指定
        string templateFolderPath = "Assets/Template";

        // フォルダが存在するか確認
        if (Directory.Exists(templateFolderPath))
        {
            // 指定したフォルダ内の全てのファイルを取得
            string[] files = Directory.GetFiles(templateFolderPath, "*.txt");

            // ファイル名だけに変換して格納
            _templateFiles = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                _templateFiles[i] = Path.GetFileNameWithoutExtension(files[i]);
            }
        }
        else
        {
            //Debug.LogError("Templateフォルダが見つかりません: " + templateFolderPath);
            _templateFiles = new string[0];
        }
    }
    private void TemplateGUI()
    {
        // ドロップダウンメニューでテンプレートを選択
        if (_templateFiles.Length > 0)
        {
            _selectedTemplateIndex = EditorGUILayout.Popup("Select Template", _selectedTemplateIndex, _templateFiles);

            if (GUILayout.Button("Create Script"))
            {
                CreateScriptFromTemplate(_templateFiles[_selectedTemplateIndex], _newScriptName);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Templateフォルダ内にテンプレートファイルがありません。");
        }
    }
    private void CreateScriptFromTemplate(string templateName, string scriptName)
    {
        // Templateファイルのパス
        string templatePath = $"Assets/Template/{templateName}.txt";

        if (File.Exists(templatePath))
        {
            // テンプレートの内容を読み込む
            string templateContent = File.ReadAllText(templatePath);

            // #CLASS_NAME# をスクリプト名で置き換える
            templateContent = templateContent.Replace("#SCRIPTNAME#", scriptName);

            // 現在選択されているフォルダのパスを取得
            string selectedFolderPath = "Assets"; // デフォルトは Assets フォルダに設定
            Object selected = Selection.activeObject;
            if (selected != null)
            {
                selectedFolderPath = AssetDatabase.GetAssetPath(selected);

                // フォルダが選択されていない場合は、ファイルが選択されている可能性があるため、その場合はフォルダパスに修正
                if (!Directory.Exists(selectedFolderPath))
                {
                    selectedFolderPath = Path.GetDirectoryName(selectedFolderPath);
                }
            }

            // 新しいスクリプトの保存先と名前を設定
            string scriptPath = Path.Combine(selectedFolderPath, $"{scriptName}.cs");

            // ファイルを書き込む
            File.WriteAllText(scriptPath, templateContent);

            RefreshTypeLists();

            Debug.Log("スクリプトが生成されました: " + scriptPath);
        }
        else
        {
            Debug.LogError("テンプレートファイルが見つかりません: " + templatePath);
        }
    }
    #endregion Template
    #region AutoCompletePath
    private void PathCreaterGUI()
    {
        EditorGUILayout.LabelField("Enter Path:");
        string newInput = EditorGUILayout.TextField(_createPath);

        if (newInput != _createPath)
        {
            _createPath = newInput;
            UpdateSuggestions();
        }

        if (showSuggestions && suggestions.Length > 0)
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(100));
            foreach (var suggestion in suggestions)
            {
                if (GUILayout.Button(suggestion))
                {
                    _createPath = suggestion;
                    showSuggestions = false;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }
    private void UpdateSuggestions()
    {
        if (string.IsNullOrEmpty(_createPath))
        {
            suggestions = new string[0];
            showSuggestions = false;
            return;
        }

        string normalizedPath = _createPath.Replace("\\", "/"); // Windows対策
        string basePath = "Assets/";

        // 途中のディレクトリも含めた補完
        string targetPath = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrEmpty(targetPath) || !Directory.Exists(targetPath))
        {
            targetPath = basePath;
        }

        // フォルダのみ取得 & 入力にマッチするものだけ表示
        suggestions = Directory.GetDirectories(targetPath)
            .Select(dir => dir.Replace("\\", "/")) // Windows用のパス変換
            .Where(dir => dir.StartsWith(normalizedPath, System.StringComparison.OrdinalIgnoreCase))
            .ToArray();

        showSuggestions = suggestions.Length > 0;
    }
    #endregion AutoCompletePath
    /// <summary>
    /// 生成するパスを取得
    /// </summary>
    /// <param name="scriptName"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private string CreatePath(string scriptName)
    {
        string selectedFolderPath = "Assets";
        if (_createPath != string.Empty)
        {
            string folderPath = _createPath.Trim();
            if (!folderPath.StartsWith("Assets"))
            {
                Debug.LogWarning("スクリプトの出力先は Assets フォルダ内でなければなりません。");
                folderPath = "Assets";
            }
            selectedFolderPath = folderPath;
        }
        else//パス指定がなかった場合は、今選択しているフォルダ・ファイルのパスを
        {
            Object selected = Selection.activeObject;

            if (selected != null)
            {
                selectedFolderPath = AssetDatabase.GetAssetPath(selected);
                if (!Directory.Exists(selectedFolderPath))
                {
                    selectedFolderPath = Path.GetDirectoryName(selectedFolderPath);
                }
            }
        }

        string scriptPath = Path.Combine(selectedFolderPath, $"{scriptName}.cs");
        return scriptPath;

    }

    

    /// <summary>
    /// Assembly-CSharpのAssemblyを返す
    /// </summary>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Assembly GetAssemblyCS()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
    }



}
#endif