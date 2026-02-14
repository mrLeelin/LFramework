

/**

*********************************************************************
Author:              LFramework.Editor
CreateTime:          19:53:31

*********************************************************************
**/

using System;
using System.IO;
using System.Reflection;
using System.Text;
using GameFramework;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ResourceComponentSetting))]
    internal sealed class ResourceComponentInspector : ComponentSettingInspector
    {
         private static readonly string[] ResourceModeNames = new string[] { "Package", "Updatable", "Updatable While Playing" ,"Addressable" };

        private SerializedProperty m_ResourceMode = null;
        private SerializedProperty m_ReadWritePathType = null;
        private SerializedProperty m_MinUnloadUnusedAssetsInterval = null;
        private SerializedProperty m_MaxUnloadUnusedAssetsInterval = null;
        private SerializedProperty m_AssetAutoReleaseInterval = null;
        private SerializedProperty m_AssetCapacity = null;
        private SerializedProperty m_AssetExpireTime = null;
        private SerializedProperty m_AssetPriority = null;
        private SerializedProperty m_ResourceAutoReleaseInterval = null;
        private SerializedProperty m_ResourceCapacity = null;
        private SerializedProperty m_ResourceExpireTime = null;
        private SerializedProperty m_ResourcePriority = null;
        private SerializedProperty m_UpdatePrefixUri = null;
        private SerializedProperty m_GenerateReadWriteVersionListLength = null;
        private SerializedProperty m_UpdateRetryCount = null;
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_LoadResourceAgentHelperCount = null;
        private FieldInfo m_EditorResourceModeFieldInfo = null;

        private int m_ResourceModeIndex = 0;
        private HelperInfo<ResourceHelperBase> m_ResourceHelperInfo = new HelperInfo<ResourceHelperBase>("Resource");
        private HelperInfo<LoadResourceAgentHelperBase> m_LoadResourceAgentHelperInfo = new HelperInfo<LoadResourceAgentHelperBase>("LoadResourceAgent");
        
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI(); 
        
            serializedObject.Update();
            var t = (ResourceComponentSetting)target;
            var component = GetComponent<ResourceComponent>();
            if (EditorApplication.isPlaying)
            {
                m_EditorResourceModeFieldInfo = component.GetType().GetField("m_EditorResourceMode", BindingFlags.NonPublic | BindingFlags.Instance);
            }

           
            bool isEditorResourceMode = m_EditorResourceModeFieldInfo != null && (bool)m_EditorResourceModeFieldInfo.GetValue(target);

            if (isEditorResourceMode)
            {
                EditorGUILayout.HelpBox("Editor resource mode is enabled. Some options are disabled.", MessageType.Warning);
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                if (EditorApplication.isPlaying )
                {
                    
                }
                else
                {
                    int selectedIndex = EditorGUILayout.Popup("Resource Mode", m_ResourceModeIndex, ResourceModeNames);
                    if (selectedIndex != m_ResourceModeIndex)
                    {
                        m_ResourceModeIndex = selectedIndex;
                        m_ResourceMode.enumValueIndex = selectedIndex + 1;
                    }
                }

                m_ReadWritePathType.enumValueIndex = (int)(ReadWritePathType)EditorGUILayout.EnumPopup("Read-Write Path Type", (ReadWritePathType)m_ReadWritePathType.enumValueIndex);
            }
            EditorGUI.EndDisabledGroup();

            float minUnloadUnusedAssetsInterval = EditorGUILayout.Slider("Min Unload Unused Assets Interval", m_MinUnloadUnusedAssetsInterval.floatValue, 0f, 3600f);
            if (minUnloadUnusedAssetsInterval != m_MinUnloadUnusedAssetsInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.MinUnloadUnusedAssetsInterval = minUnloadUnusedAssetsInterval;
                }
                else
                {
                    m_MinUnloadUnusedAssetsInterval.floatValue = minUnloadUnusedAssetsInterval;
                }
            }

            float maxUnloadUnusedAssetsInterval = EditorGUILayout.Slider("Max Unload Unused Assets Interval", m_MaxUnloadUnusedAssetsInterval.floatValue, 0f, 3600f);
            if (maxUnloadUnusedAssetsInterval != m_MaxUnloadUnusedAssetsInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.MaxUnloadUnusedAssetsInterval = maxUnloadUnusedAssetsInterval;
                }
                else
                {
                    m_MaxUnloadUnusedAssetsInterval.floatValue = maxUnloadUnusedAssetsInterval;
                }
            }

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && isEditorResourceMode);
            {
                float assetAutoReleaseInterval = EditorGUILayout.DelayedFloatField("Asset Auto Release Interval", m_AssetAutoReleaseInterval.floatValue);
                if (assetAutoReleaseInterval != m_AssetAutoReleaseInterval.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.AssetAutoReleaseInterval = assetAutoReleaseInterval;
                    }
                    else
                    {
                        m_AssetAutoReleaseInterval.floatValue = assetAutoReleaseInterval;
                    }
                }

                int assetCapacity = EditorGUILayout.DelayedIntField("Asset Capacity", m_AssetCapacity.intValue);
                if (assetCapacity != m_AssetCapacity.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.AssetCapacity = assetCapacity;
                    }
                    else
                    {
                        m_AssetCapacity.intValue = assetCapacity;
                    }
                }

                float assetExpireTime = EditorGUILayout.DelayedFloatField("Asset Expire Time", m_AssetExpireTime.floatValue);
                if (assetExpireTime != m_AssetExpireTime.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.AssetExpireTime = assetExpireTime;
                    } 
                    else
                    {
                        m_AssetExpireTime.floatValue = assetExpireTime;
                    }
                }

                int assetPriority = EditorGUILayout.DelayedIntField("Asset Priority", m_AssetPriority.intValue);
                if (assetPriority != m_AssetPriority.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.AssetPriority = assetPriority;
                    }
                    else
                    {
                        m_AssetPriority.intValue = assetPriority;
                    }
                }

                float resourceAutoReleaseInterval = EditorGUILayout.DelayedFloatField("Resource Auto Release Interval", m_ResourceAutoReleaseInterval.floatValue);
                if (resourceAutoReleaseInterval != m_ResourceAutoReleaseInterval.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.ResourceAutoReleaseInterval = resourceAutoReleaseInterval;
                    }
                    else
                    {
                        m_ResourceAutoReleaseInterval.floatValue = resourceAutoReleaseInterval;
                    }
                }

                int resourceCapacity = EditorGUILayout.DelayedIntField("Resource Capacity", m_ResourceCapacity.intValue);
                if (resourceCapacity != m_ResourceCapacity.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.ResourceCapacity = resourceCapacity;
                    }
                    else
                    {
                        m_ResourceCapacity.intValue = resourceCapacity;
                    }
                }

                float resourceExpireTime = EditorGUILayout.DelayedFloatField("Resource Expire Time", m_ResourceExpireTime.floatValue);
                if (resourceExpireTime != m_ResourceExpireTime.floatValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.ResourceExpireTime = resourceExpireTime;
                    }
                    else
                    {
                        m_ResourceExpireTime.floatValue = resourceExpireTime;
                    }
                }

                int resourcePriority = EditorGUILayout.DelayedIntField("Resource Priority", m_ResourcePriority.intValue);
                if (resourcePriority != m_ResourcePriority.intValue)
                {
                    if (EditorApplication.isPlaying)
                    {
                        component.ResourcePriority = resourcePriority;
                    }
                    else
                    {
                        m_ResourcePriority.intValue = resourcePriority;
                    }
                }

                if (m_ResourceModeIndex > 0)
                {
                    string updatePrefixUri = EditorGUILayout.DelayedTextField("Update Prefix Uri", m_UpdatePrefixUri.stringValue);
                    if (updatePrefixUri != m_UpdatePrefixUri.stringValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            component.UpdatePrefixUri = updatePrefixUri;
                        }
                        else
                        {
                            m_UpdatePrefixUri.stringValue = updatePrefixUri;
                        }
                    }

                    int generateReadWriteVersionListLength = EditorGUILayout.DelayedIntField("Generate Read-Write Version List Length", m_GenerateReadWriteVersionListLength.intValue);
                    if (generateReadWriteVersionListLength != m_GenerateReadWriteVersionListLength.intValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            component.GenerateReadWriteVersionListLength = generateReadWriteVersionListLength;
                        }
                        else
                        {
                            m_GenerateReadWriteVersionListLength.intValue = generateReadWriteVersionListLength;
                        }
                    }

                    int updateRetryCount = EditorGUILayout.DelayedIntField("Update Retry Count", m_UpdateRetryCount.intValue);
                    if (updateRetryCount != m_UpdateRetryCount.intValue)
                    {
                        if (EditorApplication.isPlaying)
                        {
                            component.UpdateRetryCount = updateRetryCount;
                        }
                        else
                        {
                            m_UpdateRetryCount.intValue = updateRetryCount;
                        }
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_InstanceRoot);

                m_ResourceHelperInfo.Draw();
                m_LoadResourceAgentHelperInfo.Draw();
                m_LoadResourceAgentHelperCount.intValue = EditorGUILayout.IntSlider("Load Resource Agent Helper Count", m_LoadResourceAgentHelperCount.intValue, 1, 128);
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
            m_ResourceMode = serializedObject.FindProperty("m_ResourceMode");
            m_ReadWritePathType = serializedObject.FindProperty("m_ReadWritePathType");
            m_MinUnloadUnusedAssetsInterval = serializedObject.FindProperty("m_MinUnloadUnusedAssetsInterval");
            m_MaxUnloadUnusedAssetsInterval = serializedObject.FindProperty("m_MaxUnloadUnusedAssetsInterval");
            m_AssetAutoReleaseInterval = serializedObject.FindProperty("m_AssetAutoReleaseInterval");
            m_AssetCapacity = serializedObject.FindProperty("m_AssetCapacity");
            m_AssetExpireTime = serializedObject.FindProperty("m_AssetExpireTime");
            m_AssetPriority = serializedObject.FindProperty("m_AssetPriority");
            m_ResourceAutoReleaseInterval = serializedObject.FindProperty("m_ResourceAutoReleaseInterval");
            m_ResourceCapacity = serializedObject.FindProperty("m_ResourceCapacity");
            m_ResourceExpireTime = serializedObject.FindProperty("m_ResourceExpireTime");
            m_ResourcePriority = serializedObject.FindProperty("m_ResourcePriority");
            m_UpdatePrefixUri = serializedObject.FindProperty("m_UpdatePrefixUri");
            m_GenerateReadWriteVersionListLength = serializedObject.FindProperty("m_GenerateReadWriteVersionListLength");
            m_UpdateRetryCount = serializedObject.FindProperty("m_UpdateRetryCount");
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_LoadResourceAgentHelperCount = serializedObject.FindProperty("m_LoadResourceAgentHelperCount");
          
            m_ResourceHelperInfo.Init(serializedObject);
            m_LoadResourceAgentHelperInfo.Init(serializedObject);

            RefreshModes();
            RefreshTypeNames();
        }

       
        private void RefreshModes()
        {
            m_ResourceModeIndex = m_ResourceMode.enumValueIndex > 0 ? m_ResourceMode.enumValueIndex - 1 : 0;
        }

        private void RefreshTypeNames()
        {
            m_ResourceHelperInfo.Refresh();
            m_LoadResourceAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}