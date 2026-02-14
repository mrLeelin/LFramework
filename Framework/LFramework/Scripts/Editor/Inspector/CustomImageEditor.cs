using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace LFramework.Editor
{
    [CustomEditor(typeof(CustomImage),true)]
    [CanEditMultipleObjects]
    public class CustomImageEditor : ImageEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            // 获取目标对象
            var resourceReference = (CustomImage)target;
            if (resourceReference)
            {
                // 显示资源引用
                EditorGUILayout.LabelField("==============Custom Image 自定义=============", EditorStyles.boldLabel);
                // 显示资源路径
                resourceReference.keepNativeSize =  EditorGUILayout.Toggle("KeepNativeSize", resourceReference.keepNativeSize);
                resourceReference.preserveAspect =   EditorGUILayout.Toggle("PreserveAspect", resourceReference.preserveAspect);
            }
            
            // 保存修改
            if (GUI.changed)
            {
                EditorUtility.SetDirty(target);
            }
        }
    }

}
