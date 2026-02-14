
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "SettingComponentSetting",
        menuName = "LFramework/Settings/SettingComponentSetting")]
    public sealed class SettingComponentSetting : ComponentSetting
    {
        
        [SerializeField]
        private string m_SettingHelperTypeName = "UnityGameFramework.Runtime.DefaultSettingHelper";

        [SerializeField]
        private SettingHelperBase m_CustomSettingHelper = null;
    }
}