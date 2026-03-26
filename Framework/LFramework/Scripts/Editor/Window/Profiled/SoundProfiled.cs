using GameFramework;
using GameFramework.Sound;
using LFramework.Editor;
using UnityEditor;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class SoundProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private SoundComponent _soundComponent;

        internal override void Draw()
        {
            GetComponent(ref _soundComponent);
            if (_soundComponent == null)
            {
                EditorGUILayout.HelpBox("SoundComponent is unavailable in the current runtime context.", MessageType.Info);
                return;
            }

            GameWindowChrome.DrawCompactHeader("Sound Overview");
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Sound Group Count", _soundComponent.SoundGroupCount.ToString());
            EditorGUILayout.EndVertical();

            ISoundGroup[] soundGroups = _soundComponent.GetAllSoundGroups();
            GameWindowChrome.DrawCompactHeader("Sound Groups");
            EditorGUILayout.BeginVertical("box");
            if (soundGroups == null || soundGroups.Length == 0)
            {
                EditorGUILayout.LabelField("No sound groups found.");
            }
            else
            {
                foreach (ISoundGroup soundGroup in soundGroups)
                {
                    DrawSoundGroup(soundGroup);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSoundGroup(ISoundGroup soundGroup)
        {
            EditorGUILayout.LabelField(
                Utility.Text.Format("Group: {0}", soundGroup.Name),
                Utility.Text.Format("Agents: {0}  Mute: {1}  Volume: {2:F2}", soundGroup.SoundAgentCount, soundGroup.Mute, soundGroup.Volume));
        }
    }
}
