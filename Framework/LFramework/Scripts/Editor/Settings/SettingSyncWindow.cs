using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// Setting 同步窗口。
    /// </summary>
    public class SettingSyncWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private ProjectSettingSelector _selector;
        private SettingSyncState _syncState;
        private List<ScriptableObject> _templates = new();

        public SettingSyncReport CurrentReport { get; private set; }

        [MenuItem("LFramework/Settings/Open Sync Window")]
        public static SettingSyncWindow OpenWindow()
        {
            var window = GetWindow<SettingSyncWindow>();
            window.titleContent = new GUIContent("Setting Sync");
            window.minSize = new Vector2(720f, 360f);
            window.RefreshFromProject();
            return window;
        }

        public void RefreshFromProject()
        {
            _selector = SettingManager.GetProjectSelector();
            _syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            _templates = SettingProjectInitializer.LoadTemplateAssets();
            RefreshReport(_selector, _syncState, _templates);
        }

        public void RefreshReport(
            ProjectSettingSelector selector,
            SettingSyncState syncState,
            IEnumerable<ScriptableObject> templates)
        {
            _selector = selector;
            _syncState = syncState;
            _templates = templates?.Where(template => template != null).ToList() ?? new List<ScriptableObject>();
            CurrentReport = SettingSyncService.AnalyzeTemplates(_selector, _syncState, _templates);
            Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (CurrentReport == null)
            {
                EditorGUILayout.HelpBox("No sync report loaded.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (SettingSyncItemReport item in CurrentReport.Items)
            {
                DrawItem(item);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshFromProject();
            }

            using (new EditorGUI.DisabledScope(CurrentReport == null))
            {
                if (GUILayout.Button("Sync All Safe", EditorStyles.toolbarButton))
                {
                    EnsureProjectContext();
                    SettingSyncService.SyncTemplates(_selector, _syncState, _templates, SettingProjectInitializer.GetProjectAssetPath);
                    RefreshReport(_selector, _syncState, _templates);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawItem(SettingSyncItemReport item)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(item.settingId, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Status", item.status.ToString());
            EditorGUILayout.LabelField("Type", item.settingTypeName ?? "<unknown>");

            if (item.fieldChanges.Count > 0)
            {
                foreach (SettingFieldChange change in item.fieldChanges)
                {
                    EditorGUILayout.LabelField($"- {change.fieldName}", change.action.ToString());
                }
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(item.localAsset == null))
            {
                if (GUILayout.Button("Ping Local"))
                {
                    EditorGUIUtility.PingObject(item.localAsset);
                }
            }

            using (new EditorGUI.DisabledScope(item.templateAsset == null))
            {
                if (GUILayout.Button("Ping Template"))
                {
                    EditorGUIUtility.PingObject(item.templateAsset);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void EnsureProjectContext()
        {
            _selector ??= SettingProjectInitializer.InitializeProjectSettings();
            _syncState ??= AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            if (_templates == null || _templates.Count == 0)
            {
                _templates = SettingProjectInitializer.LoadTemplateAssets();
            }
        }
    }
}
