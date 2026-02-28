using System.Collections.Generic;
using System.Linq;
using GameFramework.Procedure;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ProcedureComponentSetting))]
    internal sealed class ProcedureComponentInspector : ComponentSettingInspector
    {
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
                GUILayout.Label("Available Procedures", EditorStyles.boldLabel);
                if (m_ProcedureTypeNames.Length > 0)
                {
                    EditorGUILayout.BeginVertical("box");
                    {
                        foreach (string procedureTypeName in m_ProcedureTypeNames)
                        {
                            bool selected = m_CurrentAvailableProcedureTypeNames.Contains(procedureTypeName);
                            if (selected != EditorGUILayout.ToggleLeft(procedureTypeName, selected))
                            {
                                if (!selected)
                                {
                                    m_CurrentAvailableProcedureTypeNames.Add(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                                else if (procedureTypeName != m_EntranceProcedureTypeName.stringValue)
                                {
                                    m_CurrentAvailableProcedureTypeNames.Remove(procedureTypeName);
                                    WriteAvailableProcedureTypeNames();
                                }
                            }
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                else
                {
                    EditorGUILayout.HelpBox("There is no available procedure.", MessageType.Warning);
                }

                if (m_CurrentAvailableProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    int selectedIndex = EditorGUILayout.Popup("Entrance Procedure", m_EntranceProcedureIndex,
                        m_CurrentAvailableProcedureTypeNames.ToArray());
                    if (selectedIndex != m_EntranceProcedureIndex)
                    {
                        m_EntranceProcedureIndex = selectedIndex;
                        m_EntranceProcedureTypeName.stringValue = m_CurrentAvailableProcedureTypeNames[selectedIndex];
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Select available procedures first.", MessageType.Info);
                }

                if (m_CurrentAvailableHotfixProcedureTypeNames != null && m_CurrentAvailableHotfixProcedureTypeNames.Count > 0)
                {
                    EditorGUILayout.Separator();

                    int selectedIndex = EditorGUILayout.Popup("Hotfix Entrance Procedure",
                        m_HotfixEntranceProcedureIndex, m_CurrentAvailableHotfixProcedureTypeNames.ToArray());
                    if (selectedIndex != m_HotfixEntranceProcedureIndex)
                    {
                        m_HotfixEntranceProcedureIndex = selectedIndex;
                        m_HotfixEntranceProcedureTypeName.stringValue = m_CurrentAvailableHotfixProcedureTypeNames[selectedIndex];
                    }
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
            m_HotfixProcedureTypeNames = Type.GetTypeNames(typeof(ProcedureBase),
                _onceGameSetting.hotfixAssembliesSort.ToArray());
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
            }else if (!string.IsNullOrEmpty(m_HotfixEntranceProcedureTypeName.stringValue))
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
            
            if (m_CurrentAvailableHotfixProcedureTypeNames != null &&!string.IsNullOrEmpty(m_HotfixEntranceProcedureTypeName.stringValue))
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
    }
}