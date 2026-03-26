using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using GameFramework;
using LFramework.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ReferencePoolProfiled : ProfiledBase
    {
        private readonly Dictionary<string, List<ReferencePoolInfo>> m_ReferencePoolInfos =
            new Dictionary<string, List<ReferencePoolInfo>>(StringComparer.Ordinal);

        private readonly HashSet<string> m_OpenedItems = new HashSet<string>();
        private bool m_ShowFullClassName;

        internal override bool CanDraw { get; } = true;

        internal override void Draw()
        {
            GameWindowChrome.DrawCompactHeader("Overview", "Inspect pooled references by assembly and export the current snapshot.");
            EditorGUILayout.LabelField("Reference Pool Count", ReferencePool.Count.ToString());
            m_ShowFullClassName = EditorGUILayout.Toggle("Show Full Class Name", m_ShowFullClassName);

            m_ReferencePoolInfos.Clear();
            ReferencePoolInfo[] referencePoolInfos = ReferencePool.GetAllReferencePoolInfos();
            if (referencePoolInfos == null || referencePoolInfos.Length == 0)
            {
                EditorGUILayout.HelpBox("ReferencePool does not currently contain any cached types.", MessageType.Info);
                return;
            }

            foreach (ReferencePoolInfo referencePoolInfo in referencePoolInfos)
            {
                string assemblyName = referencePoolInfo.Type.Assembly.GetName().Name;
                if (!m_ReferencePoolInfos.TryGetValue(assemblyName, out List<ReferencePoolInfo> results))
                {
                    results = new List<ReferencePoolInfo>();
                    m_ReferencePoolInfos.Add(assemblyName, results);
                }

                results.Add(referencePoolInfo);
            }

            GUILayout.Space(6f);
            GameWindowChrome.DrawCompactHeader("Assemblies", "Expand an assembly to inspect pooled type activity.");
            foreach (string assemblyName in m_ReferencePoolInfos.Keys.OrderBy(item => item, StringComparer.Ordinal))
            {
                List<ReferencePoolInfo> assemblyReferencePoolInfos = m_ReferencePoolInfos[assemblyName];
                bool lastState = m_OpenedItems.Contains(assemblyName);
                bool currentState = EditorGUILayout.Foldout(lastState, Utility.Text.Format("{0}  ({1})", assemblyName, assemblyReferencePoolInfos.Count), true);
                if (currentState != lastState)
                {
                    if (currentState)
                    {
                        m_OpenedItems.Add(assemblyName);
                    }
                    else
                    {
                        m_OpenedItems.Remove(assemblyName);
                    }
                }

                if (!currentState)
                {
                    continue;
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Display Name", m_ShowFullClassName ? "Full Class Name" : "Class Name");
                assemblyReferencePoolInfos.Sort(Comparison);
                foreach (ReferencePoolInfo referencePoolInfo in assemblyReferencePoolInfos)
                {
                    DrawReferencePoolInfo(referencePoolInfo);
                }

                GUILayout.Space(4f);
                if (GUILayout.Button("Export CSV Data"))
                {
                    ExportCsv(assemblyName, assemblyReferencePoolInfos);
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(4f);
            }
        }

        private void DrawReferencePoolInfo(ReferencePoolInfo referencePoolInfo)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                m_ShowFullClassName ? referencePoolInfo.Type.FullName : referencePoolInfo.Type.Name,
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                Utility.Text.Format(
                    "Unused {0} | Using {1} | Acquire {2} | Release {3} | Add {4} | Remove {5}",
                    referencePoolInfo.UnusedReferenceCount,
                    referencePoolInfo.UsingReferenceCount,
                    referencePoolInfo.AcquireReferenceCount,
                    referencePoolInfo.ReleaseReferenceCount,
                    referencePoolInfo.AddReferenceCount,
                    referencePoolInfo.RemoveReferenceCount),
                EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private static void ExportCsv(string assemblyName, List<ReferencePoolInfo> assemblyReferencePoolInfos)
        {
            string exportFileName = EditorUtility.SaveFilePanel(
                "Export CSV Data",
                string.Empty,
                Utility.Text.Format("Reference Pool Data - {0}.csv", assemblyName),
                string.Empty);
            if (string.IsNullOrEmpty(exportFileName))
            {
                return;
            }

            try
            {
                int index = 0;
                string[] data = new string[assemblyReferencePoolInfos.Count + 1];
                data[index++] = "Class Name,Full Class Name,Unused,Using,Acquire,Release,Add,Remove";
                foreach (ReferencePoolInfo referencePoolInfo in assemblyReferencePoolInfos)
                {
                    data[index++] = Utility.Text.Format(
                        "{0},{1},{2},{3},{4},{5},{6},{7}",
                        referencePoolInfo.Type.Name,
                        referencePoolInfo.Type.FullName,
                        referencePoolInfo.UnusedReferenceCount,
                        referencePoolInfo.UsingReferenceCount,
                        referencePoolInfo.AcquireReferenceCount,
                        referencePoolInfo.ReleaseReferenceCount,
                        referencePoolInfo.AddReferenceCount,
                        referencePoolInfo.RemoveReferenceCount);
                }

                File.WriteAllLines(exportFileName, data, Encoding.UTF8);
                Debug.Log(Utility.Text.Format("Export reference pool CSV data to '{0}' success.", exportFileName));
            }
            catch (Exception exception)
            {
                Debug.LogError(Utility.Text.Format("Export reference pool CSV data to '{0}' failure, exception is '{1}'.", exportFileName, exception));
            }
        }

        private int Comparison(ReferencePoolInfo a, ReferencePoolInfo b)
        {
            return m_ShowFullClassName
                ? a.Type.FullName.CompareTo(b.Type.FullName)
                : a.Type.Name.CompareTo(b.Type.Name);
        }
    }
}
