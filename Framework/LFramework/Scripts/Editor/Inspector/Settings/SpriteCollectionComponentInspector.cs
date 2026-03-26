using LFramework.Editor;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(SpriteCollectionComponentSetting))]
    public sealed class SpriteCollectionComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_CheckCanReleaseInterval = null;
        private SerializedProperty m_AutoReleaseInterval = null;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_AutoReleaseInterval = serializedObject.FindProperty("m_AutoReleaseInterval");
            m_CheckCanReleaseInterval = serializedObject.FindProperty("m_CheckCanReleaseInterval");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                $"Check Interval: {m_CheckCanReleaseInterval.floatValue:0.##}s  Auto Release: {m_AutoReleaseInterval.floatValue:0.##}s\n" +
                "Sprite collection release cadence is grouped below.",
                MessageType.Info);

            BeginSection("Release Policy", "Tune how often the collection checks for releasable sprites and performs auto release.");
            EditorGUILayout.PropertyField(m_CheckCanReleaseInterval);
            EditorGUILayout.PropertyField(m_AutoReleaseInterval);
            EndSection();

            serializedObject.ApplyModifiedProperties();

            Repaint();
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
