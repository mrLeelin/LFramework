using System.Collections.Generic;
using LFramework.Editor;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using Type = UnityGameFramework.Editor.Type;

namespace LFramework.Editor.Inspector
{
    [CustomEditor(typeof(NetworkComponentSetting))]
    internal sealed class NetworkComponentInspector : ComponentSettingInspector
    {
        private SerializedProperty _loginTimeOutSecond;
        private SerializedProperty _timeOutSecond;
        private SerializedProperty _networkChannelTypeName;
        private List<string> _networkChannelTypeNames;
        private int _networkChannelTypeNameIndex;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.Space(4f);
            DrawNetworkSection();
            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _loginTimeOutSecond = serializedObject.FindProperty("_loginTimeOutSecond");
            _timeOutSecond = serializedObject.FindProperty("_timeOutSecond");
            _networkChannelTypeName = serializedObject.FindProperty("_networkChannelTypeName");
            RefreshTypeNames();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void RefreshTypeNames()
        {
            _networkChannelTypeNames = new List<string> { "<None>" };
            _networkChannelTypeNames.AddRange(Type.GetRuntimeTypeNames(typeof(NetworkChannelBase)));

            _networkChannelTypeNameIndex = string.IsNullOrEmpty(_networkChannelTypeName.stringValue)
                ? 0
                : _networkChannelTypeNames.IndexOf(_networkChannelTypeName.stringValue);
            if (_networkChannelTypeNameIndex < 0)
            {
                _networkChannelTypeNameIndex = 0;
            }
        }

        private void DrawNetworkSection()
        {
            BeginSection("Network Channel", "Choose the channel implementation and timeout policy.");

            _networkChannelTypeNameIndex = EditorGUILayout.Popup(
                "Network Channel",
                _networkChannelTypeNameIndex,
                _networkChannelTypeNames.ToArray());
            if (_networkChannelTypeNameIndex > 0)
            {
                _networkChannelTypeName.stringValue = _networkChannelTypeNames[_networkChannelTypeNameIndex];
            }
            else
            {
                _networkChannelTypeName.stringValue = null;
                EditorGUILayout.HelpBox("Network channel type is not selected.", MessageType.Error);
            }

            _loginTimeOutSecond.intValue =
                EditorGUILayout.IntSlider("Login Timeout Second", _loginTimeOutSecond.intValue, 1, 120);
            _timeOutSecond.intValue =
                EditorGUILayout.IntSlider("Timeout Second", _timeOutSecond.intValue, 1, 120);

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
