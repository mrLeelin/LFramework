

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    
    [CreateAssetMenu(order = 1, fileName = "ReferencePoolComponentSetting",
        menuName = "LFramework/Settings/ReferencePoolComponentSetting")]
    public class ReferencePoolComponentSetting : ComponentSetting
    {
        [SerializeField]
        private ReferenceStrictCheckType m_EnableStrictCheck = ReferenceStrictCheckType.AlwaysEnable;
    }
}