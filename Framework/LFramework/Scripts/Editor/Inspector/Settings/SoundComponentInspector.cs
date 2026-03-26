using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(SoundComponentSetting))]
    internal sealed class SoundComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnablePlaySoundUpdateEvent = null;
        private SerializedProperty m_EnablePlaySoundDependencyAssetEvent = null;
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_AudioMixer = null;
        private SerializedProperty m_SoundGroups = null;

        private readonly HelperInfo<SoundHelperBase> m_SoundHelperInfo = new HelperInfo<SoundHelperBase>("Sound");
        private readonly HelperInfo<SoundGroupHelperBase> m_SoundGroupHelperInfo = new HelperInfo<SoundGroupHelperBase>("SoundGroup");
        private readonly HelperInfo<SoundAgentHelperBase> m_SoundAgentHelperInfo = new HelperInfo<SoundAgentHelperBase>("SoundAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);
            DrawOverviewBanner();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawEventSection();
                DrawHelperSection();
                DrawGroupSection();
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
            m_EnablePlaySoundUpdateEvent = serializedObject.FindProperty("m_EnablePlaySoundUpdateEvent");
            m_EnablePlaySoundDependencyAssetEvent = serializedObject.FindProperty("m_EnablePlaySoundDependencyAssetEvent");
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_AudioMixer = serializedObject.FindProperty("m_AudioMixer");
            m_SoundGroups = serializedObject.FindProperty("m_SoundGroups");

            m_SoundHelperInfo.Init(serializedObject);
            m_SoundGroupHelperInfo.Init(serializedObject);
            m_SoundAgentHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_SoundHelperInfo.Refresh();
            m_SoundGroupHelperInfo.Refresh();
            m_SoundAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            EditorGUILayout.HelpBox(
                $"Sound Groups: {m_SoundGroups.arraySize}\n" +
                "Sound event switches, mixer binding, helper types, and group definitions are organized below.",
                MessageType.Info);
        }

        private void DrawEventSection()
        {
            BeginSection("Event Dispatch", "Enable only the sound lifecycle events you actually consume.");
            EditorGUILayout.PropertyField(m_EnablePlaySoundUpdateEvent);
            EditorGUILayout.PropertyField(m_EnablePlaySoundDependencyAssetEvent);
            EndSection();
        }

        private void DrawHelperSection()
        {
            BeginSection("Hierarchy & Helpers", "Configure the instance root, optional audio mixer, and sound-related helpers.");
            EditorGUILayout.PropertyField(m_InstanceRoot);
            EditorGUILayout.PropertyField(m_AudioMixer);
            m_SoundHelperInfo.Draw();
            m_SoundGroupHelperInfo.Draw();
            m_SoundAgentHelperInfo.Draw();

            if (m_InstanceRoot.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Instance Root is empty. SoundComponent will create a runtime root automatically.", MessageType.Info);
            }

            EndSection();
        }

        private void DrawGroupSection()
        {
            BeginSection("Sound Groups", "Configure group behavior, priorities, and agent counts for each sound channel.");
            EditorGUILayout.PropertyField(m_SoundGroups, true);
            EndSection();
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
