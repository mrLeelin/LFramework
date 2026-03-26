using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;

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
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("Scene loading event switches are grouped below and remain edit-time only.", MessageType.Info);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                BeginSection("Event Dispatch", "Choose whether scene load progress and dependency events should be emitted.");
                EditorGUILayout.PropertyField(m_EnableLoadSceneUpdateEvent);
                EditorGUILayout.PropertyField(m_EnableLoadSceneDependencyAssetEvent);
                EndSection();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EnableLoadSceneUpdateEvent = serializedObject.FindProperty("m_EnableLoadSceneUpdateEvent");
            m_EnableLoadSceneDependencyAssetEvent = serializedObject.FindProperty("m_EnableLoadSceneDependencyAssetEvent");
        }

        private static void BeginSection(string title, string subtitle)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GameWindowChrome.DrawCompactHeader(title, subtitle);
            EditorGUILayout.Space(4f);
        }

        private static void EndSection()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }
}
