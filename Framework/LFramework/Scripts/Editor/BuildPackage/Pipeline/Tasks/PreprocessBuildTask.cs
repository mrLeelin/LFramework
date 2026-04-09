using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 预处理构建任务
    /// 在构建开始前调用所有 IBuildEventHandler 的 OnPreprocessBuildApp 方法
    /// </summary>
    public class PreprocessBuildTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Preprocess Build";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Execute preprocess build event handlers";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要预处理
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时执行预处理
            return context.BuildSetting.buildType == BuildType.App;
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
                var handlers = context.EventHandlers;
                if (handlers == null || handlers.Count == 0)
                {
                    Debug.Log($"[PreprocessBuildTask] No event handlers to execute.");
                    return BuildTaskResult.CreateSuccess(TaskName);
                }

                Debug.Log($"[PreprocessBuildTask] Executing {handlers.Count} event handler(s)...");

                IBuildEventHandler.HandleList(handlers, (handler) =>
                {
                    Debug.Log($"[PreprocessBuildTask] Calling {handler.GetType().Name}.OnPreprocessBuildApp");
                    handler.OnPreprocessBuildApp(context.BuildSetting);
                });

                Debug.Log($"[PreprocessBuildTask] All event handlers executed successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Preprocess build failed: {ex.Message}");
            }
        }
    }
}
