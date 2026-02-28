using System;
using System.Linq;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{

    [System.Serializable]
    public class VersionStorage
    {
        public string appVersion;
        public string resVersion;

        public static VersionStorage Build(GameSetting setting)
        {
            return new VersionStorage()
            {
                appVersion = setting.appVersion,
                resVersion = setting.resourceVersion
            };
        }
    }

    [CreateAssetMenu(order = 1, fileName = "GameSetting",
        menuName = "LFramework/HybridCLR/GameSetting")]
    public class GameSetting : ScriptableObject
    {

#if UNITY_EDITOR
        
        [ButtonGroup]
        [Button("172.16.1.96")]
        public void SetHpfServer()
        {
            ip = "http://172.16.1.96:10031";
            webSocketIp = "ws://172.16.1.96:8085/ws";
        }
        [ButtonGroup]
        [Button("172.16.0.190")]
        public void SetYenanServer()
        {
            ip = "http://172.16.0.190:8010";
            webSocketIp = "ws://172.16.0.190:8085/ws";
        }
        [ButtonGroup]
        [Button("172.16.1.141")]
        public void SetHJServer()
        {
            ip = "http://172.16.1.141:10031";
            webSocketIp = "ws://172.16.1.141:8085/ws";
        } 
        [ButtonGroup]
        [Button("DebugServer")]
        public void SetDebugServer()
        {
            ip = "https://test-game.partygamesvc.com";
            webSocketIp = "wss://test-im.partygamesvc.com/ws";
        } 
#endif
        
        
        #region Runtime
        [HideInInspector]
        public HybridCLRSetting hybridClrSetting;
        public bool isRelease;

        /// <summary>
        /// 资源是否在包内
        /// </summary>
        public bool isResourcesBuildIn;

        [FoldoutGroup("Network")] public string versionUrl;
        [FoldoutGroup("Network")] public string ip;
        [FoldoutGroup("Network")] public string webSocketIp;

        [FoldoutGroup("Version")] public string appVersion;
        [FoldoutGroup("Version")] public string resourceVersion;
        [FoldoutGroup("Cdn")] public CdnType cdnType;
        [FoldoutGroup("Cdn")] public string channel;
        [FoldoutGroup("Cdn")] public  string cdnUrl;

        #endregion


        public string GetResourceVersion(SettingComponent settingComponent)
        {
#if UNITY_EDITOR
            return resourceVersion;
#endif
            const string versionKey = "ResourceVersionNew";
            if (!settingComponent.HasSetting(versionKey))
            {
                return resourceVersion;
            }

            //检测一下
            var version = settingComponent.GetObject<VersionStorage>(versionKey, new VersionStorage());
            if (version.appVersion.Split('.').Length != 4 || version.resVersion.Split('.').Length != 4)
            {
                settingComponent.SetObject(versionKey, VersionStorage.Build(this));
                Log.Error("The version format error, please check the version. 'appVersion' {0} 'resVersion' {1}",
                    version.appVersion, version.resVersion);
                return resourceVersion;
            }

            var storeAppVersion = new Version(version.appVersion);
            var pkgAppVersion = new Version(appVersion);
            if (pkgAppVersion != storeAppVersion)
            {
                settingComponent.SetObject(versionKey, VersionStorage.Build(this));
                Log.Info("The app version error, please check the version. 'appVersion' {0} 'resVersion' {1}",
                    version.appVersion, version.resVersion);
                return resourceVersion;
            }

            var storeVersion = new Version(version.resVersion);
            var pkgVersion = new Version(resourceVersion);
            if (pkgVersion > storeVersion)
            {
                settingComponent.SetObject(versionKey, VersionStorage.Build(this));
                Log.Info("The resource version error, please check the version. 'appVersion' {0} 'resVersion' {1}",
                    version.appVersion, version.resVersion);
                return resourceVersion;
            }

            return version.resVersion;
        }

        public string SetResourceVersion(SettingComponent settingComponent, string newResourceVersion)
        {
#if UNITY_EDITOR
            resourceVersion = newResourceVersion;
            return resourceVersion;
#endif
            const string versionKey = "ResourceVersionNew";
            resourceVersion = newResourceVersion;
            settingComponent.SetObject(versionKey, VersionStorage.Build(this));
            return newResourceVersion;
        }

        public string GetCdnUrl() 
        {
            return cdnUrl;
            /*
            if (cdnUrl.TryGetValue(cdnType, out var url))
            {
                return url;
            }

            Log.Fatal($"The cdn url error, type '{cdnType}'");
            return null;
            */
        }

        public string GetVersionRootDir()
        {
            return $"{channel}_{cdnType}/Version_{appVersion}";
        }


        public override string ToString()
        {
            return $"GameSetting:\n" +
                   $"- IsRelease: {isRelease}\n" +
                   $"- IsResourcesBuildIn: {isResourcesBuildIn}\n" +
                   $"- IP: {ip}\n" +
                   $"- WebSocketIP: {webSocketIp}\n" +
                   $"- AppVersion: {appVersion}\n" +
                   $"- ResourceVersion: {resourceVersion}\n" +
                   $"- CDN Type: {cdnType}\n" +
                   $"- Channel: {channel}\n" +
                   $"- CDN URLs: {cdnUrl}\n";
        }
    }
}