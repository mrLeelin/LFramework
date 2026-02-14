using System;
using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor
{
    [CustomEditor(typeof(VariableAutoBindTool))]
    public class VariableAutoBindToolInspector : UnityEditor.Editor
    {
        private VariableAutoBindTool _target;
        private SerializedProperty _variableArray;
        
        private void OnEnable()
        {
            _target = target as VariableAutoBindTool;
            _variableArray = serializedObject.FindProperty("variableArray");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawTopButton();
            
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();


        }

        private void DrawTopButton()
        {
            /*
            if (GUILayout.Button("删除空引用"))
            {
                RemoveNull();
            }
            */
        }

        private void RemoveNull()
        {
         

        }
        
    }

}
