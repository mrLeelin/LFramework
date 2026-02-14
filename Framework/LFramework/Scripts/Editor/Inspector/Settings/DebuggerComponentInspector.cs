
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(DebuggerComponentSetting))]
    internal sealed class DebuggerComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_Skin = null;
        private SerializedProperty m_ActiveWindow = null;
        private SerializedProperty m_ShowFullWindow = null;
        private SerializedProperty m_ConsoleWindow = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();


            EditorGUILayout.PropertyField(m_Skin);

            if (EditorApplication.isPlaying)
            {
            }
            else
            {
                EditorGUILayout.PropertyField(m_ActiveWindow);
            }

            EditorGUILayout.PropertyField(m_ShowFullWindow);
            EditorGUILayout.PropertyField(m_ConsoleWindow, true);

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnEnable()
        {
            m_Skin = serializedObject.FindProperty("m_Skin");
            m_ActiveWindow = serializedObject.FindProperty("m_ActiveWindow");
            m_ShowFullWindow = serializedObject.FindProperty("m_ShowFullWindow");
            m_ConsoleWindow = serializedObject.FindProperty("m_ConsoleWindow");
        }
    }
}