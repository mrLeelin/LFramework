using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;

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
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                EditorApplication.isPlaying
                    ? "Reference pool strict-check mode is locked while the game is running."
                    : "Choose how aggressively the reference pool validates object reuse in Edit Mode.",
                EditorApplication.isPlaying ? MessageType.Info : MessageType.None);

            BeginSection("Strict Check", "This option controls validation behavior for pooled reference objects.");
            if (EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Strict-check mode cannot be changed during Play Mode.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.PropertyField(m_EnableStrictCheck);
            }

            EndSection();

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EnableStrictCheck = serializedObject.FindProperty("m_EnableStrictCheck");
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
