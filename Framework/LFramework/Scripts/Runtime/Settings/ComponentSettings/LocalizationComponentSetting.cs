
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "LocalizationComponentSetting",
        menuName = "LFramework/Settings/LocalizationComponentSetting")]
    public class LocalizationComponentSetting : ComponentSetting
    {
        
        
        [SerializeField]
        private bool m_EnableLoadDictionaryUpdateEvent = false;

        [SerializeField]
        private bool m_EnableLoadDictionaryDependencyAssetEvent = false;

        [SerializeField]
        private string m_LocalizationHelperTypeName = "UnityGameFramework.Runtime.DefaultLocalizationHelper";

        [SerializeField]
        private LocalizationHelperBase m_CustomLocalizationHelper = null;

        [SerializeField]
        private int m_CachedBytesSize = 0;
        
    }
}