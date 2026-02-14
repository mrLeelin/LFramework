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


            EditorGUILayout.LabelField("Paused", _downloadComponent.Paused.ToString());
            EditorGUILayout.LabelField("Total Agent Count", _downloadComponent.TotalAgentCount.ToString());
            EditorGUILayout.LabelField("Free Agent Count", _downloadComponent.FreeAgentCount.ToString());
            EditorGUILayout.LabelField("Working Agent Count", _downloadComponent.WorkingAgentCount.ToString());
            EditorGUILayout.LabelField("Waiting Agent Count", _downloadComponent.WaitingTaskCount.ToString());
            EditorGUILayout.LabelField("Current Speed", _downloadComponent.CurrentSpeed.ToString());
            EditorGUILayout.BeginVertical("box");
            {
                TaskInfo[] downloadInfos = _downloadComponent.GetAllDownloadInfos();
                if (downloadInfos.Length > 0)
                {
                    foreach (TaskInfo downloadInfo in downloadInfos)
                    {
                        DrawDownloadInfo(downloadInfo);
                    }

                    if (GUILayout.Button("Export CSV Data"))
                    {
                        string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty,
                            "Download Task Data.csv", string.Empty);
                        if (!string.IsNullOrEmpty(exportFileName))
                        {
                            try
                            {
                                int index = 0;
                                string[] data = new string[downloadInfos.Length + 1];
                                data[index++] = "Download Path,Serial Id,Tag,Priority,Status";
                                foreach (TaskInfo downloadInfo in downloadInfos)
                                {
                                    data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4}",
                                        downloadInfo.Description, downloadInfo.SerialId,
                                        downloadInfo.Tag ?? string.Empty, downloadInfo.Priority,
                                        downloadInfo.Status);
                                }

                                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                Debug.Log(Utility.Text.Format("Export download task CSV data to '{0}' success.",
                                    exportFileName));
                            }
                            catch (Exception exception)
                            {
                                Debug.LogError(Utility.Text.Format(
                                    "Export download task CSV data to '{0}' failure, exception is '{1}'.",
                                    exportFileName, exception));
                            }
                        }
                    }
                }
                else
                {
                    GUILayout.Label("Download Task is Empty ...");
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDownloadInfo(TaskInfo downloadInfo)
        {
            EditorGUILayout.LabelField(downloadInfo.Description,
                Utility.Text.Format("[SerialId]{0} [Tag]{1} [Priority]{2} [Status]{3}", downloadInfo.SerialId,
                    downloadInfo.Tag ?? "<None>", downloadInfo.Priority, downloadInfo.Status));
        }

    }
}