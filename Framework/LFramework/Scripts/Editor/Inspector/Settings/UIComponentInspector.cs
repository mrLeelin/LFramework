using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(UIComponentSetting))]
    internal sealed class UIComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_EnableOpenUIFormSuccessEvent = null;
        private SerializedProperty m_EnableOpenUIFormFailureEvent = null;
        private SerializedProperty m_EnableOpenUIFormUpdateEvent = null;
        private SerializedProperty m_EnableOpenUIFormDependencyAssetEvent = null;
        private SerializedProperty m_EnableCloseUIFormCompleteEvent = null;
        private SerializedProperty m_InstanceAutoReleaseInterval = null;
        private SerializedProperty m_InstanceCapacity = null;
        private SerializedProperty m_InstanceExpireTime = null;
        private SerializedProperty m_InstancePriority = null;
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_UIGroups = null;
        private SerializedProperty m_InstanceRootOffset = null;
        private SerializedProperty m_VertexColorAlwaysGammaSpace = null;

        private readonly HelperInfo<UIFormHelperBase> m_UIFormHelperInfo = new HelperInfo<UIFormHelperBase>("UIForm");
        private readonly HelperInfo<UIGroupHelperBase> m_UIGroupHelperInfo = new HelperInfo<UIGroupHelperBase>("UIGroup");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var component = GetComponent<UIComponent>();
            serializedObject.Update();
            EditorGUILayout.Space(4f);
            DrawOverviewBanner();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawEventSection();
            }
            EditorGUI.EndDisabledGroup();

            DrawPoolSection(component);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawHierarchySection();
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
            m_EnableOpenUIFormSuccessEvent = serializedObject.FindProperty("m_EnableOpenUIFormSuccessEvent");
            m_EnableOpenUIFormFailureEvent = serializedObject.FindProperty("m_EnableOpenUIFormFailureEvent");
            m_EnableOpenUIFormUpdateEvent = serializedObject.FindProperty("m_EnableOpenUIFormUpdateEvent");
            m_EnableOpenUIFormDependencyAssetEvent =
                serializedObject.FindProperty("m_EnableOpenUIFormDependencyAssetEvent");
            m_EnableCloseUIFormCompleteEvent = serializedObject.FindProperty("m_EnableCloseUIFormCompleteEvent");
            m_InstanceAutoReleaseInterval = serializedObject.FindProperty("m_InstanceAutoReleaseInterval");
            m_InstanceCapacity = serializedObject.FindProperty("m_InstanceCapacity");
            m_InstanceExpireTime = serializedObject.FindProperty("m_InstanceExpireTime");
            m_InstancePriority = serializedObject.FindProperty("m_InstancePriority");
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_UIGroups = serializedObject.FindProperty("m_UIGroups");
            m_InstanceRootOffset = serializedObject.FindProperty("m_InstanceRootOffset");
            m_VertexColorAlwaysGammaSpace = serializedObject.FindProperty("m_VertexColorAlwaysGammaSpace");
            m_UIFormHelperInfo.Init(serializedObject);
            m_UIGroupHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_UIFormHelperInfo.Refresh();
            m_UIGroupHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            string mode = EditorApplication.isPlaying ? "Live Runtime" : "Asset Edit";
            EditorGUILayout.HelpBox(
                $"Mode: {mode}  UI Groups: {m_UIGroups.arraySize}\n" +
                "UI events, hierarchy setup, pool settings, and helper bindings are grouped below for faster inspection.",
                MessageType.Info);
        }

        private void DrawEventSection()
        {
            BeginSection("Event Dispatch", "Choose which UI lifecycle events should be emitted by the framework.");
            EditorGUILayout.PropertyField(m_EnableOpenUIFormSuccessEvent);
            EditorGUILayout.PropertyField(m_EnableOpenUIFormFailureEvent);
            EditorGUILayout.PropertyField(m_EnableOpenUIFormUpdateEvent);
            EditorGUILayout.PropertyField(m_EnableOpenUIFormDependencyAssetEvent);
            EditorGUILayout.PropertyField(m_EnableCloseUIFormCompleteEvent);
            EndSection();
        }

        private void DrawPoolSection(UIComponent component)
        {
            BeginSection("Instance Pool", "These cache settings update live in Play Mode and control UI form instance recycling.");

            float instanceAutoReleaseInterval = EditorGUILayout.DelayedFloatField(
                "Instance Auto Release Interval",
                m_InstanceAutoReleaseInterval.floatValue);
            if (instanceAutoReleaseInterval != m_InstanceAutoReleaseInterval.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.InstanceAutoReleaseInterval = instanceAutoReleaseInterval;
                }
                else
                {
                    m_InstanceAutoReleaseInterval.floatValue = instanceAutoReleaseInterval;
                }
            }

            int instanceCapacity = EditorGUILayout.DelayedIntField("Instance Capacity", m_InstanceCapacity.intValue);
            if (instanceCapacity != m_InstanceCapacity.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.InstanceCapacity = instanceCapacity;
                }
                else
                {
                    m_InstanceCapacity.intValue = instanceCapacity;
                }
            }

            float instanceExpireTime = EditorGUILayout.DelayedFloatField(
                "Instance Expire Time",
                m_InstanceExpireTime.floatValue);
            if (instanceExpireTime != m_InstanceExpireTime.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.InstanceExpireTime = instanceExpireTime;
                }
                else
                {
                    m_InstanceExpireTime.floatValue = instanceExpireTime;
                }
            }

            int instancePriority = EditorGUILayout.DelayedIntField("Instance Priority", m_InstancePriority.intValue);
            if (instancePriority != m_InstancePriority.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.InstancePriority = instancePriority;
                }
                else
                {
                    m_InstancePriority.intValue = instancePriority;
                }
            }

            EditorGUILayout.HelpBox(
                $"Auto Release: {m_InstanceAutoReleaseInterval.floatValue:0.##}s  Capacity: {m_InstanceCapacity.intValue}  " +
                $"Expire: {m_InstanceExpireTime.floatValue:0.##}s  Priority: {m_InstancePriority.intValue}",
                MessageType.None);
            EndSection();
        }

        private void DrawHierarchySection()
        {
            BeginSection("Hierarchy & Helpers", "Configure the UI root transform, root offset, and helper implementations.");
            EditorGUILayout.PropertyField(m_InstanceRoot);
            EditorGUILayout.PropertyField(m_InstanceRootOffset);
            EditorGUILayout.PropertyField(
                m_VertexColorAlwaysGammaSpace,
                new GUIContent("Vertex Color Always Gamma Space"));
            m_UIFormHelperInfo.Draw();
            m_UIGroupHelperInfo.Draw();

            if (m_InstanceRoot.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Instance Root is empty. UIComponent will create a runtime root automatically.", MessageType.Info);
            }

            EndSection();
        }

        private void DrawGroupSection()
        {
            BeginSection("UI Groups", "Configure UI group names and depths in the same order they should be created.");
            EditorGUILayout.PropertyField(m_UIGroups, true);
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
