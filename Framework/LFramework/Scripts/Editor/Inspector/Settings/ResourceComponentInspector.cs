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
        private SerializedProperty m_YooAssetDefaultPackageId = null;
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
            m_YooAssetDefaultPackageId = serializedObject.FindProperty("_defaultPackageId");
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

#if YOOASSET_SUPPORT && ADDRESSABLE_SUPPORT
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
#endif

        private void DrawOverviewBanner()
        {
            string modeName = m_ResourceMode.enumDisplayNames[m_ResourceMode.enumValueIndex];
            string message = m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset
                ? "YooAsset is active. Configure logical packages, route-index routing, and package preview below."
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
#if YOOASSET_SUPPORT
                DrawYooAssetSection();
#else
                BeginSection("YooAsset Settings", "YooAsset support is disabled for the current compilation.");
                EditorGUILayout.HelpBox(
                    "YOOASSET_SUPPORT is not defined. Enable YooAssets support before editing YooAsset package and route-index settings.",
                    MessageType.Warning);
                EndSection();
#endif
                return;
            }

            DrawAddressableSection();
        }

        private void DrawAddressableSection()
        {
#if ADDRESSABLE_SUPPORT
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
#else
            BeginSection("Addressables Settings", "Addressables support is disabled for the current compilation.");
            EditorGUILayout.HelpBox(
                "ADDRESSABLE_SUPPORT is not defined. Enable Addressables support before editing Addressables hotfix settings.",
                MessageType.Warning);
            EndSection();
#endif
        }

        private void DrawYooAssetSection()
        {
            BeginSection("YooAsset Settings", "Configure logical packages, route-index routing, and active package preview.");

            EditorGUILayout.PropertyField(m_YooAssetDefaultPackageId);
            EditorGUILayout.Space(4f);
            EditorGUILayout.PropertyField(m_YooAssetPackages, true);
            EditorGUILayout.Space(4f);
            DrawRoutingSettings();
            DrawRoutingQuickActions((ResourceComponentSetting)target);
#if YOOASSET_SUPPORT
            EditorGUILayout.Space(6f);
            DrawRouteIndexGenerationControls((ResourceComponentSetting)target);
#endif
            EditorGUILayout.Space(6f);

            DrawValidationMessages((ResourceComponentSetting)target);
            DrawActivePackagePreview((ResourceComponentSetting)target);

            EndSection();
        }

        private void DrawRoutingSettings()
        {
            if (m_YooAssetRouting == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(m_YooAssetRouting, false);
            if (!m_YooAssetRouting.isExpanded)
            {
                return;
            }

            SerializedProperty enableRouteIndex = m_YooAssetRouting.FindPropertyRelative("enableRouteIndex");
            SerializedProperty routeIndexAddress = m_YooAssetRouting.FindPropertyRelative("routeIndexAddress");
            SerializedProperty routeIndexPackageId = m_YooAssetRouting.FindPropertyRelative("routeIndexPackageId");
            SerializedProperty routeIndexAssetPath = m_YooAssetRouting.FindPropertyRelative("routeIndexAssetPath");
            SerializedProperty allowDefaultPackageFallback = m_YooAssetRouting.FindPropertyRelative("allowDefaultPackageFallback");
            SerializedProperty detectDuplicateAddress = m_YooAssetRouting.FindPropertyRelative("detectDuplicateAddress");

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(enableRouteIndex);
            EditorGUILayout.PropertyField(routeIndexAddress);
            DrawRouteIndexPackageIdSelector(routeIndexPackageId);
            EditorGUILayout.PropertyField(routeIndexAssetPath);
            EditorGUILayout.PropertyField(allowDefaultPackageFallback);
            EditorGUILayout.PropertyField(detectDuplicateAddress);
            EditorGUI.indentLevel--;
        }

        private void DrawRouteIndexPackageIdSelector(SerializedProperty routeIndexPackageId)
        {
            if (routeIndexPackageId == null)
            {
                return;
            }

            List<string> packageIds = CollectRouteIndexPackageIds(m_YooAssetPackages);
            string currentValue = routeIndexPackageId.stringValue;
            var optionValues = new List<string> { string.Empty };
            var optionLabels = new List<string> { "Use Default Package" };

            for (int i = 0; i < packageIds.Count; i++)
            {
                optionValues.Add(packageIds[i]);
                optionLabels.Add(packageIds[i]);
            }

            if (!string.IsNullOrWhiteSpace(currentValue) && !optionValues.Contains(currentValue))
            {
                optionValues.Add(currentValue);
                optionLabels.Add($"{currentValue} (Missing)");
            }

            int selectedIndex = optionValues.IndexOf(currentValue);
            if (selectedIndex < 0)
            {
                selectedIndex = 0;
            }

            using (new EditorGUI.DisabledScope(optionValues.Count == 1 && string.IsNullOrWhiteSpace(currentValue)))
            {
                int nextIndex = EditorGUILayout.Popup(routeIndexPackageId.displayName, selectedIndex, optionLabels.ToArray());
                if (nextIndex >= 0 && nextIndex < optionValues.Count)
                {
                    routeIndexPackageId.stringValue = optionValues[nextIndex];
                }
            }

            if (packageIds.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No package ids are available yet. Add entries in YooAssetPackages above before selecting a Route Index package.",
                    MessageType.Warning);
            }
            else if (!string.IsNullOrWhiteSpace(currentValue) && !packageIds.Contains(currentValue))
            {
                EditorGUILayout.HelpBox(
                    $"The current Route Index package '{currentValue}' is not present in YooAssetPackages. Please reselect a valid package.",
                    MessageType.Warning);
            }
        }

        private void DrawRoutingQuickActions(ResourceComponentSetting setting)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Ping RouteIndex", GUILayout.Width(140f), GUILayout.Height(24f)))
                {
                    serializedObject.ApplyModifiedProperties();
                    PingRouteIndexAsset(setting);
                    serializedObject.Update();
                }
            }
        }

        private void DrawRouteIndexGenerationControls(ResourceComponentSetting setting)
        {
#if YOOASSET_SUPPORT
            EditorGUILayout.LabelField("Route Index", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Generate the route index asset and refresh the generated collector so the build pipeline can pick it up automatically.",
                MessageType.Info);

            if (GUILayout.Button("Generate Route Index", GUILayout.Height(28f)))
            {
                serializedObject.ApplyModifiedProperties();
                ExecuteRouteIndexGeneration(setting);
                serializedObject.Update();
            }
#endif
        }

