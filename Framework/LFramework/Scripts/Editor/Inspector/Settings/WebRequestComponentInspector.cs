using LFramework.Editor;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(WebRequestComponentSetting))]
    internal sealed class WebRequestComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty m_InstanceRoot = null;
        private SerializedProperty m_WebRequestAgentHelperCount = null;
        private SerializedProperty m_Timeout = null;

        private readonly HelperInfo<WebRequestAgentHelperBase> m_WebRequestAgentHelperInfo =
            new HelperInfo<WebRequestAgentHelperBase>("WebRequestAgent");

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            var component = GetComponent<WebRequestComponent>();
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
            m_WebRequestAgentHelperCount = serializedObject.FindProperty("m_WebRequestAgentHelperCount");
            m_Timeout = serializedObject.FindProperty("m_Timeout");

            m_WebRequestAgentHelperInfo.Init(serializedObject);

            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            m_WebRequestAgentHelperInfo.Refresh();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawOverviewBanner()
        {
            string mode = EditorApplication.isPlaying ? "Live Runtime" : "Asset Edit";
            EditorGUILayout.HelpBox(
                $"Mode: {mode}  Helper Count: {m_WebRequestAgentHelperCount.intValue}\n" +
                "Agent creation, helper binding, and timeout policy are grouped below.",
                MessageType.Info);
        }

        private void DrawAgentSection()
        {
            BeginSection("Agents & Helpers", "Configure the request root, helper type, and worker count.");
            EditorGUILayout.PropertyField(m_InstanceRoot);
            m_WebRequestAgentHelperInfo.Draw();
            m_WebRequestAgentHelperCount.intValue = EditorGUILayout.IntSlider(
                "Web Request Agent Helper Count",
                m_WebRequestAgentHelperCount.intValue,
                1,
                16);

            if (m_InstanceRoot.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Instance Root is empty. WebRequestComponent will create a runtime root automatically.", MessageType.Info);
            }

            EndSection();
        }

        private void DrawRuntimeSection(WebRequestComponent component)
        {
            BeginSection("Timeout Policy", "This timeout value updates the live WebRequestComponent while the game is running.");

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

            EditorGUILayout.HelpBox($"Timeout: {m_Timeout.floatValue:0.##}s", MessageType.None);
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
