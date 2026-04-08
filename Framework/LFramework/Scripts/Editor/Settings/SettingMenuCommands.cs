using System.Linq;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// Setting 菜单入口。
    /// </summary>
    public static class SettingMenuCommands
    {
        [MenuItem("LFramework/Settings/Initialize Project Settings")]
        public static void InitializeProjectSettings()
        {
            ProjectSettingSelector selector = SettingProjectInitializer.InitializeProjectSettings();
            OpenProjectSettings(selector);
        }

        [MenuItem("LFramework/Settings/Sync From Package")]
        public static void SyncFromPackage()
        {
            ProjectSettingSelector selector = SettingProjectInitializer.InitializeProjectSettings();
            SettingSyncState syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            var templates = SettingProjectInitializer.LoadTemplateAssets();
            SettingSyncService.SyncTemplates(selector, syncState, templates, SettingProjectInitializer.GetProjectAssetPath);
            OpenProjectSettings(selector);
        }

        [MenuItem("LFramework/Settings/Validate Project Settings")]
        public static void ValidateProjectSettings()
        {
            ProjectSettingSelector selector = SettingManager.GetProjectSelector();
            SettingSyncState syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            SettingValidationReport report = SettingValidationService.Validate(
                selector,
                syncState,
                SettingProjectInitializer.LoadTemplateAssets());

            if (report.Issues.Count == 0)
            {
                EditorUtility.DisplayDialog("Validate Project Settings", "No validation issues found.", "OK");
                return;
            }

            string message = string.Join("\n", report.Issues.Select(issue => $"- [{issue.severity}] {issue.message}"));
            Debug.LogWarning($"[SettingMenuCommands] Validation issues:\n{message}");
            EditorUtility.DisplayDialog("Validate Project Settings", message, "OK");
        }

        [MenuItem("LFramework/Settings/Open Project Settings")]
        public static void OpenProjectSettings()
        {
            OpenProjectSettings(SettingManager.GetProjectSelector());
        }

        public static void OpenProjectSettings(ProjectSettingSelector selector)
        {
            if (selector == null)
            {
                Debug.LogWarning("[SettingMenuCommands] ProjectSettingSelector not found.");
                return;
            }

            Selection.activeObject = selector;
            EditorGUIUtility.PingObject(selector);
        }
    }
}
