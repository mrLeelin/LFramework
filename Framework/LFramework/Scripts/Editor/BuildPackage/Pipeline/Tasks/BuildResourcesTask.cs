using System.Linq;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建资源任务
    /// 构建游戏资源,包括调用事件处理器的预处理和后处理方法
    /// </summary>
    public class BuildResourcesTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Build Resources";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Build game resources with pre/post processing";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在需要构建资源时执行
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在需要构建资源时执行
            return context.BuildSetting.isBuildResources;
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
                Debug.Log($"[BuildResourcesTask] Building resources...");

                // 创建 BuildResourcesData
                var buildResourcesData = CreateBuildResourcesData(context.BuildSetting);
                context.BuildResourcesData = buildResourcesData;

                // 调用预处理事件
                var handlers = context.EventHandlers;
                if (handlers != null && handlers.Count > 0)
                {
                    Debug.Log($"[BuildResourcesTask] Calling {handlers.Count} preprocess event handler(s)...");
                    IBuildEventHandler.HandleList(handlers, (handler) =>
                    {
                        Debug.Log($"[BuildResourcesTask] Calling {handler.GetType().Name}.OnPreprocessBuildResources");
                        handler.OnPreprocessBuildResources(buildResourcesData);
                    });
                }

                // 构建资源
                Debug.Log($"[BuildResourcesTask] Executing BuildResourcesData.Build...");
                BuildResourcesData.Build(buildResourcesData);

                // 调用后处理事件
                if (handlers != null && handlers.Count > 0)
                {
                    Debug.Log($"[BuildResourcesTask] Calling {handlers.Count} postprocess event handler(s)...");
                    IBuildEventHandler.HandleList(handlers, (handler) =>
                    {
                        Debug.Log($"[BuildResourcesTask] Calling {handler.GetType().Name}.OnPostprocessBuildResources");
                        handler.OnPostprocessBuildResources(buildResourcesData);
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

        /// <summary>
        /// 创建 BuildResourcesData
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <returns>构建资源数据</returns>
        private BuildResourcesData CreateBuildResourcesData(BuildSetting buildSetting)
        {
            var buildResourcesData = new BuildResourcesData
            {
                BuilderTarget = buildSetting.builderTarget,
                IOSChannel = buildSetting.iosChannel,
                WindowsChannel = buildSetting.windowsChannel,
                AndroidChannel = buildSetting.androidChannel,
                IsResourcesBuildIn = buildSetting.isResourcesBuildIn,
                ResourcesVersion = buildSetting.resourcesVersion,
                BuildResourcesServerModel = (BuildResourcesServerModel)buildSetting.cdnType,
                BuildType = buildSetting.buildType,
                IsForceUpdate = buildSetting.isForceUpdate,
                IsBuildDll = buildSetting.isBuildDll,
                AppVersion = buildSetting.appVersion + "." + buildSetting.versionCode
            };

            return buildResourcesData;
        }
    }
}
