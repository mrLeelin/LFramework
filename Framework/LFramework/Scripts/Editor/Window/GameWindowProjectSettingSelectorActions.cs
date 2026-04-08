using System;
using System.Linq;
using LFramework.Editor.Settings;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    /// <summary>
    /// GameWindow 中 ProjectSettingSelector 页的快捷操作。
    /// </summary>
    public static class GameWindowProjectSettingSelectorActions
    {
        public static ProjectSettingSelector CollectAllSettings(ProjectSettingSelector selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            SettingProjectInitializer.InitializeProjectSettings();

            SettingSyncState syncState = LoadOrCreateSyncState();
            var templates = SettingProjectInitializer.LoadTemplateAssets();
            SettingSyncService.SyncTemplates(selector, syncState, templates, SettingProjectInitializer.GetProjectAssetPath);

            EditorUtility.SetDirty(selector);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SettingManager.ClearCacheForTests();
            return selector;
        }

        public static SettingValidationReport ValidateAllSettings(ProjectSettingSelector selector)
        {
            if (selector == null)
            {
                throw new ArgumentNullException(nameof(selector));
            }

            SettingSyncState syncState = LoadOrCreateSyncState();
            var templates = SettingProjectInitializer.LoadTemplateAssets();
            return SettingValidationService.Validate(selector, syncState, templates);
        }

        public static string BuildCollectionSummary(ProjectSettingSelector selector)
        {
            if (selector == null)
            {
                return "ProjectSettingSelector is missing.";
            }

            return $"Collected {selector.GetAllSettings().Count} base settings and {selector.GetAllComponentSettings().Count} component settings.";
        }

        public static string BuildValidationSummary(SettingValidationReport report)
        {
            if (report == null)
            {
                return "No validation report available.";
            }

            if (report.Issues.Count == 0)
            {
                return "No validation issues found.";
            }

            return string.Join("\n", report.Issues.Select(issue => $"- [{issue.severity}] {issue.message}"));
        }

        private static SettingSyncState LoadOrCreateSyncState()
        {
            SettingProjectInitializer.InitializeProjectSettings();
            SettingSyncState syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            if (syncState != null)
            {
                return syncState;
            }

            throw new InvalidOperationException($"[GameWindowProjectSettingSelectorActions] Failed to load sync state at '{SettingProjectPaths.SyncStateAssetPath}'.");
        }
    }
}
