using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// Windows 平台配置
    /// 提供 Windows Standalone 平台的构建配置
    /// </summary>
    public class WindowsPlatformConfig : IPlatformConfig
    {
        private readonly BuildSetting _buildSetting;

        public WindowsPlatformConfig(BuildSetting buildSetting)
        {
            _buildSetting = buildSetting ?? throw new ArgumentNullException(nameof(buildSetting));
        }

        public BuildTarget GetBuildTarget()
        {
            // Release 使用 StandaloneWindows，Debug 使用 StandaloneWindows64
            return _buildSetting.isRelease ? BuildTarget.StandaloneWindows : BuildTarget.StandaloneWindows64;
        }

        public BuildTargetGroup GetBuildTargetGroup()
        {
            return BuildTargetGroup.Standalone;
        }

        public BuildPlayerOptions GetBuildPlayerOptions(BuildSetting buildSetting)
        {
            var options = new BuildPlayerOptions
            {
                scenes = GetScenes(),
                target = GetBuildTarget(),
                targetGroup = GetBuildTargetGroup(),
                locationPathName = GetOutputPath(buildSetting)
            };

            options.options = PlayerBuildOptionsUtility.Apply(
                options.options, RuntimePlatform.WindowsPlayer, buildSetting.isRelease);

            return options;
        }

        public void ConfigurePlatformSettings(BuildSetting buildSetting)
        {
            PlayerSettings.macOS.buildNumber = buildSetting.versionCode.ToString();
        }

        public string GetOutputPath(BuildSetting buildSetting)
        {
            string timeInfo = DateTime.Now.ToString("yyyyMMddHHmmss");
            string releaseType = buildSetting.isRelease ? "Release" : "Debug";
            string appName = $"Build_{releaseType}_{buildSetting.GetAppVersion()}";

            return Application.dataPath +
                $"/../Builds/Window_{releaseType}_{buildSetting.GetAppVersion()}_{timeInfo}/{appName}.exe";
        }

        public string GetBuildFolderPath()
        {
            return Application.dataPath + "/../Builds";
        }

        private string[] GetScenes()
        {
            List<string> names = new List<string>();
            foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            {
                if (e != null && e.enabled)
                {
                    names.Add(e.path);
                }
            }
            return names.ToArray();
        }
    }
}
