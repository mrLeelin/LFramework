using System;
using GameFramework.Resource;
using LFramework.Editor;
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
                ? "YooAsset is active. Package name, play mode, and migration actions are shown below."
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
                BeginSection("YooAsset Settings", "Configure the package identity and editor/runtime play mode.");
                EditorGUILayout.PropertyField(m_YooAssetPackageName);
                EditorGUILayout.PropertyField(m_YooAssetPlayMode);

                if (string.IsNullOrWhiteSpace(m_YooAssetPackageName.stringValue))
                {
                    EditorGUILayout.HelpBox("Package name is empty. YooAsset initialization will need a valid package name.", MessageType.Warning);
                }

                EndSection();
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
