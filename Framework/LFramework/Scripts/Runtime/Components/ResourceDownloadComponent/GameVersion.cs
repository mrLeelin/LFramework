using System;

namespace LFramework.Runtime
{
    public enum Result
    {
        None,

        /// <summary>
        /// 热更更新
        /// </summary>
        Update,

        /// <summary>
        /// 强更包
        /// </summary>
        ForceUpdate,

        /// <summary>
        /// 不需要更新
        /// </summary>
        NoUpdate,

        /// <summary>
        /// 错误
        /// </summary>
        Exception,
    }

    [System.Serializable]
    public class GameVersionConfig
    {
        /// <summary>
        /// 资源版本
        /// </summary>
        public string resourceVersion;

        /// <summary>
        /// Cdn更新文件
        /// </summary>
        public string cdnUrl;

        /// <summary>
        /// 逻辑服务器ip
        /// </summary>
        public string logicIp;

        /// <summary>
        /// WebSocket服务器ip
        /// </summary>
        public string webSocketIp;

        public override string ToString()
        {
            return
                $"ResourceVersion: {resourceVersion}\n" +
                $"CdnUrl: {cdnUrl}\n" +
                $"LogicIp: {logicIp}\n" +
                $"WebSocketIp: {webSocketIp}";
        }
    }


    [System.Serializable]
    public class GameVersion
    {
        /// <summary>
        /// app 版本
        /// </summary>
        public string appVersion;

        /// <summary>
        /// 下载包
        /// </summary>
        public string downloadPackage;

        /// <summary>
        /// 默认配置文件
        /// </summary>
        public GameVersionConfig defaultConfig;

        /// <summary>
        /// 白名单配置文件
        /// </summary>
        public GameVersionConfig whiteListConfig;

        /// <summary>
        /// 白名单
        /// </summary>
        public string userList;


        public override string ToString()
        {
            return
                $"AppVersion: {appVersion}\n" +
                $"DownloadPackage: {downloadPackage}\n" +
                $"DefaultConfig: {defaultConfig}\n" +
                $"WhiteListConfig: {whiteListConfig}\n" +
                $"UserList: {userList}";
        }

        public static (Result result, string errorMessage) IsNeedUpdate(GameVersion remote, GameVersion client,GameVersionConfig remoteConfig,GameVersionConfig clientConfig)
        {
            if (remote == null || client == null)
            {
                return (Result.Exception, "remote or client is null");
            }


            if (string.IsNullOrEmpty(remote.appVersion) || string.IsNullOrEmpty(client.appVersion))
            {
                return (Result.Exception, "remote or client appVersion is null");
            }

            if (string.IsNullOrEmpty(remoteConfig.resourceVersion) || string.IsNullOrEmpty(clientConfig.resourceVersion))
            {
                return (Result.Exception, "remote or client resourceVersion is null");
            }

            var clientAppVersion = new Version(client.appVersion);
            var remoteAppVersion = new Version(remote.appVersion);
            if (remoteAppVersion != clientAppVersion)
            {
                return (Result.ForceUpdate, "");
            }

            var clientResVersion = new Version(clientConfig.resourceVersion);
            var remoteResVersion = new Version(remoteConfig.resourceVersion);
            if (remoteResVersion > clientResVersion)
            {
                return (Result.Update, "");
            }


            return (Result.NoUpdate, "");
        }
    }
}