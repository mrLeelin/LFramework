using System;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 框架默认平台注册提供者。
    /// 当项目未提供自定义 Provider 时，使用框架内置的平台配置映射。
    /// </summary>
    public sealed class DefaultPlatformConfigRegistryProvider : IPlatformConfigRegistryProvider
    {
        /// <summary>
        /// 默认 Provider 名称。
        /// </summary>
        public string ProviderName => nameof(DefaultPlatformConfigRegistryProvider);

        /// <summary>
        /// 默认 Provider 优先级最低，供项目侧覆盖。
        /// </summary>
        public int Priority => 0;

        /// <summary>
        /// 默认 Provider 始终可用。
        /// </summary>
        public bool IsActive => true;

        /// <summary>
        /// 判断默认 Provider 是否支持指定平台。
        /// </summary>
        /// <param name="builderTarget">构建目标。</param>
        public bool Supports(BuilderTarget builderTarget)
        {
            return builderTarget == BuilderTarget.Windows ||
                   builderTarget == BuilderTarget.Android ||
                   builderTarget == BuilderTarget.iOS;
        }

        /// <summary>
        /// 创建框架默认平台配置实现。
        /// </summary>
        /// <param name="builderTarget">构建目标。</param>
        /// <param name="buildSetting">构建设置。</param>
        public IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
        {
            if (buildSetting == null)
            {
                throw new ArgumentNullException(nameof(buildSetting));
            }

            switch (builderTarget)
            {
                case BuilderTarget.Windows:
                    return new WindowsPlatformConfig(buildSetting);
                case BuilderTarget.Android:
                    return new AndroidPlatformConfig(buildSetting);
                case BuilderTarget.iOS:
                    return new iOSPlatformConfig(buildSetting);
                default:
                    throw new ArgumentOutOfRangeException(nameof(builderTarget), builderTarget,
                        $"Unsupported builder target: {builderTarget}");
            }
        }
    }
}
