using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using LFramework.Editor;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Editor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(BaseComponentSetting))]
    internal sealed class BaseComponentInspector : ComponentSettingInspector
    {
        private const string NoneOptionName = "<None>";
        private static readonly float[] GameSpeed = new float[] { 0f, 0.01f, 0.1f, 0.25f, 0.5f, 1f, 1.5f, 2f, 4f, 8f };

        private static readonly string[] GameSpeedForDisplay = new string[]
            { "0x", "0.01x", "0.1x", "0.25x", "0.5x", "1x", "1.5x", "2x", "4x", "8x" };

        private SerializedProperty m_EditorResourceMode = null;
        private SerializedProperty m_EditorLanguage = null;
        private SerializedProperty m_TextHelperTypeName = null;
        private SerializedProperty m_VersionHelperTypeName = null;
        private SerializedProperty m_LogHelperTypeName = null;
        private SerializedProperty m_CompressionHelperTypeName = null;
        private SerializedProperty m_JsonHelperTypeName = null;
        private SerializedProperty m_FrameRate = null;
        private SerializedProperty m_GameSpeed = null;
        private SerializedProperty m_RunInBackground = null;
        private SerializedProperty m_NeverSleep = null;

        private string[] m_TextHelperTypeNames = null;
        private int m_TextHelperTypeNameIndex = 0;
        private string[] m_VersionHelperTypeNames = null;
        private int m_VersionHelperTypeNameIndex = 0;
        private string[] m_LogHelperTypeNames = null;
        private int m_LogHelperTypeNameIndex = 0;
        private string[] m_CompressionHelperTypeNames = null;
        private int m_CompressionHelperTypeNameIndex = 0;
        private string[] m_JsonHelperTypeNames = null;
        private int m_JsonHelperTypeNameIndex = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            var t = GetComponent<BaseComponent>();
            EditorGUILayout.Space(4f);
            DrawOverviewBanner();

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawEditorResourceSection();
                DrawGlobalHelpersSection();
            }
            EditorGUI.EndDisabledGroup();

            DrawRuntimeSection(t);

            serializedObject.ApplyModifiedProperties();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();

            RefreshTypeNames();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            m_EditorResourceMode = serializedObject.FindProperty("m_EditorResourceMode");
            m_EditorLanguage = serializedObject.FindProperty("m_EditorLanguage");
            m_TextHelperTypeName = serializedObject.FindProperty("m_TextHelperTypeName");
            m_VersionHelperTypeName = serializedObject.FindProperty("m_VersionHelperTypeName");
            m_LogHelperTypeName = serializedObject.FindProperty("m_LogHelperTypeName");
            m_CompressionHelperTypeName = serializedObject.FindProperty("m_CompressionHelperTypeName");
            m_JsonHelperTypeName = serializedObject.FindProperty("m_JsonHelperTypeName");
            m_FrameRate = serializedObject.FindProperty("m_FrameRate");
            m_GameSpeed = serializedObject.FindProperty("m_GameSpeed");
            m_RunInBackground = serializedObject.FindProperty("m_RunInBackground");
            m_NeverSleep = serializedObject.FindProperty("m_NeverSleep");

            RefreshTypeNames();

        }

        private void DrawOverviewBanner()
        {
            string mode = EditorApplication.isPlaying ? "Live Runtime" : "Asset Edit";
            string message = EditorApplication.isPlaying
                ? "Frame rate, game speed, and runtime toggles apply to the active BaseComponent immediately."
                : "Editor-only setup and helper bindings are editable here before entering Play Mode.";

            EditorGUILayout.HelpBox($"Mode: {mode}\n{message}", MessageType.Info);
        }

        private void DrawEditorResourceSection()
        {
            BeginSection("Editor Resource Mode", "Editor-only switches used for local resource validation and localization tests.");
            m_EditorResourceMode.boolValue =
                EditorGUILayout.BeginToggleGroup("Enable Editor Resource Mode", m_EditorResourceMode.boolValue);
            {
                EditorGUILayout.HelpBox(
                    "When enabled in the editor, Game Framework uses editor resource files. Validate them before runtime tests.",
                    MessageType.Warning);
                EditorGUILayout.PropertyField(m_EditorLanguage);
                EditorGUILayout.HelpBox(
                    "Editor Language is only used for localization checks while running inside the editor.",
                    MessageType.None);
            }
            EditorGUILayout.EndToggleGroup();
            EndSection();
        }

        private void DrawGlobalHelpersSection()
        {
            BeginSection("Global Helpers", "Configure the framework-wide helper implementations used during bootstrap.");
            DrawHelperPopup("Text Helper", ref m_TextHelperTypeNameIndex, m_TextHelperTypeNames, m_TextHelperTypeName);
            DrawHelperPopup("Version Helper", ref m_VersionHelperTypeNameIndex, m_VersionHelperTypeNames, m_VersionHelperTypeName);
            DrawHelperPopup("Log Helper", ref m_LogHelperTypeNameIndex, m_LogHelperTypeNames, m_LogHelperTypeName);
            DrawHelperPopup("Compression Helper", ref m_CompressionHelperTypeNameIndex, m_CompressionHelperTypeNames, m_CompressionHelperTypeName);
            DrawHelperPopup("JSON Helper", ref m_JsonHelperTypeNameIndex, m_JsonHelperTypeNames, m_JsonHelperTypeName);
            EndSection();
        }

        private void DrawRuntimeSection(BaseComponent component)
        {
            BeginSection("Performance & Runtime", "Core pacing and application lifecycle options. These values update live in Play Mode.");

            int frameRate = EditorGUILayout.IntSlider("Frame Rate", m_FrameRate.intValue, 1, 120);
            if (frameRate != m_FrameRate.intValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.FrameRate = frameRate;
                }
                else
                {
                    m_FrameRate.intValue = frameRate;
                }
            }

            EditorGUILayout.Space(2f);
            GameWindowChrome.DrawCompactHeader("Game Speed", "Use the slider for fine tuning or tap a preset for quick switching.");
            float gameSpeed = EditorGUILayout.Slider("Speed Value", m_GameSpeed.floatValue, 0f, 8f);
            int selectedGameSpeed = GUILayout.SelectionGrid(
                GetSelectedGameSpeed(gameSpeed),
                GameSpeedForDisplay,
                GetGameSpeedGridColumns());
            if (selectedGameSpeed >= 0)
            {
                gameSpeed = GetGameSpeed(selectedGameSpeed);
            }

            if (gameSpeed != m_GameSpeed.floatValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.GameSpeed = gameSpeed;
                }
                else
                {
                    m_GameSpeed.floatValue = gameSpeed;
                }
            }

            EditorGUILayout.HelpBox($"Current Speed: {GetGameSpeedDisplay(gameSpeed)}", MessageType.None);

            bool runInBackground = EditorGUILayout.Toggle("Run in Background", m_RunInBackground.boolValue);
            if (runInBackground != m_RunInBackground.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.RunInBackground = runInBackground;
                }
                else
                {
                    m_RunInBackground.boolValue = runInBackground;
                }
            }

            bool neverSleep = EditorGUILayout.Toggle("Never Sleep", m_NeverSleep.boolValue);
            if (neverSleep != m_NeverSleep.boolValue)
            {
                if (EditorApplication.isPlaying)
                {
                    component.NeverSleep = neverSleep;
                }
                else
                {
                    m_NeverSleep.boolValue = neverSleep;
                }
            }

            EndSection();
        }

        private void RefreshTypeNames()
        {
            List<string> textHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            textHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Text.ITextHelper)));
            m_TextHelperTypeNames = textHelperTypeNames.ToArray();
            m_TextHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_TextHelperTypeName.stringValue))
            {
                m_TextHelperTypeNameIndex = textHelperTypeNames.IndexOf(m_TextHelperTypeName.stringValue);
                if (m_TextHelperTypeNameIndex <= 0)
                {
                    m_TextHelperTypeNameIndex = 0;
                    m_TextHelperTypeName.stringValue = null;
                }
            }

            List<string> versionHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            versionHelperTypeNames.AddRange(
                UnityGameFramework.Editor.Type.GetRuntimeTypeNames(typeof(Version.IVersionHelper)));
            m_VersionHelperTypeNames = versionHelperTypeNames.ToArray();
            m_VersionHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_VersionHelperTypeName.stringValue))
            {
                m_VersionHelperTypeNameIndex = versionHelperTypeNames.IndexOf(m_VersionHelperTypeName.stringValue);
                if (m_VersionHelperTypeNameIndex <= 0)
                {
                    m_VersionHelperTypeNameIndex = 0;
                    m_VersionHelperTypeName.stringValue = null;
                }
            }

            List<string> logHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            logHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(GameFrameworkLog.ILogHelper)));
            m_LogHelperTypeNames = logHelperTypeNames.ToArray();
            m_LogHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_LogHelperTypeName.stringValue))
            {
                m_LogHelperTypeNameIndex = logHelperTypeNames.IndexOf(m_LogHelperTypeName.stringValue);
                if (m_LogHelperTypeNameIndex <= 0)
                {
                    m_LogHelperTypeNameIndex = 0;
                    m_LogHelperTypeName.stringValue = null;
                }
            }

            List<string> compressionHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            compressionHelperTypeNames.AddRange(
                Type.GetRuntimeTypeNames(typeof(Utility.Compression.ICompressionHelper)));
            m_CompressionHelperTypeNames = compressionHelperTypeNames.ToArray();
            m_CompressionHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_CompressionHelperTypeName.stringValue))
            {
                m_CompressionHelperTypeNameIndex =
                    compressionHelperTypeNames.IndexOf(m_CompressionHelperTypeName.stringValue);
                if (m_CompressionHelperTypeNameIndex <= 0)
                {
                    m_CompressionHelperTypeNameIndex = 0;
                    m_CompressionHelperTypeName.stringValue = null;
                }
            }

            List<string> jsonHelperTypeNames = new List<string>
            {
                NoneOptionName
            };

            jsonHelperTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(Utility.Json.IJsonHelper)));
            m_JsonHelperTypeNames = jsonHelperTypeNames.ToArray();
            m_JsonHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_JsonHelperTypeName.stringValue))
            {
                m_JsonHelperTypeNameIndex = jsonHelperTypeNames.IndexOf(m_JsonHelperTypeName.stringValue);
                if (m_JsonHelperTypeNameIndex <= 0)
                {
                    m_JsonHelperTypeNameIndex = 0;
                    m_JsonHelperTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawHelperPopup(
            string label,
            ref int selectedIndex,
            string[] options,
            SerializedProperty targetProperty)
        {
            int nextIndex = EditorGUILayout.Popup(label, selectedIndex, options);
            if (nextIndex != selectedIndex)
            {
                selectedIndex = nextIndex;
                targetProperty.stringValue = nextIndex <= 0 ? null : options[nextIndex];
            }
        }

        private float GetGameSpeed(int selectedGameSpeed)
        {
            if (selectedGameSpeed < 0)
            {
                return GameSpeed[0];
            }

            if (selectedGameSpeed >= GameSpeed.Length)
            {
                return GameSpeed[GameSpeed.Length - 1];
            }

            return GameSpeed[selectedGameSpeed];
        }

        private int GetSelectedGameSpeed(float gameSpeed)
        {
            for (int i = 0; i < GameSpeed.Length; i++)
            {
                if (gameSpeed == GameSpeed[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private int GetGameSpeedGridColumns()
        {
            if (EditorGUIUtility.currentViewWidth < 520f)
            {
                return 2;
            }

            if (EditorGUIUtility.currentViewWidth < 720f)
            {
                return 3;
            }

            return 5;
        }

        private string GetGameSpeedDisplay(float gameSpeed)
        {
            int selectedGameSpeed = GetSelectedGameSpeed(gameSpeed);
            if (selectedGameSpeed >= 0)
            {
                return GameSpeedForDisplay[selectedGameSpeed];
            }

            return $"{gameSpeed:0.##}x (Custom)";
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
