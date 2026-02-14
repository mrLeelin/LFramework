
/**

*********************************************************************
Author:              LFramework.Runtime
CreateTime:          20:07:58

*********************************************************************
**/

using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "WebRequestComponentSetting",
        menuName = "LFramework/Settings/WebRequestComponentSetting")]
    public sealed class WebRequestComponentSetting : ComponentSetting
    {
        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_WebRequestAgentHelperTypeName = "UnityGameFramework.Runtime.UnityWebRequestAgentHelper";

        [SerializeField]
        private WebRequestAgentHelperBase m_CustomWebRequestAgentHelper = null;

        [SerializeField]
        private int m_WebRequestAgentHelperCount = 1;

        [SerializeField]
        private float m_Timeout = 30f;
    }
}