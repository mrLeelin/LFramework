namespace LFramework.Runtime
{
    /// <summary>
    /// 版本检查提供者接口，由项目层实现
    /// 框架层只负责流程调度，具体的 URL 拼接、数据解析、版本对比逻辑由项目层决定
    /// </summary>
    public interface IVersionCheckProvider
    {
        /// <summary>
        /// 获取版本检查的请求 URL
        /// 项目层决定 URL 怎么拼（加设备 ID、渠道号、当前版本等）
        /// </summary>
        string GetVersionCheckUrl();

        /// <summary>
        /// 解析服务器返回的版本数据并判断更新类型
        /// 项目层决定：JSON 怎么解析、白名单怎么判断、版本号怎么对比
        /// </summary>
        /// <param name="responseBytes">服务器返回的原始字节数据</param>
        /// <returns>版本检查结果</returns>
        VersionCheckResult ParseAndCheck(byte[] responseBytes);

        /// <summary>
        /// 版本检查成功后的回调，用于保存远端配置（如服务器 IP、CDN 地址等）
        /// </summary>
        /// <param name="result">版本检查结果</param>
        void OnVersionCheckCompleted(VersionCheckResult result);
    }
}
