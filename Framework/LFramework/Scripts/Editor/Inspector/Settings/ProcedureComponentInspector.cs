using System;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using GameFramework.Procedure;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ProcedureComponentSetting))]
    internal sealed class ProcedureComponentInspector : ComponentSettingInspector
    {
        private const string SessionStateKeyPrefix = "ProcedureComponentInspector.AssemblyFoldout.";

        private SerializedProperty m_AvailableProcedureTypeNames = null;
        private SerializedProperty m_EntranceProcedureTypeName = null;
        private SerializedProperty m_HotfixEntranceProcedureTypeName = null;

        private string[] m_ProcedureTypeNames = null;
        private string[] m_HotfixProcedureTypeNames = null;

        private List<string> m_CurrentAvailableProcedureTypeNames = null;
        private List<string> m_CurrentAvailableHotfixProcedureTypeNames = null;

        private int m_EntranceProcedureIndex = -1;
        private int m_HotfixEntranceProcedureIndex = -1;
        private HybridCLRSetting _onceGameSetting;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            if (string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                EditorGUILayout.HelpBox("Entrance procedure is invalid.", MessageType.Error);
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawSectionHeader("Available Procedures", "Procedures discovered across runtime assemblies.");

                if (m_ProcedureTypeNames.Length > 0)
                {
                    var procedureGroups = GroupProceduresByAssembly(m_ProcedureTypeNames);
                    var sortedAssemblies = SortAssemblyNames(procedureGroups.Keys);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    {
                        EditorGUILayout.LabelField(
                            $"{m_ProcedureTypeNames.Length} procedures across {procedureGroups.Count} assemblies.",
                            EditorStyles.wordWrappedMiniLabel);
                        GUILayout.Space(6f);
                        DrawProceduresByAssembly(procedureGroups, sortedAssemblies, "Runtime");
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no available procedure.", MessageType.Warning);
                }

                GUILayout.Space(12f);
                DrawSelectionCard(
                    "Entrance Procedure",
                    "Select the procedure that runs first when the framework starts.",
                    m_CurrentAvailableProcedureTypeNames,
                    ref m_EntranceProcedureIndex,
                    m_EntranceProcedureTypeName,
                    "Select available procedures first.");

                if (m_CurrentAvailableHotfixProcedureTypeNames != null)
                {
                    GUILayout.Space(12f);
                    DrawSelectionCard(
                        "Hotfix Entrance Procedure",
                        "Pick the hotfix entry point when hotfix assemblies are enabled.",
                        m_CurrentAvailableHotfixProcedureTypeNames,
                        ref m_HotfixEntranceProcedureIndex,
                        m_HotfixEntranceProcedureTypeName,
                        "Enable hotfix assemblies to populate this list.");
                }
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
            m_AvailableProcedureTypeNames = serializedObject.FindProperty("m_AvailableProcedureTypeNames");
            m_EntranceProcedureTypeName = serializedObject.FindProperty("m_EntranceProcedureTypeName");
            m_HotfixEntranceProcedureTypeName = serializedObject.FindProperty("m_EntranceHotfixProcedureTypeName");
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            RefreshGameSetting();
            m_ProcedureTypeNames = Type.GetRuntimeTypeNames(typeof(ProcedureBase));
            m_HotfixProcedureTypeNames =
                Type.GetTypeNames(typeof(ProcedureBase), _onceGameSetting.hotfixAssembliesSort.ToArray());
            ReadAvailableProcedureTypeNames();
            int oldCount = m_CurrentAvailableProcedureTypeNames.Count;
            m_CurrentAvailableProcedureTypeNames = m_CurrentAvailableProcedureTypeNames
                .Where(x => m_ProcedureTypeNames.Contains(x)).ToList();
            if (m_CurrentAvailableProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex =
                    m_CurrentAvailableProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0)
                {
                    m_EntranceProcedureTypeName.stringValue = null;
                }
            }

            oldCount = m_CurrentAvailableHotfixProcedureTypeNames?.Count ?? 0;
            m_CurrentAvailableHotfixProcedureTypeNames = m_HotfixProcedureTypeNames.ToList();
            if (m_CurrentAvailableHotfixProcedureTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(m_HotfixEntranceProcedureTypeName.stringValue))
            {
                m_HotfixEntranceProcedureIndex =
                    m_CurrentAvailableHotfixProcedureTypeNames.IndexOf(m_HotfixEntranceProcedureTypeName.stringValue);
                if (m_HotfixEntranceProcedureIndex < 0)
                {
                    m_HotfixEntranceProcedureTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ReadAvailableProcedureTypeNames()
        {
            m_CurrentAvailableProcedureTypeNames = new List<string>();
            int count = m_AvailableProcedureTypeNames.arraySize;
            for (int i = 0; i < count; i++)
            {
                m_CurrentAvailableProcedureTypeNames.Add(m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i)
                    .stringValue);
            }
        }

        private void WriteAvailableProcedureTypeNames()
        {
            m_AvailableProcedureTypeNames.ClearArray();
            if (m_CurrentAvailableProcedureTypeNames == null)
            {
                return;
            }

            m_CurrentAvailableProcedureTypeNames.Sort();
            int count = m_CurrentAvailableProcedureTypeNames.Count;
            for (int i = 0; i < count; i++)
            {
                m_AvailableProcedureTypeNames.InsertArrayElementAtIndex(i);
                m_AvailableProcedureTypeNames.GetArrayElementAtIndex(i).stringValue =
                    m_CurrentAvailableProcedureTypeNames[i];
            }

            if (!string.IsNullOrEmpty(m_EntranceProcedureTypeName.stringValue))
            {
                m_EntranceProcedureIndex =
                    m_CurrentAvailableProcedureTypeNames.IndexOf(m_EntranceProcedureTypeName.stringValue);
                if (m_EntranceProcedureIndex < 0)
                {
                    m_EntranceProcedureTypeName.stringValue = null;
                }
            }

            if (m_CurrentAvailableHotfixProcedureTypeNames != null &&
                !string.IsNullOrEmpty(m_HotfixEntranceProcedureTypeName.stringValue))
            {
                m_HotfixEntranceProcedureIndex =
                    m_CurrentAvailableHotfixProcedureTypeNames.IndexOf(m_HotfixEntranceProcedureTypeName.stringValue);
                if (m_HotfixEntranceProcedureIndex < 0)
                {
                    m_HotfixEntranceProcedureTypeName.stringValue = null;
                }
            }
        }

        private void RefreshGameSetting()
        {
            var gameSetting = SettingManager.GetSetting<HybridCLRSetting>();
            if (gameSetting == null)
            {
                Debug.LogWarning("[ProcedureComponentInspector] GameSetting not found!");
                return;
            }

            _onceGameSetting = gameSetting;
        }

        /// <summary>
        /// 按程序集分组 procedure 类型名称
        /// </summary>
        private Dictionary<string, List<string>> GroupProceduresByAssembly(string[] typeNames)
        {
            var groups = new Dictionary<string, List<string>>();

            foreach (string typeName in typeNames)
            {
                string assemblyName = GetAssemblyName(typeName);

                if (!groups.ContainsKey(assemblyName))
                {
                    groups[assemblyName] = new List<string>();
                }

                groups[assemblyName].Add(typeName);
            }

            return groups;
        }

        /// <summary>
        /// 获取类型的程序集名称
        /// </summary>
        private string GetAssemblyName(string typeName)
        {
            try
            {
                System.Type type = Utility.Assembly.GetType(typeName);
                if (type != null)
                {
                    return type.Assembly.GetName().Name;
                }
            }
            catch
            {
                // 类型解析失败
            }

            return "Unknown Assembly";
        }

        /// <summary>
        /// 对程序集名称进行排序（Runtime 程序集优先）
        /// </summary>
        private List<string> SortAssemblyNames(IEnumerable<string> assemblyNames)
        {
            var sorted = assemblyNames.OrderBy(name =>
            {
                // Runtime 程序集优先级最高
                if (name.Contains("Runtime"))
                    return 0;
                // Unknown Assembly 优先级最低
                if (name == "Unknown Assembly")
                    return 2;
                // 其他程序集
                return 1;
            }).ThenBy(name => name).ToList();

            return sorted;
        }

        /// <summary>
        /// 绘制按程序集分组的 procedures
        /// </summary>
        private void DrawProceduresByAssembly(Dictionary<string, List<string>> groups, List<string> sortedAssemblies,
            string category)
        {
            foreach (string assemblyName in sortedAssemblies)
            {
                string sessionKey = SessionStateKeyPrefix + category + "." + assemblyName;
                bool foldout = SessionState.GetBool(sessionKey, true);

                var procedures = groups[assemblyName];
                string label = $"{assemblyName} ({procedures.Count})";
                bool newFoldout = EditorGUILayout.Foldout(foldout, label, true);
                if (newFoldout != foldout)
                {
                    SessionState.SetBool(sessionKey, newFoldout);
                }

                if (!newFoldout)
                {
                    continue;
                }

                EditorGUI.indentLevel++;

                foreach (string procedureTypeName in procedures)
                {
                    bool selected = m_CurrentAvailableProcedureTypeNames.Contains(procedureTypeName);
                    bool toggled = EditorGUILayout.ToggleLeft(procedureTypeName, selected);
                    if (toggled != selected)
                    {
                        if (toggled)
                        {
                            m_CurrentAvailableProcedureTypeNames.Add(procedureTypeName);
                        }
                        else if (procedureTypeName != m_EntranceProcedureTypeName.stringValue)
                        {
                            m_CurrentAvailableProcedureTypeNames.Remove(procedureTypeName);
                        }

                        WriteAvailableProcedureTypeNames();
                    }
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawSelectionCard(string title, string description, List<string> options, ref int index,
            SerializedProperty property, string emptyMessage)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(6f);

                if (options == null || options.Count == 0)
                {
                    EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
                }
                else
                {
                    int clampedIndex = Mathf.Clamp(index, 0, options.Count - 1);
                    int selectedIndex = EditorGUILayout.Popup(string.Empty, clampedIndex, options.ToArray());
                    if (selectedIndex != index)
                    {
                        index = selectedIndex;
                        property.stringValue = options[selectedIndex];
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawSectionHeader(string title, string subtitle)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            if (!string.IsNullOrEmpty(subtitle))
            {
                EditorGUILayout.LabelField(subtitle, EditorStyles.wordWrappedMiniLabel);
            }

            GUILayout.Space(6f);
        }
    }
}

