
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(SceneComponentSetting))]
    internal sealed class SceneComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnableLoadSceneUpdateEvent = null;
        private SerializedProperty m_EnableLoadSceneDependencyAssetEvent = null;

       
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

      
            
            serializedObject.Update();

           
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_EnableLoadSceneUpdateEvent);
                EditorGUILayout.PropertyField(m_EnableLoadSceneDependencyAssetEvent);
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            Repaint();  
            
        }

        protected override void OnEnable()
        {
            m_EnableLoadSceneUpdateEvent = serializedObject.FindProperty("m_EnableLoadSceneUpdateEvent");
            m_EnableLoadSceneDependencyAssetEvent = serializedObject.FindProperty("m_EnableLoadSceneDependencyAssetEvent");
        }

       
    }
}