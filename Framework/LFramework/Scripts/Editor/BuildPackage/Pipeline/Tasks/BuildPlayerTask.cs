using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建玩家任务
    /// 调用 BaseBuilder 的 BuildInternal 方法执行实际的玩家构建
    /// </summary>
    public class BuildPlayerTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Build Player";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Execute platform-specific player build";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要构建玩家
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时构建玩家
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
                Debug.Log($"[BuildPlayerTask] Building player...");

                // 通过反射调用 BaseBuilder 的 BuildInternal 方法
                var builder = context.Builder;
                var method = builder.GetType().GetMethod("BuildInternal",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (method == null)
                {
                    return BuildTaskResult.CreateFailed(TaskName, "BuildInternal method not found in BaseBuilder.");
                }

                method.Invoke(builder, null);

                Debug.Log($"[BuildPlayerTask] Player built successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build player failed: {ex.Message}");
            }
        }
    }
}
