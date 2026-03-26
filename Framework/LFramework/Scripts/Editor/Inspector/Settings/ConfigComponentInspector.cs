using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ConfigComponentSetting))]
    internal sealed class ConfigComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnableLoadConfigUpdateEvent = null;
        private SerializedProperty m_EnableLoadConfigDependencyAssetEvent = null;
        private SerializedProperty m_CachedBytesSize = null;

        private readonly HelperInfo<ConfigHelperBase> m_ConfigHelperInfo = new HelperInfo<ConfigHelperBase>("Config");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);
            DrawOverviewBanner();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawEventSection();
                DrawStorageSection();
            }
            EditorGUI.EndDisabledGroup();

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EnableLoadConfigUpdateEvent = serializedObject.FindProperty("m_EnableLoadConfigUpdateEvent");
            m_EnableLoadConfigDependencyAssetEvent =
                serializedObject.FindProperty("m_EnableLoadConfigDependencyAssetEvent");
            m_CachedBytesSize = serializedObject.FindProperty("m_CachedBytesSize");

            m_ConfigHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_ConfigHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            EditorGUILayout.HelpBox(
                $"Cached Bytes Size: {m_CachedBytesSize.intValue}\n" +
                "Config loading events, helper binding, and byte cache size are grouped below.",
                MessageType.Info);
        }

        private void DrawEventSection()
        {
            BeginSection("Event Dispatch", "Choose whether config load progress and dependency events should be emitted.");
            EditorGUILayout.PropertyField(m_EnableLoadConfigUpdateEvent);
            EditorGUILayout.PropertyField(m_EnableLoadConfigDependencyAssetEvent);
            EndSection();
        }

        private void DrawStorageSection()
        {
            BeginSection("Helper & Cache", "Configure the config helper implementation and the byte cache budget.");
            m_ConfigHelperInfo.Draw();
            EditorGUILayout.PropertyField(m_CachedBytesSize);
            EndSection();
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
