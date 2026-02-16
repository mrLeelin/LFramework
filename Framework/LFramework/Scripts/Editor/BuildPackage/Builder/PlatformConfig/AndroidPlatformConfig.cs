using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// Android 平台配置
    /// 提供 Android 平台的构建配置
    /// </summary>
    public class AndroidPlatformConfig : IPlatformConfig
    {
        private readonly BuildSetting _buildSetting;

        public AndroidPlatformConfig(BuildSetting buildSetting)
        {
            _buildSetting = buildSetting ?? throw new ArgumentNullException(nameof(buildSetting));
        }

        public BuildTarget GetBuildTarget()
        {
            return BuildTarget.Android;
        }

        public BuildTargetGroup GetBuildTargetGroup()
        {
            return BuildTargetGroup.Android;
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

            // 根据是否导出项目设置选项
            if (buildSetting.buildAndroidAppType == BuildAndroidAppType.ExportAndroidProject)
            {
                options.options = BuildOptions.AcceptExternalModificationsToPlayer;
            }
            else
            {
                options.options = BuildOptions.None;
            }

            if (!buildSetting.isRelease)
            {
                options.options |= BuildOptions.AllowDebugging | BuildOptions.Development;

                if (buildSetting.isDeepProfiler)
                {
                    options.options |= BuildOptions.EnableDeepProfilingSupport;
                }
            }

            return options;
        }

        public void ConfigurePlatformSettings(BuildSetting buildSetting)
        {
            // 配置构建系统
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = buildSetting.buildAndroidAppType == BuildAndroidAppType.AppBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject =
                buildSetting.buildAndroidAppType == BuildAndroidAppType.ExportAndroidProject;

            // 配置脚本后端和架构
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android),
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;

            // 配置版本号
            PlayerSettings.Android.bundleVersionCode = GetVersionCode(buildSetting);

            // 配置签名（仅 Release 且非导出项目时）
            if (buildSetting.isRelease && buildSetting.buildAndroidAppType != BuildAndroidAppType.ExportAndroidProject)
            {
                ConfigureKeystore();
                PlayerSettings.Android.useCustomKeystore = true;
            }
        }

        public string GetOutputPath(BuildSetting buildSetting)
        {
            string releaseType = buildSetting.isRelease ? "Release" : "Debug";
            string appName = $"Build_{releaseType}_{buildSetting.appVersion}_{buildSetting.versionCode}";

            if (buildSetting.isDeepProfiler)
            {
                appName += "_DeepProfiler";
            }

            // 根据构建类型返回不同路径
            if (buildSetting.buildAndroidAppType == BuildAndroidAppType.ExportAndroidProject)
            {
                string timeInfo = DateTime.Now.ToString("yyyyMMddHHmmss");
                return Application.dataPath + $"/../Builds/Android_{releaseType}_{buildSetting.appVersion}_{timeInfo}/{appName}";
            }
            else
            {
                var ext = buildSetting.buildAndroidAppType == BuildAndroidAppType.AppBundle ? "aab" : "apk";
                return Application.dataPath + $"/../Builds/AndroidAPK/{appName}.{ext}";
            }
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

        private int GetVersionCode(BuildSetting buildSetting)
        {
            var version = buildSetting.appVersion;
            var versionCode = buildSetting.versionCode;
            string cleanedString = version.Replace(".", "");
            int result = int.Parse(cleanedString + versionCode.ToString());
            return result;
        }

        private void ConfigureKeystore()
        {
            PlayerSettings.Android.keystoreName = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../BuildBat/keystore/partygo.keystore"));
            PlayerSettings.Android.keystorePass = "123456";
            PlayerSettings.Android.keyaliasName = "partygo";
            PlayerSettings.Android.keyaliasPass = "123456";
        }
    }
}
