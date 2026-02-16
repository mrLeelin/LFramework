using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建前处理任务
    /// 完全独立实现，执行构建前的准备工作
    /// 不依赖任何 Builder 或 Strategy
    /// </summary>
    public class BuildBeforeTask : IBuildTask
    {
        public string TaskName => "Build Before";
        public string Description => "Execute build before processing";

        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            return context.BuildSetting.buildType == BuildType.APP;
        }

        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildBeforeTask] Executing build before processing...");

                // 构建前处理逻辑（如果需要）
                // 大部分平台特定的配置已经在 PlatformConfig.ConfigurePlatformSettings() 中处理

                Debug.Log($"[BuildBeforeTask] Build before processing completed.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build before processing failed: {ex.Message}");
            }
        }
    }
}
