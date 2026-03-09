

using GameFramework;
using GameFramework.Sound;
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

            EditorGUILayout.LabelField("Sound Group Count", _soundComponent.SoundGroupCount.ToString());

            ISoundGroup[] soundGroups = _soundComponent.GetAllSoundGroups();
            foreach (ISoundGroup soundGroup in soundGroups)
            {
                DrawSoundGroup(soundGroup);
            }
        }

        private void DrawSoundGroup(ISoundGroup soundGroup)
        {
            EditorGUILayout.LabelField(Utility.Text.Format("Group: {0}", soundGroup.Name),
                Utility.Text.Format("Agent: {0}  Mute: {1}  Volume: {2:F2}", soundGroup.SoundAgentCount,
                    soundGroup.Mute, soundGroup.Volume));
        }
    }
}