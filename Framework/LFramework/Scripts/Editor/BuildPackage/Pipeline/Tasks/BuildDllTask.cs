using System;
using System.Linq;
using LFramework.Editor.Builder.HybridCLR;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建 DLL 任务
    /// 完全独立实现，直接调用 BuildDllsHelper 构建热更新 DLL
    /// 不依赖任何 Builder 或 Strategy
    /// </summary>
    public class BuildDllTask : IBuildTask
    {
        public string TaskName => "Build DLL";
        public string Description => "Build hot-fix DLL files using BuildDllsHelper";

        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 旗舰版资源更新需要校验 DHE 资源与包的模式一致性，即使本次不重新编译 DLL。
#if LFRAMEWORK_HYBRIDCLR_DHE
            if (HybridCLRBuildWorkflow.IsDheEdition)
            {
                return context.BuildSetting.buildType == BuildType.ResourcesUpdate ||
                       context.BuildSetting.isbuildDll;
            }
#endif
            return context.BuildSetting.isbuildDll;
        }

        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildDllTask] Building hot-fix DLL files...");

                var buildSetting = context.BuildSetting;

#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
                var hybridClrSetting = SettingManager.GetSetting<HybridCLRSetting>();
#if LFRAMEWORK_HYBRIDCLR_DHE
                const bool useDhe = true;
                HybridCLRBuildWorkflow.EnsureDheAvailable();
#else
                const bool useDhe = false;
#endif
                if (buildSetting.buildType == BuildType.ResourcesUpdate)
                {
                    if (hybridClrSetting == null)
                    {
                        return BuildTaskResult.CreateFailed(
                            TaskName, "HybridCLRSetting asset is missing for resource update.");
                    }

                    if (!buildSetting.isBuildResources)
                    {
                        return BuildTaskResult.CreateFailed(
                            TaskName, "A resource update must enable resource building.");
                    }

                    if (useDhe && !buildSetting.isbuildDll)
                    {
                        return BuildTaskResult.CreateFailed(
                            TaskName,
                            "A DHE resource update must rebuild DLLs and MetaVersion artifacts.");
                    }
                }

                if (!buildSetting.isbuildDll)
                {
                    Debug.Log($"[BuildDllTask] Hot-fix DLL files successfully.");
                    return BuildTaskResult.CreateSuccess(TaskName);
                }

                // 获取备份路径
                string backupPath = GetBackupPath(buildSetting);
                BuildTarget target = context.PlatformConfig.GetBuildTarget();

                // 构建 DLL
                if (!BuildDllsHelper.BuildDll(buildSetting.buildType == BuildType.App, backupPath, target))
                {
                    return BuildTaskResult.CreateFailed(TaskName, "Build DLL failed.");
                }

                Debug.Log($"[BuildDllTask] DLL built successfully, refreshing AssetDatabase...");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                // 复制 DLL
                if (!BuildDllsHelper.CopyDll(
                        buildSetting,
                        SettingManager.GetSetting<HybridCLRSetting>(),
                        backupPath,
                        target))
                {
                    return BuildTaskResult.CreateFailed(TaskName, "Copy DLL failed.");
                }
#endif


                Debug.Log($"[BuildDllTask] Hot-fix DLL files built and copied successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Build DLL failed: {ex.Message}");
            }
        }

        private string GetBackupPath(BuildSetting buildSetting)
        {
            return BuildResourcePathHelper.GetBackupPath(buildSetting);
        }
    }
}
