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
        private SerializedProperty _allSettings;
        private SerializedProperty _gameSetting;


        private string[] _currentComponentRegisterTypes;
        private List<ComponentSetting> _componentSettings;
        private List<GameSetting> _gameSettings;
        private GameSetting _onceGameSetting;


        private bool _isCompileComplete;


        private void OnEnable()
        {
            _allComponentTypes = serializedObject.FindProperty("allComponentTypes");
            _allSettings = serializedObject.FindProperty("allSettings");
            _gameSetting = serializedObject.FindProperty("gameSetting");

            RefreshRegisterTypes();
            RefreshSettings();
            RefreshGameSetting();

            _isCompileComplete = true;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Popup("ComponentRegister", 0, _currentComponentRegisterTypes);
            EditorGUILayout.Popup("Settings", 0, _componentSettings.Select(x => x.bindTypeName).ToArray());
            
            if (_isCompileComplete)
            {
                _allComponentTypes.ClearArray();
                for (int i = 0; i < _currentComponentRegisterTypes.Length; i++)
                {
                    var registerFullNames = _currentComponentRegisterTypes[i];
                    _allComponentTypes.InsertArrayElementAtIndex(i);
                    _allComponentTypes.GetArrayElementAtIndex(i).stringValue = registerFullNames;
                }

                _allSettings.ClearArray();
                for (int i = 0; i < _componentSettings.Count; i++)
                {
                    var setting = _componentSettings[i];
                    _allSettings.InsertArrayElementAtIndex(i);
                    _allSettings.GetArrayElementAtIndex(i).objectReferenceValue = setting;
                }

                _gameSetting.objectReferenceValue = _onceGameSetting;
                
            }

            serializedObject.ApplyModifiedProperties();
            _isCompileComplete = false;
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshRegisterTypes();
            RefreshSettings();
            RefreshGameSetting();

            _isCompileComplete = true;
        }

        private void RefreshRegisterTypes()
        {
            var componentTypes = Type.GetRuntimeTypes(typeof(GameFrameworkComponent));
            _currentComponentRegisterTypes = componentTypes.Where(x => x != null).Select(x => x.FullName).ToArray();
        }

        private void RefreshSettings()
        {
            var objs = AssetUtilities.GetAllAssetsOfType<ComponentSetting>();
            _componentSettings ??= new List<ComponentSetting>();
            _componentSettings.Clear();
            foreach (var setting in objs)
            {
                if (setting == null)
                {
                    continue;
                }

                _componentSettings.Add(setting);
            }
        }

        private void RefreshGameSetting()
        {
            _gameSettings = AssetUtilities.GetAllAssetsOfType<GameSetting>().ToList();
            var count = _gameSettings.Count;
            if (count != 1)
            {
                return;
            }

            _onceGameSetting = _gameSettings[0];

            foreach (var hybridClrSetting in AssetUtilities.GetAllAssetsOfType<HybridCLRSetting>())
            {
                _onceGameSetting.hybridClrSetting = hybridClrSetting;
                break;
            }
        }
    }
}