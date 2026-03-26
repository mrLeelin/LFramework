using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(DownloadComponentSetting))]
    internal sealed class DownloadComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_DownloadAgentHelperCount = null;
        private SerializedProperty m_Timeout = null;
        private SerializedProperty m_FlushSize = null;

        private readonly HelperInfo<DownloadAgentHelperBase> m_DownloadAgentHelperInfo =
            new HelperInfo<DownloadAgentHelperBase>("DownloadAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            var component = GetComponent<DownloadComponent>();
            EditorGUILayout.Space(4f);
            DrawOverviewBanner();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawAgentSection();
            }
            EditorGUI.EndDisabledGroup();

            DrawRuntimeSection(component);

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
            m_InstanceRoot = serializedObject.FindProperty("m_InstanceRoot");
            m_DownloadAgentHelperCount = serializedObject.FindProperty("m_DownloadAgentHelperCount");
            m_Timeout = serializedObject.FindProperty("m_Timeout");
            m_FlushSize = serializedObject.FindProperty("m_FlushSize");

            m_DownloadAgentHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_DownloadAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            string mode = EditorApplication.isPlaying ? "Live Runtime" : "Asset Edit";
            EditorGUILayout.HelpBox(
                $"Mode: {mode}  Helper Count: {m_DownloadAgentHelperCount.intValue}\n" +
                "Agent creation, timeout policy, and download flush settings are grouped below.",
                MessageType.Info);
        }

        private void DrawAgentSection()
        {
            BeginSection("Agents & Helpers", "Configure the download instance root, helper type, and worker count.");
            EditorGUILayout.PropertyField(m_InstanceRoot);
            m_DownloadAgentHelperInfo.Draw();
            m_DownloadAgentHelperCount.intValue = EditorGUILayout.IntSlider(
                "Download Agent Helper Count",
                m_DownloadAgentHelperCount.intValue,
                1,
                16);

            if (m_InstanceRoot.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Instance Root is empty. DownloadComponent will create a runtime root automatically.", MessageType.Info);
            }

            EndSection();
        }

        private void DrawRuntimeSection(DownloadComponent component)
        {
            BeginSection("Transfer Policy", "Timeout and flush size can be tuned here and now apply in Play Mode as well.");

            float timeout = EditorGUILayout.Slider("Timeout", m_Timeout.floatValue, 0f, 120f);
            if (timeout != m_Timeout.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.Timeout = timeout;
                }
                else
                {
                    m_Timeout.floatValue = timeout;
                }
            }

            int flushSize = EditorGUILayout.DelayedIntField("Flush Size", m_FlushSize.intValue);
            if (flushSize != m_FlushSize.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.FlushSize = flushSize;
                }
                else
                {
                    m_FlushSize.intValue = flushSize;
                }
            }

            EditorGUILayout.HelpBox(
                $"Timeout: {m_Timeout.floatValue:0.##}s  Flush Size: {m_FlushSize.intValue}",
                MessageType.None);
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
