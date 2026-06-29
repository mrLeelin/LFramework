using System;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 版本检查任务的默认配置策略，保持框架默认启动流程行为。
    /// </summary>
    public class DefaultCheckVersionConfigProvider : ICheckVersionConfigProvider
    {
        /// <summary>
        /// 默认使用 GameSetting 中的版本地址和版本根目录拼接请求地址。
        /// </summary>
        public string GetVersionUrl(GameSetting gameSetting, SettingComponent settingComponent)
        {
            if (string.IsNullOrWhiteSpace(gameSetting.versionUrl))
            {
                return string.Empty;
            }

            return $"{gameSetting.versionUrl}/{gameSetting.GetVersionRootDir()}";
        }

        /// <summary>
        /// 默认将远端 JSON 解析为 BaseGameVersionConfig。
        /// </summary>
        public IGameVersionConfig ParseRemoteGameVersion(string jsonStr)
        {
            var baseConfig = JsonUtility.FromJson<BaseGameVersionConfig>(jsonStr);
            if (baseConfig == null)
            {
                Log.Error("[CheckVersionTask] The parse game version error.");
            }

            return baseConfig;
        }

        /// <summary>
        /// 默认使用 GameSetting 的应用版本和本地保存的资源版本构建客户端版本配置。
        /// </summary>
        public IGameVersionConfig BuildLocalGameVersion(GameSetting gameSetting, SettingComponent settingComponent)
        {
            return new BaseGameVersionConfig
            {
                resourceVersion = gameSetting.GetResourceVersion(settingComponent),
                appVersion = gameSetting.appVersion
            };
        }

        /// <summary>
        /// 默认比较规则：应用版本更高时强更，应用版本一致且资源版本更高时热更。
        /// </summary>
        public (GameVersionCompareResult result, string errorMessage) CompareGameVersion(
            IGameVersionConfig remote,
            IGameVersionConfig client)
        {
            if (remote == null || client == null)
            {
                return (GameVersionCompareResult.Invalid, "remote or client is null");
            }

            if (string.IsNullOrEmpty(remote.AppVersion) || string.IsNullOrEmpty(client.AppVersion))
            {
                return (GameVersionCompareResult.Invalid, "remote or client appVersion is null");
            }

            if (string.IsNullOrEmpty(remote.ResourceVersion) ||
                string.IsNullOrEmpty(client.ResourceVersion))
            {
                return (GameVersionCompareResult.Invalid, "remote or client resourceVersion is null");
            }

            var clientAppVersion = new Version(client.AppVersion);
            var remoteAppVersion = new Version(remote.AppVersion);
            if (remoteAppVersion > clientAppVersion)
            {
                return (GameVersionCompareResult.ForceUpdate, string.Empty);
            }

            if (remoteAppVersion < clientAppVersion)
            {
                return (GameVersionCompareResult.NoUpdate, string.Empty);
            }

            var clientResVersion = new Version(client.ResourceVersion);
            var remoteResVersion = new Version(remote.ResourceVersion);
            if (remoteResVersion > clientResVersion)
            {
                return (GameVersionCompareResult.Update, string.Empty);
            }

            return (GameVersionCompareResult.NoUpdate, string.Empty);
        }

        /// <summary>
        /// 默认将资源版本、CDN 和服务器地址写回 GameSetting。
        /// </summary>
        public void ApplyRemoteGameVersion(
            IGameVersionConfig remote,
            GameSetting gameSetting,
            SettingComponent settingComponent)
        {
            gameSetting.SetResourceVersion(settingComponent, remote.ResourceVersion);
            if (remote is IGameVersionEndpointConfig endpointConfig)
            {
                gameSetting.ip = endpointConfig.LogicIp;
                gameSetting.webSocketIp = endpointConfig.WebSocketIp;
                gameSetting.cdnUrl = endpointConfig.CdnUrl;
            }

            Log.Info("[CheckVersionTask] 更新 GameSetting 完成: {0}", gameSetting);
        }
    }
}
