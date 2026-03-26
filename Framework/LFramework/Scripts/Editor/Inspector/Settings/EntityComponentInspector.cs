using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(EntityComponentSetting))]
    internal sealed class EntityComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnableShowEntityUpdateEvent = null;
        private SerializedProperty m_EnableShowEntityDependencyAssetEvent = null;
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_EntityGroups = null;

        private readonly HelperInfo<EntityHelperBase> m_EntityHelperInfo = new HelperInfo<EntityHelperBase>("Entity");
        private readonly HelperInfo<EntityGroupHelperBase> m_EntityGroupHelperInfo =
            new HelperInfo<EntityGroupHelperBase>("EntityGroup");

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
            m_EnableShowEntityUpdateEvent = serializedObject.FindProperty("m_EnableShowEntityUpdateEvent");
            m_EnableShowEntityDependencyAssetEvent =
                serializedObject.FindProperty("m_EnableShowEntityDependencyAssetEvent");
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_EntityGroups = serializedObject.FindProperty("m_EntityGroups");

            m_EntityHelperInfo.Init(serializedObject);
            m_EntityGroupHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_EntityHelperInfo.Refresh();
            m_EntityGroupHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            EditorGUILayout.HelpBox(
                $"Entity Groups: {m_EntityGroups.arraySize}\n" +
                "Entity events, root hierarchy, helper bindings, and group definitions are organized below.",
                MessageType.Info);
        }

        private void DrawEventSection()
        {
            BeginSection("Event Dispatch", "Enable only the entity lifecycle events that your runtime actually consumes.");
            EditorGUILayout.PropertyField(m_EnableShowEntityUpdateEvent);
            EditorGUILayout.PropertyField(m_EnableShowEntityDependencyAssetEvent);
            EndSection();
        }

        private void DrawHelperSection()
        {
            BeginSection("Hierarchy & Helpers", "Configure the entity instance root and helper implementations.");
            EditorGUILayout.PropertyField(m_InstanceRoot);
            m_EntityHelperInfo.Draw();
            m_EntityGroupHelperInfo.Draw();

            if (m_InstanceRoot.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Instance Root is empty. EntityComponent will create a runtime root automatically.", MessageType.Info);
            }

            EndSection();
        }

        private void DrawGroupSection()
        {
            BeginSection("Entity Groups", "Configure group names, release cadence, capacity, and priorities.");
            EditorGUILayout.PropertyField(m_EntityGroups, true);
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
