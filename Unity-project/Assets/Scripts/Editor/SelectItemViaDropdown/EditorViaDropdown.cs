using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Editor.SelectItemViaDropdown
{
  [CustomEditor(typeof(DropdownMonobehaviour))]
  public class UiTestElementEditor : UnityEditor.Editor
  {
    private const string TypesPath = @"Assets\Scripts\DemoTypes";

    private SerializedObject SObject { get { return new SerializedObject(target); } }

    private string _methodName
    {
      get { return SObject.FindProperty("_methodName").stringValue; }
      set
      {
        var so = SObject;
        so.FindProperty("_methodName").stringValue = value;
        so.ApplyModifiedProperties();
      }
    }

    private string _typeName
    {
      get { return SObject.FindProperty("_typeName").stringValue; }
      set
      {
        var so = SObject;
        so.FindProperty("_typeName").stringValue = value;
        so.ApplyModifiedProperties();
      }
    }

    private string BindingPropertyString { get; set; }

    private void Awake()
    {
      if (string.IsNullOrEmpty(BindingPropertyString) && _typeName != null && _methodName != null)
      {
        BindingPropertyString = string.Concat(_typeName, ".", _methodName);
      }
    }

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      var types = GetTestsTypes(TypesPath);
      if(types == null)
        return;

      var props = GetBindableMembers(types);
      if(props == null)
        return;
      
      Type viewPropertyType;
      ShowBindingMenu(
          props,
          OnPropertyChange,
          BindingPropertyString,
          out viewPropertyType
      );

      if (string.IsNullOrEmpty(BindingPropertyString))
      {
        GUI.enabled = false;
      }
      
      EditorGUILayout.Space();
      EditorUtility.SetDirty(target);
    }

    private void OnPropertyChange(string updatedValue)
    {
      BindingPropertyString = updatedValue;

      var splitted = updatedValue.Split('.');
      _methodName = splitted[splitted.Length - 1];
      _typeName = splitted[splitted.Length - 2];
    }

    private BindableMember<FieldInfo>[] GetBindableMembers(IList<Type> types)
    {
      var properties = new Dictionary<Type, FieldInfo[]>();

      foreach (var type in types)
      {
        var f = type.GetFields(
          BindingFlags.Public |
          BindingFlags.Instance |
          BindingFlags.DeclaredOnly)
          .ToArray();

        if (f.Any())
        {
          properties.Add(type, f);
        }
      }

      var props = new List<BindableMember<FieldInfo>>();

      foreach (var sp in properties)
      {
        props.AddRange(sp.Value.Select(prop => new BindableMember<FieldInfo>(prop, sp.Key)));
      }

      return props.ToArray();
    }

    private void UpdateProperty<TValue>(Action<TValue> setter, TValue oldValue, TValue newValue, string undoActionName)
        where TValue : class
    {
      if (oldValue != null && newValue.ToString() == oldValue.ToString())
      {
        return;
      }

      Undo.RecordObject(target, undoActionName);

      setter(newValue);
    }

    private void ShowBindingMenu(
        IList<BindableMember<FieldInfo>> properties,
        Action<string> propertyValueSetter,
        string curPropertyValue,
        out Type selectedPropertyType
    )
    {
      var propertyNames = properties
          .Select(m => m.ToString())
          .ToArray();
      var selectedIndex = Array.IndexOf(propertyNames, curPropertyValue);
      var content = properties.Select(prop => new GUIContent(string.Concat(
              prop.TypeName,
              "/",
              prop.MemberName,
              " : ",
              prop.Member.FieldType.Name
          )))
          .ToArray();

      var lab = "Property to bind";
      if (_methodName != null && _typeName != null)
      {
        lab = string.Concat(_typeName, ".", _methodName);
      }

      var newSelectedIndex = EditorGUILayout.Popup(new GUIContent("Binding property", lab), selectedIndex, content);
      if (newSelectedIndex != selectedIndex)
      {
        var newSelectedProperty = properties[newSelectedIndex];

        UpdateProperty(
            propertyValueSetter,
            curPropertyValue,
            newSelectedProperty.ToString(),
            "Set property"
        );

        selectedPropertyType = newSelectedProperty.Member.FieldType;
      }
      else
      {
        if (selectedIndex < 0)
        {
          selectedPropertyType = null;
          return;
        }

        selectedPropertyType = properties[selectedIndex].Member.FieldType;
      }
    }

    private static IList<Type> GetTestsTypes(string path)
    {
      var queue = new Queue<string>();
      queue.Enqueue(path);

      var types = new List<Type>();

      while (queue.Count > 0)
      {
        path = queue.Dequeue();
        foreach (var subDir in Directory.GetDirectories(path))
        {
          queue.Enqueue(subDir);
        }
        var files = Directory.GetFiles(path);
        var withoutExtention = files.Select(Path.GetFileNameWithoutExtension).ToArray();

        foreach (var we in withoutExtention)
        {
          var typeName = "DemoTypes." + we + ", Assembly-CSharp";
          var type = Type.GetType(typeName);
          if (type == null)
            continue;

          types.Add(type);
        }
      }

      return types;
    }

    private class BindableMember<TMemberType> where TMemberType : MemberInfo
    {
      public readonly TMemberType Member;

      private readonly Type _type;
      public string TypeName
      {
        get
        {
          return _type.Name;
        }
      }

      public string MemberName
      {
        get
        {
          return Member.Name;
        }
      }

      public BindableMember(TMemberType member, Type type)
      {
        Member = member;
        _type = type;
      }

      public override string ToString()
      {
        return string.Concat(TypeName, ".", MemberName);
      }
    }
  }
}