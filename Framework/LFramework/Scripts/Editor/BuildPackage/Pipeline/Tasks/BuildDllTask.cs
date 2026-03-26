using System;
using System.Linq;
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

            // 仅在需要构建 DLL 时执行
            return context.BuildSetting.isBuildDll;
        }

        public BuildTaskResult Execute(BuildPipelineContext context)
        {
            try
            {
                Debug.Log($"[BuildDllTask] Building hot-fix DLL files...");

                var buildSetting = context.BuildSetting;

                // 获取备份路径
                string backupPath = GetBackupPath(buildSetting);

                // 构建 DLL
                if (!BuildDllsHelper.BuildDll(buildSetting.buildType == BuildType.APP, backupPath))
                {
                    return BuildTaskResult.CreateFailed(TaskName, "Build DLL failed.");
                }

                Debug.Log($"[BuildDllTask] DLL built successfully, refreshing AssetDatabase...");
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                // 复制 DLL
                if (!BuildDllsHelper.CopyDll(buildSetting,SettingManager.GetSetting<HybridCLRSetting>(), backupPath))
                {
                    return BuildTaskResult.CreateFailed(TaskName, "Copy DLL failed.");
                }

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
