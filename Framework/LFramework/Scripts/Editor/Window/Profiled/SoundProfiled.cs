using GameFramework;
using GameFramework.Sound;
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

            DrawMetricCards(
                new ProfiledMetric("Sound Groups", _soundComponent.SoundGroupCount.ToString(), "Registered groups"));

            ISoundGroup[] soundGroups = _soundComponent.GetAllSoundGroups();
            DrawSection(
                "Sound Groups",
                "Playback capacity, mute state, and volume for each sound group registered in the runtime component.",
                () =>
                {
                    if (soundGroups == null || soundGroups.Length == 0)
                    {
                        DrawKeyValueRow("Groups", "None");
                        return;
                    }

                    foreach (ISoundGroup soundGroup in soundGroups)
                    {
                        DrawKeyValueRow(
                            soundGroup.Name,
                            Utility.Text.Format("Agents {0}  Mute {1}  Volume {2:F2}", soundGroup.SoundAgentCount, soundGroup.Mute, soundGroup.Volume));
                    }
                });
        }
    }
}
