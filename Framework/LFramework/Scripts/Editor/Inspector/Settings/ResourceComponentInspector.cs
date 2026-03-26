using System;
using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;
using LocalResourceServerController = LFramework.Editor.LocalResourceServerController;

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
        private LocalResourceServerController m_LocalResourceServerController;
        private int m_LocalResourceServerPort;
        private string m_LocalResourceServerMessage;
        private MessageType m_LocalResourceServerMessageType = MessageType.Info;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_ResourceMode);
                m_ResourceHelperInfo.Draw();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Resource Release Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_MinUnloadInterval);
                EditorGUILayout.PropertyField(m_MaxUnloadInterval);

                if (m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("YooAsset Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_YooAssetPackageName);
                    EditorGUILayout.PropertyField(m_YooAssetPlayMode);
                }
                else
                {
                    //Addressable in this
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Addressable Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_AddressableHotfixProfileName);
                }

                EditorGUILayout.Space();
                DrawMigrationButtons();

                DrawLocalResourceServerSection();
                
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

            m_LocalResourceServerController = new LocalResourceServerController();
            m_LocalResourceServerPort = m_LocalResourceServerController.Port;
        }

        private void RefreshTypeNames()
        {
            m_ResourceHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawMigrationButtons()
        {
            EditorGUILayout.LabelField("Resource Migration", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Main thread handles Unity APIs. Worker threads handle validation, conflict checks, and migration planning.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("YooAssets -> Addressables", GUILayout.Height(28)))
                {
                    ExecuteMigration(
                        "This will rebuild generated Addressable groups and move matching entries. Continue?",
                        ResourceConfigMigrationHelper.ConvertYooAssetsToAddressables);
                }

                if (GUILayout.Button("Addressables -> YooAssets", GUILayout.Height(28)))
                {
                    ExecuteMigration(
                        "This will rebuild the target YooAssets package collectors. Continue?",
                        ResourceConfigMigrationHelper.ConvertAddressablesToYooAssets);
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

        private void DrawLocalResourceServerSection()
        {
            if (m_LocalResourceServerController == null)
            {
                return;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Local Resource Server", EditorStyles.boldLabel);

            EditorGUI.BeginDisabledGroup(m_LocalResourceServerController.IsRunning);
            {
                var portValue = EditorGUILayout.IntField("Port", m_LocalResourceServerPort);
                if (portValue != m_LocalResourceServerPort)
                {
                    m_LocalResourceServerPort = portValue;
                    m_LocalResourceServerController.Port = m_LocalResourceServerPort;
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(true);
            {
                EditorGUILayout.Toggle("Is Running", m_LocalResourceServerController.IsRunning);
                EditorGUILayout.TextField("ServerData Path", m_LocalResourceServerController.RootDirectory ?? string.Empty);
            }
            EditorGUI.EndDisabledGroup();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(m_LocalResourceServerController.IsRunning);
                if (GUILayout.Button("Start", GUILayout.Height(28)))
                {
                    TryStartLocalResourceServer();
                }
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!m_LocalResourceServerController.IsRunning);
                if (GUILayout.Button("Stop", GUILayout.Height(28)))
                {
                    m_LocalResourceServerController.Stop();
                    m_LocalResourceServerMessage = "Local resource server stopped.";
                    m_LocalResourceServerMessageType = MessageType.Info;
                }
                EditorGUI.EndDisabledGroup();
            }

            if (!string.IsNullOrEmpty(m_LocalResourceServerMessage))
            {
                EditorGUILayout.HelpBox(m_LocalResourceServerMessage, m_LocalResourceServerMessageType);
            }
        }

        private void TryStartLocalResourceServer()
        {
            if (m_LocalResourceServerController == null)
            {
                return;
            }

            m_LocalResourceServerController.EnsureServerDataDirectory();
            m_LocalResourceServerController.Port = m_LocalResourceServerPort;

            if (m_LocalResourceServerController.TryStart(out string errorMessage))
            {
                m_LocalResourceServerMessage = $"Local resource server running at {m_LocalResourceServerController.BaseUrl}.";
                m_LocalResourceServerMessageType = MessageType.Info;
            }
            else
            {
                m_LocalResourceServerMessage = string.IsNullOrEmpty(errorMessage) ? "Failed to start local resource server." : errorMessage;
                m_LocalResourceServerMessageType = MessageType.Error;
            }
        }
    }
}
