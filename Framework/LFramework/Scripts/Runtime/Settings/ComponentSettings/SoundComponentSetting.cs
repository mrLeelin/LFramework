

using UnityEngine;
using UnityEngine.Audio;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "SoundComponentSetting",
        menuName = "LFramework/Settings/SoundComponentSetting")]
    public sealed class SoundComponentSetting : ComponentSetting
    {
        public bool m_EnablePlaySoundUpdateEvent = false;

        public bool m_EnablePlaySoundDependencyAssetEvent = false;

        public Transform m_InstanceRoot = null;

        public AudioMixer m_AudioMixer = null;

        public string m_SoundHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundHelper";

        public SoundHelperBase m_CustomSoundHelper = null;

        public string m_SoundGroupHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundGroupHelper";

        public SoundGroupHelperBase m_CustomSoundGroupHelper = null;

        public string m_SoundAgentHelperTypeName = "UnityGameFramework.Runtime.DefaultSoundAgentHelper";

        public SoundAgentHelperBase m_CustomSoundAgentHelper = null;

        public SoundComponent.SoundGroup[] m_SoundGroups = null;
        
    }
}