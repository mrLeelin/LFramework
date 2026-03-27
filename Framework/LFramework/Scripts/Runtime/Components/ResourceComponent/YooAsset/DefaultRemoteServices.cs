#if YOOASSET_SUPPORT
using UnityGameFramework.Runtime;
using YooAsset;

namespace LFramework.Runtime
{
    public class DefaultRemoteServices : IRemoteServices
    {
        private readonly string _defaultHostServer;
        

        public DefaultRemoteServices(SettingComponent settingComponent,GameSetting gameSetting)
        {
            var url = gameSetting.GetCdnUrl();
            var version = gameSetting.GetResourceVersion(settingComponent);
            var appVersion = gameSetting.appVersion;
            var channel = gameSetting.channel;
            var cdnType = gameSetting.cdnType;
            var newUrl = $"{url}{channel}_{appVersion}_{cdnType}/{channel}_{version}_{cdnType}";
            Log.Info($"[DefaultRemoteServices] The new url is {newUrl}");
            _defaultHostServer = newUrl;
        }
        string IRemoteServices.GetRemoteMainURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
        string IRemoteServices.GetRemoteFallbackURL(string fileName)
        {
            return $"{_defaultHostServer}/{fileName}";
        }
    }
}
#endif