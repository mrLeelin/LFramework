using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(FileSystemComponentSetting))]
    internal sealed class FileSystemComponentInspector : ComponentSettingInspector
    {
        private readonly HelperInfo<FileSystemHelperBase> m_FileSystemHelperInfo =
            new HelperInfo<FileSystemHelperBase>("FileSystem");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox("Configure the FileSystem helper used to create and resolve framework file systems.", MessageType.Info);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                BeginSection("Helper", "This helper is only changed in Edit Mode.");
                m_FileSystemHelperInfo.Draw();
                EndSection();
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
            base.OnEnable();
            m_FileSystemHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_FileSystemHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private static void BeginSection(string title, string subtitle)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GameWindowChrome.DrawCompactHeader(title, subtitle);
            EditorGUILayout.Space(4f);
        }

        private static void EndSection()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
        }
    }
}
