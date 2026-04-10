namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 平台配置注册提供者。
    /// 由单个 Provider 统一决定项目内所有 BuilderTarget 对应的 IPlatformConfig 映射关系。
    /// </summary>
    public interface IPlatformConfigRegistryProvider
    {
        /// <summary>
        /// Provider 名称，用于日志和错误提示。
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Provider 优先级，数值越大优先级越高。
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 当前 Provider 是否处于激活状态。
        /// 只有激活的 Provider 才会参与平台注册选择。
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// 当前 Provider 是否支持指定构建目标。
        /// </summary>
        /// <param name="builderTarget">构建目标。</param>
        bool Supports(BuilderTarget builderTarget);

        /// <summary>
        /// 为指定构建目标创建平台配置。
        /// </summary>
        /// <param name="builderTarget">构建目标。</param>
        /// <param name="buildSetting">构建设置。</param>
        IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting);
    }
}
