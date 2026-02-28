using System;
using System.Linq;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
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
                var settings = AddressableAssetSettingsDefaultObject.Settings;

                // 使用 SettingManager 获取 GameSetting
                var gameSetting = SettingManager.GetSetting<GameSetting>();
                if (gameSetting == null)
                {
                    return BuildTaskResult.CreateFailed(TaskName, "GameSetting not found in project!");
                }

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
                if (!BuildDllsHelper.CopyDll(buildSetting, settings, gameSetting, backupPath))
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
            string channelName = AddressableBuildHelper.GetChannelName(buildSetting);
            string folderName = AddressableBuildHelper.GetFolderName(buildSetting);
            return $"{AddressableBuildHelper.GetExportPath()}/PartyGame_BackUp_BuildResource/{channelName}/{folderName}";
        }
    }
}
