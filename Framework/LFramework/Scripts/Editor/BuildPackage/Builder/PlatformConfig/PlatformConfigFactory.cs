using System;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 平台配置工厂
    /// 根据构建目标创建对应的平台配置
    /// </summary>
    public static class PlatformConfigFactory
    {
        /// <summary>
        /// 创建平台配置
        /// </summary>
        /// <param name="builderTarget">构建目标平台</param>
        /// <param name="buildSetting">构建设置</param>
        /// <returns>平台配置实例</returns>
        public static IPlatformConfig CreateConfig(BuilderTarget builderTarget, BuildSetting buildSetting)
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
                    throw new ArgumentException($"Unsupported builder target: {builderTarget}", nameof(builderTarget));
            }
        }

        /// <summary>
        /// 检查指定的构建目标是否受支持
        /// </summary>
        public static bool IsSupported(BuilderTarget builderTarget)
        {
            return builderTarget == BuilderTarget.Windows ||
                   builderTarget == BuilderTarget.Android ||
                   builderTarget == BuilderTarget.iOS;
        }

        /// <summary>
        /// 获取构建目标的显示名称
        /// </summary>
        public static string GetDisplayName(BuilderTarget builderTarget)
        {
            switch (builderTarget)
            {
                case BuilderTarget.Windows:
                    return "Windows Standalone";
                case BuilderTarget.Android:
                    return "Android";
                case BuilderTarget.iOS:
                    return "iOS";
                default:
                    return "Unknown";
            }
        }
    }
}
