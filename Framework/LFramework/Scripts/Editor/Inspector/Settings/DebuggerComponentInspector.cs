using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(DebuggerComponentSetting))]
    internal sealed class DebuggerComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_Skin = null;
        private SerializedProperty m_ActiveWindow = null;
        private SerializedProperty m_ShowFullWindow = null;
        private SerializedProperty m_ConsoleWindow = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                EditorApplication.isPlaying
                    ? "Debugger appearance is editable now, but the startup active window stays locked during Play Mode."
                    : "Debugger appearance and startup window preferences are grouped below.",
                MessageType.Info);

            BeginSection("Appearance", "Configure the debugger skin and startup window selection.");
            EditorGUILayout.PropertyField(m_Skin);
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Active Window is locked during Play Mode.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(m_ActiveWindow);
            }

            EndSection();

            BeginSection("Window Options", "Control full-window startup behavior and console view settings.");
            EditorGUILayout.PropertyField(m_ShowFullWindow);
            EditorGUILayout.PropertyField(m_ConsoleWindow, true);
            EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Skin = serializedObject.FindProperty("m_Skin");
            m_ActiveWindow = serializedObject.FindProperty("m_ActiveWindow");
            m_ShowFullWindow = serializedObject.FindProperty("m_ShowFullWindow");
            m_ConsoleWindow = serializedObject.FindProperty("m_ConsoleWindow");
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
