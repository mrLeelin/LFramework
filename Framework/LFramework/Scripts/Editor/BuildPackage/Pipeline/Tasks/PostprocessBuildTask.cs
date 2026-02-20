using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 后处理构建任务
    /// 在构建完成后调用所有 IBuildEventHandler 的 OnPostprocessBuildApp 方法
    /// </summary>
    public class PostprocessBuildTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Postprocess Build";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Execute postprocess build event handlers";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要后处理
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时执行后处理
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
                var handlers = context.EventHandlers;
                if (handlers == null || handlers.Count == 0)
                {
                    Debug.Log($"[PostprocessBuildTask] No event handlers to execute.");
                    return BuildTaskResult.CreateSuccess(TaskName);
                }

                Debug.Log($"[PostprocessBuildTask] Executing {handlers.Count} event handler(s)...");

                IBuildEventHandler.HandleList(handlers, (handler) =>
                {
                    Debug.Log($"[PostprocessBuildTask] Calling {handler.GetType().Name}.OnPostprocessBuildApp");
                   
                    handler.OnPostprocessBuildApp(context.BuildSetting,context.OutputFolder);
                });

                Debug.Log($"[PostprocessBuildTask] All event handlers executed successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Postprocess build failed: {ex.Message}");
            }
        }
    }
}
