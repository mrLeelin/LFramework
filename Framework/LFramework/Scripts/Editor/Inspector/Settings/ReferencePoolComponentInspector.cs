
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GameFramework;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ReferencePoolComponentSetting))]
    internal sealed class ReferencePoolComponentInspector : ComponentSettingInspector
    {
    
        private SerializedProperty m_EnableStrictCheck = null;

       

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();


            if (EditorApplication.isPlaying)
            {
                
            }
            else
            {
                EditorGUILayout.PropertyField(m_EnableStrictCheck);
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnEnable()
        {
            m_EnableStrictCheck = serializedObject.FindProperty("m_EnableStrictCheck");
        }

     
    }
}