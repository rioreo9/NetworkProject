#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.Collections;
using System.Reflection;

[CustomPropertyDrawer(typeof(InterfaceReferenceBase), true)]
public class InterfaceReferenceDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var referenceProp = property.FindPropertyRelative("_reference");
        var ifaceType = GetInterfaceType(property);

        EditorGUI.BeginProperty(position, label, property);

        var current = referenceProp.objectReferenceValue;
        var picked = EditorGUI.ObjectField(position, label, current, typeof(UnityEngine.Object), true);

        if (picked != current)
        {
            referenceProp.objectReferenceValue = FilterAssignable(picked, ifaceType);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }

    private static UnityEngine.Object FilterAssignable(UnityEngine.Object obj, Type required)
    {
        if (obj == null || required == null) return null;

        if (required.IsInstanceOfType(obj))
            return obj;

        if (obj is GameObject go)
        {
            var comp = go.GetComponent(required);
            if (comp != null) return comp;
            return null;
        }

        if (obj is Component c)
        {
            if (required.IsInstanceOfType(c)) return c;
            return null;
        }

        if (obj is ScriptableObject so)
        {
            if (required.IsInstanceOfType(so)) return so;
            return null;
        }

        return null;
    }

    private static Type GetInterfaceType(SerializedProperty property)
    {
        // まずは実オブジェクトから安全に取得
        var boxed = GetTargetObjectOfProperty(property) as InterfaceReferenceBase;
        if (boxed != null) 
        {
            // リフレクションを使用してInterfaceTypeプロパティにアクセス
            var interfaceTypeProperty = boxed.GetType().GetProperty("InterfaceType", 
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (interfaceTypeProperty != null)
            {
                return interfaceTypeProperty.GetValue(boxed) as Type;
            }
        }

        // フォールバック（配列/リストでの要素にも対応）
        var fieldInfo = property.serializedObject.targetObject.GetType()
            .GetField(property.propertyPath, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (fieldInfo != null)
        {
            var t = fieldInfo.FieldType;
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(InterfaceReference<>))
                return t.GetGenericArguments()[0];
        }

        return typeof(UnityEngine.Object);
    }

    // SerializedProperty から実際の対象オブジェクトを辿る
    private static object GetTargetObjectOfProperty(SerializedProperty prop)
    {
        if (prop == null) return null;

        var path = prop.propertyPath.Replace(".Array.data[", "[");
        object obj = prop.serializedObject.targetObject;
        var elements = path.Split('.');

        foreach (var element in elements)
        {
            if (element.Contains("["))
            {
                var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                var indexStr = element.Substring(element.IndexOf("[", StringComparison.Ordinal))
                                      .Trim('[', ']');
                if (!int.TryParse(indexStr, out var index)) return null;
                obj = GetValue_Imp(obj, elementName, index);
            }
            else
            {
                obj = GetValue_Imp(obj, element);
            }
        }
        return obj;
    }

    private static object GetValue_Imp(object source, string name)
    {
        if (source == null) return null;
        var type = source.GetType();

        var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (f != null) return f.GetValue(source);

        var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (p != null) return p.GetValue(source, null);

        return null;
    }

    private static object GetValue_Imp(object source, string name, int index)
    {
        var enumerable = GetValue_Imp(source, name) as IEnumerable;
        if (enumerable == null) return null;

        var enumerator = enumerable.GetEnumerator();
        for (int i = 0; i <= index; i++)
        {
            if (!enumerator.MoveNext()) return null;
        }
        return enumerator.Current;
    }
}
#endif
