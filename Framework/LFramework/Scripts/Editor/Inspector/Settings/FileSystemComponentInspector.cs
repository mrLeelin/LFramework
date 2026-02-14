
using GameFramework;
using GameFramework.FileSystem;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    
    [CustomEditor(typeof(FileSystemComponentSetting))]
    internal sealed class FileSystemComponentInspector : ComponentSettingInspector
    {
        private HelperInfo<FileSystemHelperBase> m_FileSystemHelperInfo =
            new HelperInfo<FileSystemHelperBase>("FileSystem");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

         
            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                m_FileSystemHelperInfo.Draw();
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
            m_FileSystemHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_FileSystemHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

     
    }
}