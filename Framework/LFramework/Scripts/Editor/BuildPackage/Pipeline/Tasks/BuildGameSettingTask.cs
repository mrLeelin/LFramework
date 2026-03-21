using System;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
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

                // 使用 SettingManager 获取 GameSetting
                var setting = SettingManager.GetSetting<GameSetting>();

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

                // 应用平台特定的配置
                ApplyPlatformSettings(buildSetting);

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

        /// <summary>
        /// 应用平台特定的配置
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        private void ApplyPlatformSettings(BuildSetting buildSetting)
        {
            switch (buildSetting.builderTarget)
            {
                case BuilderTarget.iOS:
                    ApplyiOSSettings();
                    break;

                case BuilderTarget.Android:
                    ApplyAndroidSettings();
                    break;

                case BuilderTarget.Windows:
                    // Windows 平台暂无特殊配置
                    Debug.Log("[BuildGameSettingTask] Windows platform - no additional settings to apply.");
                    break;

                default:
                    Debug.LogWarning($"[BuildGameSettingTask] Unsupported platform: {buildSetting.builderTarget}");
                    break;
            }
        }

        /// <summary>
        /// 应用 iOS 平台配置
        /// </summary>
        private void ApplyiOSSettings()
        {
            var iosSetting = SettingManager.GetSetting<iOSSetting>();
            if (iosSetting == null)
            {
                Debug.LogWarning("[BuildGameSettingTask] iOSSetting not found in SettingSelector, skipping iOS platform settings.");
                return;
            }

            // 验证配置
            if (!iosSetting.Validate(out var errorMessage))
            {
                Debug.LogError($"[BuildGameSettingTask] iOSSetting validation failed: {errorMessage}");
                return;
            }

            // 应用配置
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, iosSetting.BundleIdentifier);
            PlayerSettings.iOS.targetOSVersionString = iosSetting.TargetOSVersion;
            PlayerSettings.iOS.requiresFullScreen = iosSetting.RequiresFullScreen;

            // 设置权限描述
            if (!string.IsNullOrEmpty(iosSetting.CameraUsageDescription))
            {
                PlayerSettings.iOS.cameraUsageDescription = iosSetting.CameraUsageDescription;
            }
            if (!string.IsNullOrEmpty(iosSetting.LocationUsageDescription))
            {
                PlayerSettings.iOS.locationUsageDescription = iosSetting.LocationUsageDescription;
            }

            Debug.Log($"[BuildGameSettingTask] iOS settings applied successfully - Bundle ID: {iosSetting.BundleIdentifier}");
        }

        /// <summary>
        /// 应用 Android 平台配置
        /// </summary>
        private void ApplyAndroidSettings()
        {
            var androidSetting = SettingManager.GetSetting<AndroidSetting>();
            if (androidSetting == null)
            {
                Debug.LogWarning("[BuildGameSettingTask] AndroidSetting not found in SettingSelector, skipping Android platform settings.");
                return;
            }

            // 验证配置
            if (!androidSetting.Validate(out var errorMessage))
            {
                Debug.LogError($"[BuildGameSettingTask] AndroidSetting validation failed: {errorMessage}");
                return;
            }

            // 应用配置
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, androidSetting.BundleIdentifier);
            ApplyAndroidSdkVersions(androidSetting);

            // 设置脚本后端
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android,
                androidSetting.UseIL2CPP ? ScriptingImplementation.IL2CPP : ScriptingImplementation.Mono2x);

            Debug.Log($"[BuildGameSettingTask] Android settings applied successfully - Bundle ID: {androidSetting.BundleIdentifier}");
        }

        private static void ApplyAndroidSdkVersions(AndroidSetting androidSetting)
        {
            const int minimumSupportedApi = 25;

            int requestedMinApi = Mathf.Max(androidSetting.MinSdkVersion, minimumSupportedApi);
            int requestedTargetApi = Mathf.Max(androidSetting.TargetSdkVersion, requestedMinApi);

            AndroidSdkVersions? resolvedTarget = ResolveAndroidApiLevel(requestedTargetApi, fallbackToHighestAvailable: true);
            if (!resolvedTarget.HasValue)
            {
                throw new InvalidOperationException("No valid Android target SDK enum is available in the current Unity editor.");
            }

            AndroidSdkVersions? resolvedMin = ResolveAndroidApiLevel(requestedMinApi, fallbackToHighestAvailable: false);
            if (!resolvedMin.HasValue)
            {
                throw new InvalidOperationException($"No valid Android minimum SDK enum is available for API {requestedMinApi}.");
            }

            if (ExtractApiLevel(resolvedMin.Value) > ExtractApiLevel(resolvedTarget.Value))
            {
                resolvedMin = resolvedTarget;
            }

            PlayerSettings.Android.minSdkVersion = resolvedMin.Value;
            PlayerSettings.Android.targetSdkVersion = resolvedTarget.Value;

            Debug.Log($"[BuildGameSettingTask] Android SDK versions applied - Min API: {ExtractApiLevel(resolvedMin.Value)}, Target API: {ExtractApiLevel(resolvedTarget.Value)}");
        }

        private static AndroidSdkVersions? ResolveAndroidApiLevel(int requestedApiLevel, bool fallbackToHighestAvailable)
        {
            string directName = $"AndroidApiLevel{requestedApiLevel}";
            string[] enumNames = Enum.GetNames(typeof(AndroidSdkVersions));

            if (enumNames.Contains(directName))
            {
                return (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), directName);
            }

            var availableApiLevels = enumNames
                .Select(name => new { Name = name, ApiLevel = ParseApiLevel(name) })
                .Where(item => item.ApiLevel.HasValue)
                .OrderBy(item => item.ApiLevel.Value)
                .ToArray();

            if (availableApiLevels.Length == 0)
            {
                return null;
            }

            var capped = availableApiLevels.LastOrDefault(item => item.ApiLevel.Value <= requestedApiLevel);
            if (capped != null)
            {
                return (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), capped.Name);
            }

            var fallback = fallbackToHighestAvailable ? availableApiLevels[^1] : availableApiLevels[0];
            return (AndroidSdkVersions)Enum.Parse(typeof(AndroidSdkVersions), fallback.Name);
        }

        private static int ExtractApiLevel(AndroidSdkVersions sdkVersion)
        {
            return ParseApiLevel(sdkVersion.ToString()) ?? (int)sdkVersion;
        }

        private static int? ParseApiLevel(string enumName)
        {
            const string prefix = "AndroidApiLevel";
            if (!enumName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return null;
            }

            string numericPart = new string(enumName.Substring(prefix.Length).TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(numericPart, out int apiLevel) ? apiLevel : null;
        }
    }
}
