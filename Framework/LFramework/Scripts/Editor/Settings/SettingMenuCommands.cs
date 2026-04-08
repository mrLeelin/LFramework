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
        public static void InitializeProjectSettings()
        {
            ProjectSettingSelector selector = SettingProjectInitializer.InitializeProjectSettings();
            OpenProjectSettings(selector);
        }

        public static void SyncFromPackage()
        {
            ProjectSettingSelector selector = SettingProjectInitializer.InitializeProjectSettings();
            SettingSyncState syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            var templates = SettingProjectInitializer.LoadTemplateAssets();
            SettingSyncService.SyncTemplates(selector, syncState, templates, SettingProjectInitializer.GetProjectAssetPath);
            OpenProjectSettings(selector);
        }

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
