using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(LSystemApplicationBehaviour), true)]
    internal class LSystemApplicationBehaviourInspector : GameFrameworkInspector
    {
        private SerializedProperty _allComponentTypes;


        private string[] _currentComponentRegisterTypes;


        private bool _isCompileComplete;


        private void OnEnable()
        {
            _allComponentTypes = serializedObject.FindProperty("allComponentTypes");

            RefreshRegisterTypes();

            _isCompileComplete = true;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Popup("ComponentRegister", 0, _currentComponentRegisterTypes);

            if (_isCompileComplete)
            {
                _allComponentTypes.ClearArray();
                for (int i = 0; i < _currentComponentRegisterTypes.Length; i++)
                {
                    var registerFullNames = _currentComponentRegisterTypes[i];
                    _allComponentTypes.InsertArrayElementAtIndex(i);
                    _allComponentTypes.GetArrayElementAtIndex(i).stringValue = registerFullNames;
                }
            }

            serializedObject.ApplyModifiedProperties();
            _isCompileComplete = false;
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshRegisterTypes();

            _isCompileComplete = true;
        }

        private void RefreshRegisterTypes()
        {
            var componentTypes = Type.GetRuntimeTypes(typeof(GameFrameworkComponent));
            _currentComponentRegisterTypes = componentTypes.Where(x => x != null).Select(x => x.FullName).ToArray();
        }
    }
}
