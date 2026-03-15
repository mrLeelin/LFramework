namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 版本检查结果类型
    /// </summary>
    public enum VersionCheckResultType
    {
        /// <summary>
        /// 无需更新
        /// </summary>
        NoUpdate,

        /// <summary>
        /// 热更新（资源更新）
        /// </summary>
        HotUpdate,

        /// <summary>
        /// 强制更新（需要下载新包）
        /// </summary>
        ForceUpdate,

        /// <summary>
        /// 检查失败
        /// </summary>
        Failed
    }

    /// <summary>
    /// 版本检查结果，在任务之间传递版本检查状态
    /// </summary>
    public class VersionCheckResult
    {
        /// <summary>
        /// 结果类型
        /// </summary>
        public VersionCheckResultType ResultType { get; set; }

        /// <summary>
        /// 错误信息（仅在 Failed 时有效）
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 强更下载地址（仅在 ForceUpdate 时有效）
        /// </summary>
        public string DownloadUrl { get; set; }

        /// <summary>
        /// 远程版本信息
        /// </summary>
        public GameVersion RemoteVersion { get; set; }

        /// <summary>
        /// 匹配的远程版本配置（考虑白名单后的最终配置）
        /// </summary>
        public GameVersionConfig RemoteVersionConfig { get; set; }
    }
}
