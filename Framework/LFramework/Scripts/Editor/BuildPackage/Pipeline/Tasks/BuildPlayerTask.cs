using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建玩家任务
    /// 完全独立实现，直接调用 Unity BuildPipeline.BuildPlayer()
    /// 不依赖任何 Builder 或 Strategy，通过 PlatformConfig 获取配置
    /// </summary>
    public class BuildPlayerTask : IBuildTask
    {
        public string TaskName => "Build Player";
        public string Description => "Build player using Unity BuildPipeline";

        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null || context.PlatformConfig == null)
            {
                return false;
            }

            return context.BuildSetting.buildType == BuildType.APP;
        }

        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildPlayerTask] Building player for {context.PlatformConfig.GetBuildTarget()}...");

                // 配置平台设置
                context.PlatformConfig.ConfigurePlatformSettings(context.BuildSetting);

                // 获取构建选项
                BuildPlayerOptions options = context.PlatformConfig.GetBuildPlayerOptions(context.BuildSetting);

                
                // 执行构建
                var report = BuildPipeline.BuildPlayer(options);

                if (report.summary.result != BuildResult.Succeeded)
                {
                    return BuildTaskResult.CreateFailed(TaskName, $"Build failed: {report.summary.result}");
                }
                
                Debug.Log($"[BuildPlayerTask] Player built successfully at: {options.locationPathName}");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build player failed: {ex.Message}");
            }
        }
    }
}
