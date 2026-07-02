using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
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

            SyncSerializedComponentTypes();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();

            EditorGUILayout.Popup("ComponentRegister", 0, _currentComponentRegisterTypes);

            if (_isCompileComplete)
            {
                SyncSerializedComponentTypes();
            }

            serializedObject.ApplyModifiedProperties();
            _isCompileComplete = false;
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshRegisterTypes();

            SyncSerializedComponentTypes();
            Repaint();
        }

        private void RefreshRegisterTypes()
        {
            _currentComponentRegisterTypes = LSystemApplicationComponentTypeRegistry.GetRuntimeComponentTypeNames();
        }

        private void SyncSerializedComponentTypes()
        {
            serializedObject.Update();
            LSystemApplicationComponentTypeRegistry.SyncSerializedComponentTypes(
                _allComponentTypes,
                _currentComponentRegisterTypes);
            serializedObject.ApplyModifiedProperties();
            _isCompileComplete = false;
        }
    }

    [InitializeOnLoad]
    internal static class LSystemApplicationComponentTypeRegistry
    {
        static LSystemApplicationComponentTypeRegistry()
        {
            EditorApplication.delayCall -= RefreshLoadedApplicationBehaviours;
            EditorApplication.delayCall += RefreshLoadedApplicationBehaviours;
        }

        internal static string[] GetRuntimeComponentTypeNames()
        {
            var componentTypes = Type.GetRuntimeTypes(typeof(GameFrameworkComponent));
            return componentTypes
                .Where(type => type != null && !string.IsNullOrWhiteSpace(type.FullName))
                .Select(type => type.FullName)
                .Distinct()
                .ToArray();
        }

        internal static bool SyncSerializedComponentTypes(
            SerializedProperty allComponentTypes,
            string[] componentTypeNames)
        {
            if (allComponentTypes == null)
            {
                return false;
            }

            componentTypeNames ??= Array.Empty<string>();
            if (ContainsSameValues(allComponentTypes, componentTypeNames))
            {
                return false;
            }

            allComponentTypes.ClearArray();
            for (int i = 0; i < componentTypeNames.Length; i++)
            {
                allComponentTypes.InsertArrayElementAtIndex(i);
                allComponentTypes.GetArrayElementAtIndex(i).stringValue = componentTypeNames[i];
            }

            return true;
        }

        private static void RefreshLoadedApplicationBehaviours()
        {
            var componentTypeNames = GetRuntimeComponentTypeNames();
            foreach (var application in Resources.FindObjectsOfTypeAll<LSystemApplicationBehaviour>())
            {
                if (application == null || EditorUtility.IsPersistent(application))
                {
                    continue;
                }

                var serializedApplication = new SerializedObject(application);
                var allComponentTypes = serializedApplication.FindProperty("allComponentTypes");
                serializedApplication.Update();

                if (!SyncSerializedComponentTypes(allComponentTypes, componentTypeNames))
                {
                    continue;
                }

                serializedApplication.ApplyModifiedProperties();
                EditorUtility.SetDirty(application);

                Scene scene = application.gameObject.scene;
                if (scene.IsValid() && scene.isLoaded)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                }
            }
        }

        private static bool ContainsSameValues(SerializedProperty property, string[] values)
        {
            if (property.arraySize != values.Length)
            {
                return false;
            }

            for (int i = 0; i < values.Length; i++)
            {
                if (property.GetArrayElementAtIndex(i).stringValue != values[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
