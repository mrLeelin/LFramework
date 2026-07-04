using UnityEngine;

namespace LFramework.Runtime.Settings
{
    [CreateAssetMenu(order = 1, fileName = "NetworkComponentSetting",
        menuName = "LFramework/Settings/NetworkComponentSetting")]
    public sealed class NetworkComponentSetting : ComponentSetting
    {
        [SerializeField]
        private int _loginTimeOutSecond = 10;

        [SerializeField]
        private int _timeOutSecond = 10;

        [SerializeField]
        private string _networkChannelTypeName = null;
    }
}
