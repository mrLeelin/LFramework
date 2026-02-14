using System.Collections;
using System.Collections.Generic;
using LFramework.Editor.Inspector;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    [CustomEditor(typeof(GameNotificationsComponentSetting))]
    public class GameNotificationsComponentSettingInspector : ComponentSettingInspector
    {
        private SerializedProperty _modelSerializedProperty;
        private SerializedProperty _autoBadgingSerializedProperty;


        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            this.DrawDefaultInspector();
            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            _modelSerializedProperty = serializedObject.FindProperty("model");
            _autoBadgingSerializedProperty = serializedObject.FindProperty("autoBadging");
        }
    }
}