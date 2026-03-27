#if NOTIFICATION_SUPPORT
using LFramework.Editor.Inspector;
using LFramework.Runtime;
using UnityEditor;

namespace LFramework.Editor
{
    [CustomEditor(typeof(GameNotificationsComponentSetting))]
    public sealed class GameNotificationsComponentSettingInspector : ComponentSettingInspector
    {
        private SerializedProperty m_Mode = null;
        private SerializedProperty m_AutoBadging = null;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Configure how notification requests are queued and whether badge counts are assigned automatically.",
                MessageType.Info);

            BeginSection("Notification Behavior", "These options control operating mode and badge assignment strategy.");
            EditorGUILayout.PropertyField(m_Mode);
            EditorGUILayout.PropertyField(m_AutoBadging);
            EndSection();

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Mode = serializedObject.FindProperty("mode");
            m_AutoBadging = serializedObject.FindProperty("autoBadging");
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
#endif
