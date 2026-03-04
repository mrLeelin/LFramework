using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(ResourceComponentSetting))]
    internal sealed class ResourceComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_ResourceMode = null;
        private SerializedProperty m_MinUnloadInterval = null;
        private SerializedProperty m_MaxUnloadInterval = null;
        private SerializedProperty m_YooAssetPackageName = null;
        private SerializedProperty m_YooAssetPlayMode = null;

        private HelperInfo<ResourceHelperBase> m_ResourceHelperInfo = new HelperInfo<ResourceHelperBase>("Resource");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                EditorGUILayout.PropertyField(m_ResourceMode);
                m_ResourceHelperInfo.Draw();

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Resource Release Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(m_MinUnloadInterval);
                EditorGUILayout.PropertyField(m_MaxUnloadInterval);

                if (m_ResourceMode.enumValueIndex == (int)ResourceMode.YooAsset)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("YooAsset Settings", EditorStyles.boldLabel);
                    EditorGUILayout.PropertyField(m_YooAssetPackageName);
                    EditorGUILayout.PropertyField(m_YooAssetPlayMode);
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
            m_ResourceMode = serializedObject.FindProperty("_resourceMode");
            m_MinUnloadInterval = serializedObject.FindProperty("_minUnloadInterval");
            m_MaxUnloadInterval = serializedObject.FindProperty("_maxUnloadInterval");
            m_YooAssetPackageName = serializedObject.FindProperty("_yooAssetPackageName");
            m_YooAssetPlayMode = serializedObject.FindProperty("_yooAssetPlayMode");

            m_ResourceHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_ResourceHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }
    }
}
