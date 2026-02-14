using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Inspector
{
    
    [CustomEditor(typeof(SpriteCollectionComponentSetting))]
    public class SpriteCollectionComponentInspector : ComponentSettingInspector
    {

        private SerializedProperty m_CheckCanReleaseInterval;
        private SerializedProperty m_AutoReleaseInterval;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_AutoReleaseInterval = serializedObject.FindProperty("m_AutoReleaseInterval");
            m_CheckCanReleaseInterval = serializedObject.FindProperty("m_CheckCanReleaseInterval");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_CheckCanReleaseInterval);
            EditorGUILayout.PropertyField(m_AutoReleaseInterval);
            serializedObject.ApplyModifiedProperties();

            Repaint();
        }
    }

}
