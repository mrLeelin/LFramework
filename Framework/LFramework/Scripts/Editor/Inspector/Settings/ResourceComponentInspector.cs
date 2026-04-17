using System;
using System.Collections.Generic;
using GameFramework.Resource;
using LFramework.Editor;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ResourceComponentSetting))]
    internal sealed class ResourceComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_ResourceMode = null;
        private SerializedProperty m_MinUnloadInterval = null;
        private SerializedProperty m_MaxUnloadInterval = null;
        private SerializedProperty m_YooAssetPackageName = null;
        private SerializedProperty m_YooAssetPlayMode = null;
        private SerializedProperty m_YooAssetDefaultPackageId = null;
        private SerializedProperty m_YooAssetBootstrapPackageId = null;
        private SerializedProperty m_YooAssetPackages = null;
        private SerializedProperty m_YooAssetRouting = null;
        private SerializedProperty m_AddressableHotfixProfileName;

        private HelperInfo<ResourceHelperBase> m_ResourceHelperInfo = new HelperInfo<ResourceHelperBase>("Resource");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawOverviewBanner();
                DrawPipelineSection();
                DrawReleaseSection();
                DrawBackendSection();
                DrawMigrationSection();
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
            m_ResourceMode = serializedObject.FindProperty("_resourceMode");
            m_MinUnloadInterval = serializedObject.FindProperty("_minUnloadInterval");
            m_MaxUnloadInterval = serializedObject.FindProperty("_maxUnloadInterval");
            m_YooAssetPackageName = serializedObject.FindProperty("_yooAssetPackageName");
            m_YooAssetPlayMode = serializedObject.FindProperty("_yooAssetPlayMode");
            m_YooAssetDefaultPackageId = serializedObject.FindProperty("_defaultPackageId");
            m_YooAssetBootstrapPackageId = serializedObject.FindProperty("_bootstrapPackageId");
            m_YooAssetPackages = serializedObject.FindProperty("_yooAssetPackages");
            m_YooAssetRouting = serializedObject.FindProperty("_routing");
            m_AddressableHotfixProfileName = serializedObject.FindProperty("_hotfixProfileName");

            m_ResourceHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_ResourceHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMigrationButtons()
        {
            bool useVerticalButtons = EditorGUIUtility.currentViewWidth < 650f;

            if (useVerticalButtons)
            {
                if (GUILayout.Button("YooAssets -> Addressables", GUILayout.Height(28f)))
                {
                    ExecuteMigration(
                        "This will rebuild generated Addressable groups and move matching entries. Continue?",
                        ResourceConfigMigrationHelper.ConvertYooAssetsToAddressables);
                }

                if (GUILayout.Button("Addressables -> YooAssets", GUILayout.Height(28f)))
                {
                    ExecuteMigration(
                        "This will rebuild the target YooAssets package collectors. Continue?",
                        ResourceConfigMigrationHelper.ConvertAddressablesToYooAssets);
                }
            }
            else
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("YooAssets -> Addressables", GUILayout.Height(28f)))
                    {
                        ExecuteMigration(
                            "This will rebuild generated Addressable groups and move matching entries. Continue?",
                            ResourceConfigMigrationHelper.ConvertYooAssetsToAddressables);
                    }

                    if (GUILayout.Button("Addressables -> YooAssets", GUILayout.Height(28f)))
                    {
                        ExecuteMigration(
                            "This will rebuild the target YooAssets package collectors. Continue?",
                            ResourceConfigMigrationHelper.ConvertAddressablesToYooAssets);
                    }
                }
            }
        }

        private void ExecuteMigration(
            string confirmationMessage,
            Func<ResourceComponentSetting, ResourceConfigMigrationHelper.ResourceConfigMigrationResult> action)
        {
            if (!EditorUtility.DisplayDialog("Resource Migration", confirmationMessage, "Continue", "Cancel"))
            {
                return;
            }

            var result = action((ResourceComponentSetting)target);
            var dialogTitle = result.Success ? "Migration Success" : "Migration Failed";
            var dialogBody = $"{result.Summary}\nReport: {result.ReportPath}";
            EditorUtility.DisplayDialog(dialogTitle, dialogBody, "OK");
        }

        private void DrawOverviewBanner()
        {
            string modeName = m_ResourceMode.enumDisplayNames[m_ResourceMode.enumValueIndex];
            string message = m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset
                ? "YooAsset is active. Configure logical packages, bootstrap routing, and package preview below."
                : "Addressables is active. Hotfix profile configuration and migration actions are shown below.";

            EditorGUILayout.HelpBox($"Active Mode: {modeName}\n{message}", MessageType.Info);
        }

        private void DrawPipelineSection()
        {
            BeginSection("Pipeline", "Select the active runtime backend and the helper that serves it.");
            EditorGUILayout.PropertyField(m_ResourceMode);
            m_ResourceHelperInfo.Draw();
            EndSection();
        }

        private void DrawReleaseSection()
        {
            BeginSection("Release Policy", "Tune how aggressively unused resources are released at runtime.");
            EditorGUILayout.PropertyField(m_MinUnloadInterval);
            EditorGUILayout.PropertyField(m_MaxUnloadInterval);

            if (m_MinUnloadInterval.floatValue > m_MaxUnloadInterval.floatValue)
            {
                EditorGUILayout.HelpBox(
                    "Min Unload Interval is greater than Max Unload Interval. Runtime release cadence may become confusing.",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Unload Window: {m_MinUnloadInterval.floatValue:0.##}s - {m_MaxUnloadInterval.floatValue:0.##}s",
                    MessageType.None);
            }

            EndSection();
        }

        private void DrawBackendSection()
        {
            if (m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset)
            {
                DrawYooAssetSection();
                return;
            }

            BeginSection("Addressables Settings", "Configure the hotfix profile used by the Addressables pipeline.");
            EditorGUILayout.PropertyField(m_AddressableHotfixProfileName);

            if (string.IsNullOrWhiteSpace(m_AddressableHotfixProfileName.stringValue))
            {
                EditorGUILayout.HelpBox("Hotfix profile name is empty. Addressables hotfix content may not resolve the expected profile.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"Hotfix Profile: {m_AddressableHotfixProfileName.stringValue}", MessageType.None);
            }

            EndSection();
        }

        private void DrawYooAssetSection()
        {
            BeginSection("YooAsset Settings", "Configure compatibility fields, logical packages, bootstrap routing, and active package preview.");

            EditorGUILayout.PropertyField(m_YooAssetPackageName);
            EditorGUILayout.PropertyField(m_YooAssetPlayMode);
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(m_YooAssetDefaultPackageId);
            EditorGUILayout.PropertyField(m_YooAssetBootstrapPackageId);
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(m_YooAssetPackages, true);
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(m_YooAssetRouting, true);
            EditorGUILayout.Space(6f);

            DrawValidationMessages((ResourceComponentSetting)target);
            DrawActivePackagePreview((ResourceComponentSetting)target);

            EndSection();
        }

        private void DrawValidationMessages(ResourceComponentSetting setting)
        {
            bool isValid = setting.ValidateMultiPackageConfiguration(out List<string> errors, out List<string> warnings);

            if (!isValid)
            {
                foreach (string error in errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
            }
            else
            {
                if (errors.Count == 0 && warnings.Count == 0)
                {
                    EditorGUILayout.HelpBox("Multi-package configuration is valid for the current editor state.", MessageType.None);
                }
            }

            foreach (string warning in warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }
        }

        private void DrawActivePackagePreview(ResourceComponentSetting setting)
        {
            RuntimePlatform platform = GetPreviewRuntimePlatform();
            string channel = GetPreviewChannel();
            List<string> lines = BuildActivePackagePreview(setting, platform, channel);

            EditorGUILayout.LabelField("Active Package Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Preview Context: {platform} / Channel: {channel}\n" +
                "This preview shows which logical packages are active for the current editor build target and channel.",
                MessageType.Info);

            if (lines.Count == 0)
            {
                EditorGUILayout.HelpBox("No active package definitions are currently matched.", MessageType.Warning);
                return;
            }

            foreach (string line in lines)
            {
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            }
        }

        private static RuntimePlatform GetPreviewRuntimePlatform()
        {
            return EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneWindows => RuntimePlatform.WindowsPlayer,
                BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                _ => RuntimePlatform.WindowsEditor
            };
        }

        private static string GetPreviewChannel()
        {
            try
            {
                GameSetting gameSetting = SettingManager.GetSetting<GameSetting>();
                if (gameSetting != null && !string.IsNullOrWhiteSpace(gameSetting.channel))
                {
                    return gameSetting.channel;
                }
            }
            catch
            {
                // Keep preview resilient when project settings are not initialized yet.
            }

            return "Unknown";
        }

        private static List<string> BuildActivePackagePreview(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            var lines = new List<string>();
            if (setting == null)
            {
                return lines;
            }

            IReadOnlyList<PackageDefinition> effectivePackages = setting.GetEffectivePackageDefinitions();
            if (setting.YooAssetPackages.Count == 0 && effectivePackages.Count == 1)
            {
                lines.Add($"Legacy -> {effectivePackages[0].yooPackageName}");
                return lines;
            }

            var registry = new PackageRegistry();
            registry.Configure(effectivePackages, platform, channel);

            foreach (KeyValuePair<string, PackageDefinition> pair in registry.ActivePackages)
            {
                PackageDefinition definition = pair.Value;
                lines.Add(
                    $"{pair.Key} -> {definition.yooPackageName}  " +
                    $"(play: {definition.playModeOverride}, init: {definition.initOnLaunch}, update: {definition.updateManifestOnLaunch}, download: {definition.downloadOnLaunch})");
            }

            return lines;
        }

        private void DrawMigrationSection()
        {
            BeginSection("Migration Tools", "Rebuild generated configuration when switching between YooAsset and Addressables.");
            EditorGUILayout.HelpBox(
                "Both migration actions keep Unity-side APIs on the main thread and generate a report path after completion.",
                MessageType.Info);
            DrawMigrationButtons();
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
