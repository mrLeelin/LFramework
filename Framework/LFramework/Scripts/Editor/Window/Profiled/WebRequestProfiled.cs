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

            DrawMetricCards(
                new ProfiledMetric("Total Agents", _webRequestComponent.TotalAgentCount.ToString(), "All agents"),
                new ProfiledMetric("Free Agents", _webRequestComponent.FreeAgentCount.ToString(), "Idle agents"),
                new ProfiledMetric("Working Agents", _webRequestComponent.WorkingAgentCount.ToString(), "Active agents"),
                new ProfiledMetric("Waiting Tasks", _webRequestComponent.WaitingTaskCount.ToString(), "Queued tasks"));

            TaskInfo[] webRequestInfos = _webRequestComponent.GetAllWebRequestInfos();
            DrawSection(
                "Task Queue",
                "Current web request tasks, including serial id, tag, priority, and state. Export remains available for offline analysis.",
                () =>
                {
                    if (webRequestInfos == null || webRequestInfos.Length == 0)
                    {
                        DrawKeyValueRow("Tasks", "Empty");
                        return;
                    }

                    foreach (TaskInfo webRequestInfo in webRequestInfos)
                    {
                        DrawKeyValueRow(
                            webRequestInfo.Description,
                            Utility.Text.Format(
                                "Serial {0}  Tag {1}  Priority {2}  Status {3}",
                                webRequestInfo.SerialId,
                                webRequestInfo.Tag ?? "<None>",
                                webRequestInfo.Priority,
                                webRequestInfo.Status));
                    }

                    if (GUILayout.Button("Export CSV Data"))
                    {
                        ExportCsv(webRequestInfos);
                    }
                });
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
                    data[index++] = Utility.Text.Format(
                        "{0},{1},{2},{3},{4}",
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
                Debug.LogError(Utility.Text.Format(
                    "Export web request task CSV data to '{0}' failure, exception is '{1}'.",
                    exportFileName,
                    exception));
            }
        }
    }
}
