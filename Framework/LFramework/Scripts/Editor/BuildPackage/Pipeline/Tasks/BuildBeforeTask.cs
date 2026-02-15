using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建前处理任务
    /// 调用 BaseBuilder 的 BuildBeforeInternal 方法执行平台特定的构建前处理
    /// </summary>
    public class BuildBeforeTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Build Before";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Execute platform-specific build before processing";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要执行构建前处理
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时执行构建前处理
            return context.BuildSetting.buildType == BuildType.APP;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>任务执行结果</returns>
        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildBeforeTask] Executing platform-specific build before processing...");

                // 通过反射调用 BaseBuilder 的 BuildBeforeInternal 方法
                var builder = context.Builder;
                var method = builder.GetType().GetMethod("BuildBeforeInternal",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (method != null)
                {
                    method.Invoke(builder, null);
                    Debug.Log($"[BuildBeforeTask] BuildBeforeInternal executed successfully.");
                }
                else
                {
                    Debug.Log($"[BuildBeforeTask] BuildBeforeInternal method not found, skipping.");
                }

                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build before processing failed: {ex.Message}");
            }
        }
    }
}
