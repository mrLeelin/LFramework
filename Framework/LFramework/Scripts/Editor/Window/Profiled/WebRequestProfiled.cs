using System;
using System.IO;
using System.Text;
using GameFramework;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class WebRequestProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private WebRequestComponent _webRequestComponent;

        internal override void Draw()
        {
            GetComponent(ref _webRequestComponent);
            if (_webRequestComponent == null)
            {
                EditorGUILayout.HelpBox("WebRequest component is unavailable.", MessageType.Warning);
                return;
            }

            TaskInfo[] webRequestInfos = _webRequestComponent.GetAllWebRequestInfos() ?? Array.Empty<TaskInfo>();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Overview", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Total Agent Count", _webRequestComponent.TotalAgentCount.ToString());
                EditorGUILayout.LabelField("Free Agent Count", _webRequestComponent.FreeAgentCount.ToString());
                EditorGUILayout.LabelField("Working Agent Count", _webRequestComponent.WorkingAgentCount.ToString());
                EditorGUILayout.LabelField("Waiting Agent Count", _webRequestComponent.WaitingTaskCount.ToString());
                EditorGUILayout.LabelField("Task Count", webRequestInfos.Length.ToString());
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField("Task List", EditorStyles.boldLabel);
                if (webRequestInfos.Length == 0)
                {
                    EditorGUILayout.HelpBox("No active web request tasks.", MessageType.Info);
                }
                else
                {
                    foreach (TaskInfo webRequestInfo in webRequestInfos)
                    {
                        DrawWebRequestInfo(webRequestInfo);
                    }
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button($"Export CSV Data ({webRequestInfos.Length})", GUILayout.Width(220f)))
            {
                ExportCsv(webRequestInfos);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawWebRequestInfo(TaskInfo webRequestInfo)
        {
            string description = string.IsNullOrEmpty(webRequestInfo.Description) ? "<No Description>" : webRequestInfo.Description;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(description, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                Utility.Text.Format("SerialId: {0}    Tag: {1}    Priority: {2}    Status: {3}",
                    webRequestInfo.SerialId,
                    webRequestInfo.Tag ?? "<None>",
                    webRequestInfo.Priority,
                    webRequestInfo.Status));
            EditorGUILayout.EndVertical();
        }

        private static void ExportCsv(TaskInfo[] webRequestInfos)
        {
            string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty, "WebRequest Task Data.csv", string.Empty);
            if (string.IsNullOrEmpty(exportFileName))
            {
                return;
            }

            try
            {
                int index = 0;
                string[] data = new string[webRequestInfos.Length + 1];
                data[index++] = "WebRequest Uri,Serial Id,Tag,Priority,Status";
                foreach (TaskInfo webRequestInfo in webRequestInfos)
                {
                    data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4}",
                        webRequestInfo.Description,
                        webRequestInfo.SerialId,
                        webRequestInfo.Tag ?? string.Empty,
                        webRequestInfo.Priority,
                        webRequestInfo.Status);
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                Debug.Log(Utility.Text.Format("Export web request task CSV data to '{0}' success.", exportFileName));
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Export web request task CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
            }
        }
    }
}