#if YOOASSET_SUPPORT
        private void ExecuteRouteIndexGeneration(ResourceComponentSetting setting)
        {
            RouteIndexGenerationResult result = RouteIndexGenerator.Generate(setting);
            if (result.Succeeded)
            {
                EditorUtility.DisplayDialog(
                    "Route Index Generated",
                    $"Asset: {result.AssetPath}\nEntries: {result.EntryCount}",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Route Index Generation Failed",
                    result.ErrorMessage ?? "Unknown error.",
                    "OK");
            }
        }
#endif

        private static void PingRouteIndexAsset(ResourceComponentSetting setting)
        {
            string assetPath = ResolveRouteIndexAssetPath(setting);
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog(
                    "Route Index Not Configured",
                    "Routing.routeIndexAssetPath is empty.",
                    "OK");
                return;
            }

            UnityEngine.Object routeIndexAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (routeIndexAsset == null)
            {
                EditorUtility.DisplayDialog(
                    "Route Index Not Found",
                    $"No asset exists at:\n{assetPath}",
                    "OK");
                return;
            }

            Selection.activeObject = routeIndexAsset;
            EditorGUIUtility.PingObject(routeIndexAsset);
        }

        private static string ResolveRouteIndexAssetPath(ResourceComponentSetting setting)
        {
            return setting?.YooAssetRouting?.routeIndexAssetPath ?? string.Empty;
        }

        private static List<string> CollectRouteIndexPackageIds(SerializedProperty yooAssetPackagesProperty)
        {
            var packageIds = new List<string>();
            if (yooAssetPackagesProperty == null || !yooAssetPackagesProperty.isArray)
            {
                return packageIds;
            }

            var seenPackageIds = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < yooAssetPackagesProperty.arraySize; i++)
            {
                SerializedProperty packageProperty = yooAssetPackagesProperty.GetArrayElementAtIndex(i);
                SerializedProperty packageIdProperty = packageProperty?.FindPropertyRelative("packageId");
                string packageId = packageIdProperty?.stringValue;
                if (string.IsNullOrWhiteSpace(packageId))
                {
                    continue;
                }

                if (seenPackageIds.Add(packageId))
                {
                    packageIds.Add(packageId);
                }
            }

            return packageIds;
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
#if YOOASSET_SUPPORT && ADDRESSABLE_SUPPORT
            EditorGUILayout.HelpBox(
                "Both migration actions keep Unity-side APIs on the main thread and generate a report path after completion.",
                MessageType.Info);
            DrawMigrationButtons();
#else
            EditorGUILayout.HelpBox(GetMigrationDisabledMessage(), MessageType.Warning);
#endif
            EndSection();
        }

        private static string GetMigrationDisabledMessage()
        {
            string yooAssetState = "disabled";
            string addressableState = "disabled";
#if YOOASSET_SUPPORT
            yooAssetState = "enabled";
#endif
#if ADDRESSABLE_SUPPORT
            addressableState = "enabled";
#endif
            return "Migration tools require both YOOASSET_SUPPORT and ADDRESSABLE_SUPPORT. " +
                   $"Current state: YOOASSET_SUPPORT is {yooAssetState}, ADDRESSABLE_SUPPORT is {addressableState}.";
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
