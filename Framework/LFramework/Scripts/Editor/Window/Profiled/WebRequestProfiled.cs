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
            
            
             EditorGUILayout.LabelField("Total Agent Count", _webRequestComponent.TotalAgentCount.ToString());
                EditorGUILayout.LabelField("Free Agent Count", _webRequestComponent.FreeAgentCount.ToString());
                EditorGUILayout.LabelField("Working Agent Count", _webRequestComponent.WorkingAgentCount.ToString());
                EditorGUILayout.LabelField("Waiting Agent Count", _webRequestComponent.WaitingTaskCount.ToString());
                EditorGUILayout.BeginVertical("box");
                {
                    TaskInfo[] webRequestInfos = _webRequestComponent.GetAllWebRequestInfos();
                    if (webRequestInfos.Length > 0)
                    {
                        foreach (TaskInfo webRequestInfo in webRequestInfos)
                        {
                            DrawWebRequestInfo(webRequestInfo);
                        }

                        if (GUILayout.Button("Export CSV Data"))
                        {
                            string exportFileName = EditorUtility.SaveFilePanel("Export CSV Data", string.Empty,
                                "WebRequest Task Data.csv", string.Empty);
                            if (!string.IsNullOrEmpty(exportFileName))
                            {
                                try
                                {
                                    int index = 0;
                                    string[] data = new string[webRequestInfos.Length + 1];
                                    data[index++] = "WebRequest Uri,Serial Id,Tag,Priority,Status";
                                    foreach (TaskInfo webRequestInfo in webRequestInfos)
                                    {
                                        data[index++] = Utility.Text.Format("{0},{1},{2},{3},{4}",
                                            webRequestInfo.Description, webRequestInfo.SerialId,
                                            webRequestInfo.Tag ?? string.Empty, webRequestInfo.Priority,
                                            webRequestInfo.Status);
                                    }

                                    File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                                    Debug.Log(Utility.Text.Format("Export web request task CSV data to '{0}' success.",
                                        exportFileName));
                                }
                                catch (Exception exception)
                                {
                                    Debug.LogError(Utility.Text.Format(
                                        "Export web request task CSV data to '{0}' failure, exception is '{1}'.",
                                        exportFileName, exception));
                                }
                            }
                        }
                    }
                    else
                    {
                        GUILayout.Label("WebRequset Task is Empty ...");
                    }
                }
                EditorGUILayout.EndVertical();
        }
        
        private void DrawWebRequestInfo(TaskInfo webRequestInfo)
        {
            EditorGUILayout.LabelField(webRequestInfo.Description,
                Utility.Text.Format("[SerialId]{0} [Tag]{1} [Priority]{2} [Status]{3}", webRequestInfo.SerialId,
                    webRequestInfo.Tag ?? "<None>", webRequestInfo.Priority, webRequestInfo.Status));
        }

    }
}