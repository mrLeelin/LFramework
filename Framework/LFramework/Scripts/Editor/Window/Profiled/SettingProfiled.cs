using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class SettingProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private SettingComponent _settingComponent;

        internal override void Draw()
        {
            GetComponent(ref _settingComponent);
            EditorGUILayout.LabelField("Setting Count", _settingComponent.Count >= 0 ? _settingComponent.Count.ToString() : "<Unknown>");
            if (_settingComponent.Count > 0)
            {
                string[] settingNames = _settingComponent.GetAllSettingNames();
                foreach (string settingName in settingNames)
                {
                    EditorGUILayout.LabelField(settingName, _settingComponent.GetString(settingName));
                }
            }

            if (GUILayout.Button("Save Settings"))
            {
                _settingComponent.Save();
            }

            if (GUILayout.Button("Remove All Settings"))
            {
                _settingComponent.RemoveAllSettings();
            }
        }
    }
}