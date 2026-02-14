


using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "DownloadComponentSetting",
        menuName = "LFramework/Settings/DownloadComponentSetting")]
    public class DownloadComponentSetting  : ComponentSetting
    {
        [SerializeField]
        private Transform m_InstanceRoot = null;

        [SerializeField]
        private string m_DownloadAgentHelperTypeName = "UnityGameFramework.Runtime.UnityWebRequestDownloadAgentHelper";

        [SerializeField]
        private DownloadAgentHelperBase m_CustomDownloadAgentHelper = null;

        [SerializeField]
        private int m_DownloadAgentHelperCount = 3;

        [SerializeField]
        private float m_Timeout = 30f;

        [SerializeField]
        private int m_FlushSize =  1024 * 1024;
    }
}