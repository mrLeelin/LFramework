using System;

namespace LFramework.Runtime
{
    public enum GameVersionCompareResult
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
        /// 无效配置或比较异常
        /// </summary>
        Invalid,
    }

    /// <summary>
    /// 游戏版本
    /// </summary>
    public interface IGameVersionConfig
    {
        /// <summary>
        /// App版本
        /// </summary>
        public string AppVersion { get; }

        /// <summary>
        /// 资源版本
        /// </summary>
        public string ResourceVersion { get; }
    }

    /// <summary>
    /// 版本配置携带的运行时服务端点。
    /// </summary>
    public interface IGameVersionEndpointConfig
    {
        /// <summary>
        /// CDN 更新地址。
        /// </summary>
        public string CdnUrl { get; }

        /// <summary>
        /// 逻辑服 IP。
        /// </summary>
        public string LogicIp { get; }

        /// <summary>
        /// WebSocket IP。
        /// </summary>
        public string WebSocketIp { get; }
    }

    [System.Serializable]
    public class BaseGameVersionConfig : IGameVersionConfig, IGameVersionEndpointConfig
    {
        public string appVersion;
        public string resourceVersion;
        public string cdnUrl;
        public string logicIp;
        public string webSocketIp;


        public string AppVersion => appVersion;
        public string ResourceVersion => resourceVersion;
        public string CdnUrl => cdnUrl;
        public string LogicIp => logicIp;
        public string WebSocketIp => webSocketIp;


        public override string ToString()
        {
            return
                $"AppVersion:{appVersion}\n" +
                $"ResourceVersion: {resourceVersion}\n" +
                $"CdnUrl: {cdnUrl}\n" +
                $"LogicIp: {logicIp}\n" +
                $"WebSocketIp: {webSocketIp}";
        }
    }
}
