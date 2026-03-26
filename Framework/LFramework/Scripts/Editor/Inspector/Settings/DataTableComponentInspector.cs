using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(DataTableComponentSetting))]
    internal sealed class DataTableComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_TableFullName = null;
        private SerializedProperty m_EnableLoadDataTableUpdateEvent = null;
        private SerializedProperty m_EnableLoadDataTableDependencyAssetEvent = null;
        private SerializedProperty m_CachedBytesSize = null;

        private readonly HelperInfo<DataTableHelperBase> m_DataTableHelperInfo =
            new HelperInfo<DataTableHelperBase>("DataTable");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);
            DrawOverviewBanner();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawIdentitySection();
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
            m_TableFullName = serializedObject.FindProperty("tableFullName");
            m_EnableLoadDataTableUpdateEvent = serializedObject.FindProperty("m_EnableLoadDataTableUpdateEvent");
            m_EnableLoadDataTableDependencyAssetEvent =
                serializedObject.FindProperty("m_EnableLoadDataTableDependencyAssetEvent");
            m_CachedBytesSize = serializedObject.FindProperty("m_CachedBytesSize");

            m_DataTableHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_DataTableHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            string tableName = string.IsNullOrWhiteSpace(m_TableFullName.stringValue)
                ? "Unassigned"
                : m_TableFullName.stringValue;
            EditorGUILayout.HelpBox(
                $"Table: {tableName}  Cached Bytes Size: {m_CachedBytesSize.intValue}\n" +
                "Identity, load events, helper binding, and cache size are grouped below.",
                MessageType.Info);
        }

        private void DrawIdentitySection()
        {
            BeginSection("Identity", "Configure the full type name or table key used by this data table setting.");
            EditorGUILayout.PropertyField(m_TableFullName, new GUIContent("Table Full Name"));
            EndSection();
        }

        private void DrawEventSection()
        {
            BeginSection("Event Dispatch", "Choose whether data table load progress and dependency events should be emitted.");
            EditorGUILayout.PropertyField(m_EnableLoadDataTableUpdateEvent);
            EditorGUILayout.PropertyField(m_EnableLoadDataTableDependencyAssetEvent);
            EndSection();
        }

        private void DrawStorageSection()
        {
            BeginSection("Helper & Cache", "Configure the data table helper implementation and the byte cache budget.");
            m_DataTableHelperInfo.Draw();
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
