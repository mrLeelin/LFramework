
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "LocalizationComponentSetting",
        menuName = "LFramework/Settings/LocalizationComponentSetting")]
    public class LocalizationComponentSetting : ComponentSetting
    {
        
        
        // 这些字段源于 UnityGameFramework 的 LocalizationComponent 配置，LFramework 的 LanguageComponent
        // 不消费它们，仅保留设置资产兼容性，因此不参与组件参数映射。
        [ComponentSettingMappingIgnore]
        [SerializeField]
        private bool m_EnableLoadDictionaryUpdateEvent = false;

        [ComponentSettingMappingIgnore]
        [SerializeField]
        private bool m_EnableLoadDictionaryDependencyAssetEvent = false;

        [ComponentSettingMappingIgnore]
        [SerializeField]
        private string m_LocalizationHelperTypeName = "UnityGameFramework.Runtime.DefaultLocalizationHelper";

        [ComponentSettingMappingIgnore]
        [SerializeField]
        private LocalizationHelperBase m_CustomLocalizationHelper = null;

        [ComponentSettingMappingIgnore]
        [SerializeField]
        private int m_CachedBytesSize = 0;
        
    }
}