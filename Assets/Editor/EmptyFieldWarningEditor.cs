using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel.DataAnnotations;

[InitializeOnLoad]
public class RequiredFieldWarningSceneView
{
    private static readonly List<WarningInfo> warnings = new List<WarningInfo>();
    private static double lastUpdateTime;
    private const double UPDATE_INTERVAL = 1.0f;

    private const float WARNING_AREA_MARGIN = 10f;
    private const float WARNING_AREA_WIDTH = 400f;
    private const float WARNING_ITEM_HEIGHT = 20f;
    private const float WARNING_PADDING = 40f;

    private static GUIStyle titleStyle;
    private static GUIStyle buttonStyle;
    private static bool stylesInitialized;

    private readonly struct WarningInfo
    {
        public readonly string Message { get; }
        public readonly GameObject TargetObject { get; }
        public readonly Component TargetComponent { get; }

        public WarningInfo(string message, GameObject targetObject, Component targetComponent)
        {
            Message = message;
            TargetObject = targetObject;
            TargetComponent = targetComponent;
        }
    }

    static RequiredFieldWarningSceneView()
    {
        EditorApplication.delayCall += () =>
        {
            SceneView.duringSceneGui += OnSceneGUI;
            EditorApplication.update += OnEditorUpdate;
        };
    }

    private static void EnsureStylesInitialized()
    {
        if (stylesInitialized) return;

        if (EditorStyles.helpBox == null) return;

        titleStyle = new GUIStyle(EditorStyles.helpBox)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };

        buttonStyle = new GUIStyle(EditorStyles.linkLabel)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleLeft
        };

        stylesInitialized = true;
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.timeSinceStartup - lastUpdateTime < UPDATE_INTERVAL)
            return;

        lastUpdateTime = EditorApplication.timeSinceStartup;
        UpdateWarnings();
    }

    private static void UpdateWarnings()
    {
        warnings.Clear();
        HashSet<GameObject> processedObjects = new HashSet<GameObject>();

        GameObject[] sceneObjects = MonoBehaviour.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        foreach (GameObject obj in sceneObjects)
        {
            if (!processedObjects.Add(obj)) continue;
            CheckGameObjectComponents(obj);
        }
    }

    private static void CheckGameObjectComponents(GameObject obj)
    {
        MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            if (component == null) continue;
            CheckComponentFields(component, obj);
        }
    }

    private static void CheckComponentFields(MonoBehaviour component, GameObject obj)
    {
        System.Type type = component.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        foreach (FieldInfo field in fields)
        {
            if (!IsRequiredSerializedField(field)) continue;

            object value = field.GetValue(component);
            if (IsNullOrEmpty(value))
            {
                AddWarning(obj, component, field);
            }
        }
    }

    private static bool IsRequiredSerializedField(FieldInfo field)
    {
        return field.IsDefined(typeof(SerializeField), false) &&
               field.IsDefined(typeof(RequiredAttribute), false);
    }

    private static bool IsNullOrEmpty(object value)
    {
        return value == null || (value is Object unityObj && unityObj == null);
    }

    private static void AddWarning(GameObject obj, MonoBehaviour component, FieldInfo field)
    {
        string message = $"{obj.name} ({component.GetType().Name}) -> {field.Name}";
        warnings.Add(new WarningInfo(message, obj, component));
    }

    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!stylesInitialized)
        {
            EnsureStylesInitialized();
            if (!stylesInitialized) return;
        }

        if (warnings.Count == 0) return;

        Handles.BeginGUI();
        DrawWarningArea(sceneView);
        Handles.EndGUI();
    }

    private static void DrawWarningArea(SceneView sceneView)
    {
        float areaHeight = warnings.Count * WARNING_ITEM_HEIGHT + WARNING_PADDING;
        Rect areaRect = new Rect(
            WARNING_AREA_MARGIN,
            sceneView.position.height - areaHeight - WARNING_AREA_MARGIN,
            WARNING_AREA_WIDTH,
            areaHeight
        );

        GUILayout.BeginArea(areaRect);
        DrawWarnings();
        GUILayout.EndArea();
    }

    private static void DrawWarnings()
    {
        GUILayout.Label("Required Fields Not Assigned:", titleStyle);

        foreach (WarningInfo warning in warnings)
        {
            if (GUILayout.Button(warning.Message, buttonStyle))
            {
                Selection.activeObject = warning.TargetObject;
                EditorGUIUtility.PingObject(warning.TargetObject);

                if (warning.TargetComponent != null)
                {
                    EditorGUIUtility.ShowObjectPicker<Component>(
                        warning.TargetComponent, false, "", EditorGUIUtility.GetControlID(FocusType.Passive)
                    );
                }
            }
        }
    }
}
