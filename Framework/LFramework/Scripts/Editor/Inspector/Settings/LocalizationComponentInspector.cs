using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(LocalizationComponentSetting))]
    internal sealed class LocalizationComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnableLoadDictionaryUpdateEvent = null;
        private SerializedProperty m_EnableLoadDictionaryDependencyAssetEvent = null;
        private SerializedProperty m_CachedBytesSize = null;

        private readonly HelperInfo<LocalizationHelperBase> m_LocalizationHelperInfo =
            new HelperInfo<LocalizationHelperBase>("Localization");

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
            m_EnableLoadDictionaryUpdateEvent = serializedObject.FindProperty("m_EnableLoadDictionaryUpdateEvent");
            m_EnableLoadDictionaryDependencyAssetEvent =
                serializedObject.FindProperty("m_EnableLoadDictionaryDependencyAssetEvent");
            m_CachedBytesSize = serializedObject.FindProperty("m_CachedBytesSize");

            m_LocalizationHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_LocalizationHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            EditorGUILayout.HelpBox(
                $"Cached Bytes Size: {m_CachedBytesSize.intValue}\n" +
                "Dictionary load events, helper binding, and cache size are grouped below.",
                MessageType.Info);
        }

        private void DrawEventSection()
        {
            BeginSection("Event Dispatch", "Choose whether dictionary load progress and dependency events should be emitted.");
            EditorGUILayout.PropertyField(m_EnableLoadDictionaryUpdateEvent);
            EditorGUILayout.PropertyField(m_EnableLoadDictionaryDependencyAssetEvent);
            EndSection();
        }

        private void DrawStorageSection()
        {
            BeginSection("Helper & Cache", "Configure the localization helper implementation and the byte cache budget.");
            m_LocalizationHelperInfo.Draw();
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
