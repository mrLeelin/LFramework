namespace LFramework.Runtime
{
    /// <summary>
    /// 版本检查结果类型
    /// </summary>
    public enum VersionCheckResultType
    {
        /// <summary>
        /// 不需要更新
        /// </summary>
        NoUpdate,

        /// <summary>
        /// 热更资源
        /// </summary>
        HotUpdate,

        /// <summary>
        /// 强制更新整包（需要去商店下载新版本）
        /// </summary>
        ForceUpdate,

        /// <summary>
        /// 检查失败
        /// </summary>
        Failed,
    }

    /// <summary>
    /// 版本检查结果，由 IVersionCheckProvider 返回
    /// </summary>
    public class VersionCheckResult
    {
        /// <summary>
        /// 检查结果类型
        /// </summary>
        public VersionCheckResultType ResultType { get; set; }

        /// <summary>
        /// 强更时的下载链接
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 远端资源版本号
        /// </summary>
        public string RemoteResourceVersion { get; set; }

        /// <summary>
        /// 错误信息（ResultType 为 Failed 时有值）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 项目自定义数据（比如服务器 IP、CDN 地址等，由项目层自行解析使用）
        /// </summary>
        public object CustomData { get; set; }

        public static VersionCheckResult Success(VersionCheckResultType type,
            string remoteResourceVersion = null, object customData = null)
        {
            return new VersionCheckResult
            {
                ResultType = type,
                RemoteResourceVersion = remoteResourceVersion,
                CustomData = customData,
            };
        }

        public static VersionCheckResult ForceUpdateResult(string downloadUrl, object customData = null)
        {
            return new VersionCheckResult
            {
                ResultType = VersionCheckResultType.ForceUpdate,
                DownloadUrl = downloadUrl,
                CustomData = customData,
            };
        }

        public static VersionCheckResult Failure(string errorMessage)
        {
            return new VersionCheckResult
            {
                ResultType = VersionCheckResultType.Failed,
                ErrorMessage = errorMessage,
            };
        }
    }
}
