using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Editor;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ComponentSetting))]
    public abstract class ComponentSettingInspector : GameFrameworkInspector
    {
        private SerializedProperty _bindTypeName;
        private List<string> _allComponentNames;
        private int _index;
        private GameFrameworkComponent _gameFrameworkComponent;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _bindTypeName ??= serializedObject.FindProperty("bindTypeName");
            if (_allComponentNames == null)
            {
                RefreshTypeNames();
            }

            GameWindowChrome.DrawCompactHeader("Binding", "Choose the runtime component type that this setting asset should bind to.");
            _index = EditorGUILayout.Popup("Bind Component", _index, _allComponentNames.ToArray());
            GUILayout.Space(4f);
            if (_index >= 0)
            {
                _bindTypeName.stringValue = _allComponentNames[_index];
            }
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
        }

        protected virtual void OnEnable()
        {
            _bindTypeName = serializedObject.FindProperty("bindTypeName");
            RefreshTypeNames();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _allComponentNames = Type.GetRuntimeTypeNames(typeof(GameFrameworkComponent)).ToList();
            
            _index = _allComponentNames.IndexOf(_bindTypeName.stringValue);
        }

        protected T GetComponent<T>() where T : GameFrameworkComponent
        {
            if (!EditorApplication.isPlaying)
            {
                return null;
            }

            _gameFrameworkComponent ??= LFrameworkAspect.Instance.Get<T>();
            return _gameFrameworkComponent as T;
        }
    }
}
