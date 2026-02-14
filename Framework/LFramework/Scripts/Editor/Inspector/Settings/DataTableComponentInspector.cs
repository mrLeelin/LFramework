using System;
using GameFramework;
using GameFramework.DataTable;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;
using Type = System.Type;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(DataTableComponentSetting))]
    internal sealed class DataTableComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty _tableFullName = null;

        private object tableObject;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            _tableFullName.stringValue = EditorGUILayout.TextField("名字", _tableFullName.stringValue);
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
            _tableFullName = serializedObject.FindProperty("tableFullName");
            RefreshTypeNames();
        }


        private void RefreshTypeNames()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}