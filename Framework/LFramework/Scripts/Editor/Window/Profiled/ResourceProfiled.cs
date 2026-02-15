using System;
using System.IO;
using System.Reflection;
using System.Text;
using GameFramework;
using GameFramework.Resource;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ResourceProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private ResourceComponent _resourceComponent;
        private int m_ResourceModeIndex = 0;
        private bool _isEditorResourceMode;
        private FieldInfo _editorResourceModeField;
        private FieldInfo _resourceModeField;

        internal override void Draw()
        {
            GetComponent(ref _resourceComponent);
            if (_editorResourceModeField == null)
            {
                _editorResourceModeField = _resourceComponent.GetType()
                    .GetField("m_EditorResourceMode", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (_resourceModeField == null)
            {
                _resourceModeField = _resourceComponent.GetType()
                    .GetField("m_ResourceMode", BindingFlags.NonPublic | BindingFlags.Instance);
            }

            var isEditorResourceMode = (bool)_editorResourceModeField.GetValue(_resourceComponent);
            var resourceModeIndex = (int)_resourceModeField.GetValue(_resourceComponent);

            EditorGUILayout.LabelField("Unload Unused Assets",
                Utility.Text.Format("{0:F2} / {1:F2}", _resourceComponent.LastUnloadUnusedAssetsOperationElapseSeconds,
                    _resourceComponent.MaxUnloadUnusedAssetsInterval));
            EditorGUILayout.LabelField("Read-Only Path", _resourceComponent.ReadOnlyPath.ToString());
            EditorGUILayout.LabelField("Read-Write Path", _resourceComponent.ReadWritePath.ToString());
            EditorGUILayout.LabelField("Current Variant", _resourceComponent.CurrentVariant ?? "<Unknwon>");
            EditorGUILayout.LabelField("Applicable Game Version",
                isEditorResourceMode ? "N/A" : _resourceComponent.ApplicableGameVersion ?? "<Unknwon>");
            EditorGUILayout.LabelField("Internal Resource Version",
                isEditorResourceMode ? "N/A" : _resourceComponent.InternalResourceVersion.ToString());
            EditorGUILayout.LabelField("Asset Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.AssetCount.ToString());
            EditorGUILayout.LabelField("Resource Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.ResourceCount.ToString());
            EditorGUILayout.LabelField("Resource Group Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.ResourceGroupCount.ToString());
            if (m_ResourceModeIndex > 0)
            {
                EditorGUILayout.LabelField("Applying Resource Pack Path",
                    isEditorResourceMode ? "N/A" : _resourceComponent.ApplyingResourcePackPath ?? "<Unknwon>");
                EditorGUILayout.LabelField("Apply Waiting Count",
                    isEditorResourceMode ? "N/A" : _resourceComponent.ApplyWaitingCount.ToString());
                EditorGUILayout.LabelField("Updating Resource Group",
                    isEditorResourceMode ? "N/A" :
                    _resourceComponent.UpdatingResourceGroup != null ? _resourceComponent.UpdatingResourceGroup.Name :
                    "<Unknwon>");
                EditorGUILayout.LabelField("Update Waiting Count",
                    isEditorResourceMode ? "N/A" : _resourceComponent.UpdateWaitingCount.ToString());
                EditorGUILayout.LabelField("Update Waiting While Playing Count",
                    isEditorResourceMode ? "N/A" : _resourceComponent.UpdateWaitingWhilePlayingCount.ToString());
                EditorGUILayout.LabelField("Update Candidate Count",
                    isEditorResourceMode ? "N/A" : _resourceComponent.UpdateCandidateCount.ToString());
            }

            EditorGUILayout.LabelField("Load Total Agent Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.LoadTotalAgentCount.ToString());
            EditorGUILayout.LabelField("Load Free Agent Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.LoadFreeAgentCount.ToString());
            EditorGUILayout.LabelField("Load Working Agent Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.LoadWorkingAgentCount.ToString());
            EditorGUILayout.LabelField("Load Waiting Task Count",
                isEditorResourceMode ? "N/A" : _resourceComponent.LoadWaitingTaskCount.ToString());
            if (!isEditorResourceMode)
            {
                EditorGUILayout.BeginVertical("box");
                {
                    TaskInfo[] loadAssetInfos = _resourceComponent.GetAllLoadAssetInfos();
                    if (loadAssetInfos.Length > 0)
                    {
                        foreach (TaskInfo loadAssetInfo in loadAssetInfos)
                        {
                            DrawLoadAssetInfo(loadAssetInfo);
                        }

                        if (GUILayout.Button("Export CSV Data"))
                        {
                            string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty,
                                "Load Asset Task Data.csv", string.Empty);
                            if (!string.IsNullOrEmpty(exportFileName))
                            {
                                try
                                {
                                    int index = 0;
                                    string[] data = new string[loadAssetInfos.Length + 1];
                                    data[index++] = "Load Asset Name,Serial Id,Priority,Status";
                                    foreach (TaskInfo loadAssetInfo in loadAssetInfos)
                                    {
                                        data[index++] = Utility.Text.Format("{0},{1},{2},{3}",
                                            loadAssetInfo.Description, loadAssetInfo.SerialId, loadAssetInfo.Priority,
                                            loadAssetInfo.Status);
                                    }

                                    File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                    Debug.Log(Utility.Text.Format("Export load asset task CSV data to '{0}' success.",
                                        exportFileName));
                                }
                                catch (Exception exception)
                                {
                                    Debug.LogError(Utility.Text.Format(
                                        "Export load asset task CSV data to '{0}' failure, exception is '{1}'.",
                                        exportFileName, exception));
                                }
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("Load Asset Task is Empty ...");
                    }
                }
                EditorGUILayout.EndVertical();


                m_ResourceModeIndex = resourceModeIndex > 0 ? resourceModeIndex - 1 : 0;
            }
        }

        private void DrawLoadAssetInfo(TaskInfo loadAssetInfo)
        {
            EditorGUILayout.LabelField(loadAssetInfo.Description,
                Utility.Text.Format("[SerialId]{0} [Priority]{1} [Status]{2}", loadAssetInfo.SerialId,
                    loadAssetInfo.Priority, loadAssetInfo.Status));
        }
    }
}