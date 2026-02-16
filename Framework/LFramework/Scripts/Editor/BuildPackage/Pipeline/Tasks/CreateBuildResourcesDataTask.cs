using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 创建构建资源数据任务
    /// 在构建流程开始时创建 BuildResourcesData 并放入 Context
    /// 供后续的 BuildDllTask 和 BuildResourcesTask 使用
    /// </summary>
    public class CreateBuildResourcesDataTask : IBuildTask
    {
        public string TaskName => "Create Build Resources Data";
        public string Description => "Create BuildResourcesData and add to context";

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
                Debug.Log($"[CreateBuildResourcesDataTask] Creating BuildResourcesData...");

                // 创建 BuildResourcesData
                var buildResourcesData = new BuildResourcesData
                {
                    BuilderTarget = context.BuildSetting.builderTarget,
                    IOSChannel = context.BuildSetting.iosChannel,
                    WindowsChannel = context.BuildSetting.windowsChannel,
                    AndroidChannel = context.BuildSetting.androidChannel,
                    IsResourcesBuildIn = context.BuildSetting.isResourcesBuildIn,
                    ResourcesVersion = context.BuildSetting.resourcesVersion,
                    BuildResourcesServerModel = (BuildResourcesServerModel)context.BuildSetting.cdnType,
                    BuildType = context.BuildSetting.buildType,
                    IsForceUpdate = context.BuildSetting.isForceUpdate,
                    IsBuildDll = context.BuildSetting.isBuildDll,
                    AppVersion = context.BuildSetting.appVersion + "." + context.BuildSetting.versionCode
                };

                // 放入 Context
                context.BuildResourcesData = buildResourcesData;

                Debug.Log($"[CreateBuildResourcesDataTask] BuildResourcesData created successfully.");
                Debug.Log($"[CreateBuildResourcesDataTask] - BuilderTarget: {buildResourcesData.BuilderTarget}");
                Debug.Log($"[CreateBuildResourcesDataTask] - IsBuildDll: {buildResourcesData.IsBuildDll}");
                Debug.Log($"[CreateBuildResourcesDataTask] - ResourceSystem: {buildResourcesData.ResourceSystem}");

                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (System.Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Create BuildResourcesData failed: {ex.Message}");
            }
        }
    }
}
