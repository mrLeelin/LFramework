using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(TableComponentSetting))]
    internal class TableComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnableLoadDataTableUpdateEvent = null;
        private SerializedProperty m_EnableLoadDataTableDependencyAssetEvent = null;
        private SerializedProperty m_CachedBytesSize = null;
        
        private HelperInfo<TableHelperBase>
            m_DataTableHelperInfo = new HelperInfo<TableHelperBase>("DataTable");
        
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_EnableLoadDataTableUpdateEvent);
                EditorGUILayout.PropertyField(m_EnableLoadDataTableDependencyAssetEvent);
                m_DataTableHelperInfo.Draw();
                EditorGUILayout.PropertyField(m_CachedBytesSize);
            }
            EditorGUI.EndDisabledGroup();
            
            serializedObject.ApplyModifiedProperties();

            Repaint();
        }
        
        
        protected override void OnEnable()
        {
            m_EnableLoadDataTableUpdateEvent = serializedObject.FindProperty("m_EnableLoadDataTableUpdateEvent");
            m_EnableLoadDataTableDependencyAssetEvent =
                serializedObject.FindProperty("m_EnableLoadDataTableDependencyAssetEvent");
            m_CachedBytesSize = serializedObject.FindProperty("m_CachedBytesSize");

            m_DataTableHelperInfo.Init(serializedObject);
            
            RefreshTypeNames();
        }
        
        private void RefreshTypeNames()
        {
            m_DataTableHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
        
    }
}

