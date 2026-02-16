using UnityEditor;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 平台配置接口
    /// 只负责提供平台特定的配置信息，不包含构建逻辑
    /// 构建逻辑由 Task 独立实现
    /// </summary>
    public interface IPlatformConfig
    {
        /// <summary>
        /// 获取构建目标平台
        /// </summary>
        BuildTarget GetBuildTarget();

        /// <summary>
        /// 获取构建目标组
        /// </summary>
        BuildTargetGroup GetBuildTargetGroup();

        /// <summary>
        /// 获取构建选项
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        BuildPlayerOptions GetBuildPlayerOptions(BuildSetting buildSetting);

        /// <summary>
        /// 配置平台特定的 PlayerSettings
        /// 在构建前调用，设置平台相关的配置
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        void ConfigurePlatformSettings(BuildSetting buildSetting);

        /// <summary>
        /// 获取输出路径
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        string GetOutputPath(BuildSetting buildSetting);

        /// <summary>
        /// 获取构建文件夹路径
        /// </summary>
        string GetBuildFolderPath();
    }
}
