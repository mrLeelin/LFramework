using System;
using System.IO;
using System.Text;
using GameFramework;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class DownloadProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private DownloadComponent _downloadComponent;

        internal override void Draw()
        {
            GetComponent(ref _downloadComponent);
            if (_downloadComponent == null)
            {
                EditorGUILayout.HelpBox("Download component is unavailable.", MessageType.Warning);
                return;
            }

            TaskInfo[] downloadInfos = _downloadComponent.GetAllDownloadInfos() ?? Array.Empty<TaskInfo>();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Paused", _downloadComponent.Paused.ToString());
                EditorGUILayout.LabelField("Current Speed", _downloadComponent.CurrentSpeed.ToString());
                EditorGUILayout.LabelField("Total Agent Count", _downloadComponent.TotalAgentCount.ToString());
                EditorGUILayout.LabelField("Free Agent Count", _downloadComponent.FreeAgentCount.ToString());
                EditorGUILayout.LabelField("Working Agent Count", _downloadComponent.WorkingAgentCount.ToString());
                EditorGUILayout.LabelField("Waiting Agent Count", _downloadComponent.WaitingTaskCount.ToString());
                EditorGUILayout.LabelField("Task Count", downloadInfos.Length.ToString());
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Task List", EditorStyles.boldLabel);
                if (downloadInfos.Length == 0)
                {
                    EditorGUILayout.HelpBox("No active download tasks.", MessageType.Info);
                }
                else
                {
                    foreach (TaskInfo downloadInfo in downloadInfos)
                    {
                        DrawDownloadInfo(downloadInfo);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button($"Export CSV Data ({downloadInfos.Length})", GUILayout.Width(220f)))
            {
                ExportCsv(downloadInfos);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDownloadInfo(TaskInfo downloadInfo)
        {
            string description = string.IsNullOrEmpty(downloadInfo.Description) ? "<No Description>" : downloadInfo.Description;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(description, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                Utility.Text.Format("SerialId: {0}    Tag: {1}    Priority: {2}    Status: {3}",
                    downloadInfo.SerialId,
                    downloadInfo.Tag ?? "<None>",
                    downloadInfo.Priority,
                    downloadInfo.Status));
            EditorGUILayout.EndVertical();
        }

        private static void ExportCsv(TaskInfo[] downloadInfos)
        {
            string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, "Download Task Data.csv", string.Empty);
            if (string.IsNullOrEmpty(exportFileName))
            {
                return;
            }

            try
            {
                int index = 0;
                string[] data = new string[downloadInfos.Length + 1];
                data[index++] = "Download Path,Serial Id,Tag,Priority,Status";
                foreach (TaskInfo downloadInfo in downloadInfos)
                {
                    data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4}",
                        downloadInfo.Description,
                        downloadInfo.SerialId,
                        downloadInfo.Tag ?? string.Empty,
                        downloadInfo.Priority,
                        downloadInfo.Status);
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                Debug.Log(Utility.Text.Format("Export download task CSV data to '{0}' success.", exportFileName));
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Export download task CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
            }
        }
    }
}
