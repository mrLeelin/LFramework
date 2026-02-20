using System;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Builder.Pipeline.Tasks
{
    /// <summary>
    /// 构建游戏设置任务
    /// 根据构建配置更新 GameSetting 资源
    /// </summary>
    public class BuildGameSettingTask : IBuildTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => "Build Game Setting";

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "Update GameSetting asset based on build configuration";

        /// <summary>
        /// 判断任务是否可以执行
        /// 仅在构建 APP 时需要更新游戏设置
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        public bool CanExecute(BuildPipelineContext context)
        {
            if (context?.BuildSetting == null)
            {
                return false;
            }

            // 仅在构建 APP 时更新游戏设置
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
                Debug.Log($"[BuildGameSettingTask] Updating GameSetting asset...");

                var buildSetting = context.BuildSetting;

                // 使用 GameSettingProvider 获取 GameSetting
                var setting = GameSettingProvider.GetGameSetting();

                if (setting == null)
                {
                    return BuildTaskResult.CreateFailed(TaskName, "GameSetting asset not found in project.");
                }

                Debug.Log($"[BuildGameSettingTask] Found GameSetting at: {AssetDatabase.GetAssetPath(setting)}");

                // 更新设置
                setting.isRelease = buildSetting.isRelease;

                if (!string.IsNullOrEmpty(buildSetting.ip))
                {
                    setting.versionUrl = buildSetting.ip;
                    Debug.Log($"[BuildGameSettingTask] Set versionUrl: {buildSetting.ip}");
                }

                setting.isResourcesBuildIn = buildSetting.isResourcesBuildIn;

                if (!setting.isResourcesBuildIn)
                {
                    setting.appVersion = buildSetting.appVersion + "." + buildSetting.versionCode;
                    setting.resourceVersion = buildSetting.resourcesVersion;
                    setting.cdnType = buildSetting.cdnType;

                    Debug.Log($"[BuildGameSettingTask] Set appVersion: {setting.appVersion}");
                    Debug.Log($"[BuildGameSettingTask] Set resourceVersion: {setting.resourceVersion}");
                    Debug.Log($"[BuildGameSettingTask] Set cdnType: {setting.cdnType}");
                }

                setting.channel = GetBuildChannel(buildSetting);
                Debug.Log($"[BuildGameSettingTask] Set channel: {setting.channel}");

                // 标记为脏并保存
                EditorUtility.SetDirty(setting);
                AssetDatabase.SaveAssets();

                Debug.Log($"[BuildGameSettingTask] GameSetting updated successfully.");
                return BuildTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                return BuildTaskResult.CreateFailed(TaskName, $"Failed to update GameSetting: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取构建渠道
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <returns>渠道名称</returns>
        private string GetBuildChannel(BuildSetting buildSetting)
        {
            switch (buildSetting.builderTarget)
            {
                case BuilderTarget.Windows:
                    return buildSetting.windowsChannel.ToString();
                case BuilderTarget.Android:
                    return buildSetting.androidChannel.ToString();
                case BuilderTarget.iOS:
                    return buildSetting.iosChannel.ToString();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
