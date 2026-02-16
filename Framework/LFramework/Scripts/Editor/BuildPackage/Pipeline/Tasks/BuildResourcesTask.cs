using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建资源任务
    /// 完全独立实现，调用 BuildResourcesService 构建游戏资源
    /// 符合 Task 架构原则：Task 负责编排，Service 负责逻辑
    /// </summary>
    public class BuildResourcesTask : IBuildTask
    {
        public string TaskName => "Build Resources";
        public string Description => "Build game resources using BuildResourcesService";

        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在需要构建资源时执行
            return context.BuildSetting.isBuildResources;
        }

        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildResourcesTask] Building resources...");

                // 直接使用 BuildSetting
                var buildSetting = context.BuildSetting;
                if (buildSetting == null)
                {
                    return BuildTaskResult.CreateFailed(TaskName, "BuildSetting is null.");
                }

                // 调用预处理事件
                var handlers = context.EventHandlers;
                if (handlers != null && handlers.Count > 0)
                {
                    Debug.Log($"[BuildResourcesTask] Calling {handlers.Count} preprocess event handler(s)...");
                    IBuildEventHandler.HandleList(handlers, (handler) =>
                    {
                        Debug.Log($"[BuildResourcesTask] Calling {handler.GetType().Name}.OnPreprocessBuildResources");
                        handler.OnPreprocessBuildResources(buildSetting);
                    });
                }

                // 构建资源 - 使用 BuildResourcesService
                Debug.Log($"[BuildResourcesTask] Executing BuildResourcesService.Build...");
                BuildResourcesService.Build(buildSetting);

                // 调用后处理事件
                if (handlers != null && handlers.Count > 0)
                {
                    Debug.Log($"[BuildResourcesTask] Calling {handlers.Count} postprocess event handler(s)...");
                    IBuildEventHandler.HandleList(handlers, (handler) =>
                    {
                        Debug.Log($"[BuildResourcesTask] Calling {handler.GetType().Name}.OnPostprocessBuildResources");
                        handler.OnPostprocessBuildResources(buildSetting);
                    });
                }

                Debug.Log($"[BuildResourcesTask] Resources built successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build resources failed: {ex.Message}");
            }
        }
    }
}
