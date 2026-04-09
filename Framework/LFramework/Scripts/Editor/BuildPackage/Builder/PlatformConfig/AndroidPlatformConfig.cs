using System;
using System.Collections.Generic;
using System.IO;
using LFramework.Runtime.Settings;
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
                var androidSetting = SettingManager.GetSetting<AndroidSetting>();
                if (androidSetting == null)
                {
                    throw new InvalidOperationException(
                        "AndroidSetting not found in ProjectSettingSelector, unable to configure Android keystore.");
                }

                if (!androidSetting.Validate(out var errorMessage))
                {
                    throw new InvalidOperationException(
                        $"AndroidSetting validation failed, unable to configure Android keystore. {errorMessage}");
                }

                ConfigureKeystore(androidSetting);
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
            }
        }

        public string GetOutputPath(BuildSetting buildSetting)
        {
            string releaseType = buildSetting.isRelease ? "Release" : "Debug";
            string appName = $"Build_{releaseType}_{buildSetting.GetAppVersion()}";

            if (buildSetting.isDeepProfiler)
            {
                appName += "_DeepProfiler";
            }

            // 根据构建类型返回不同路径
            if (buildSetting.buildAndroidAppType == BuildAndroidAppType.ExportAndroidProject)
            {
                string timeInfo = DateTime.Now.ToString("yyyyMMddHHmmss");
                return Application.dataPath + $"/../Builds/Android_{releaseType}_{buildSetting.GetAppVersion()}_{timeInfo}/{appName}";
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

        private void ConfigureKeystore(AndroidSetting androidSetting)
        {
            if (!androidSetting.UseCustomKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[AndroidPlatformConfig] Custom keystore disabled by AndroidSetting.");
                return;
            }

            string resolvedKeystorePath = Path.IsPathRooted(androidSetting.KeystorePath)
                ? Path.GetFullPath(androidSetting.KeystorePath)
                : Path.GetFullPath(Path.Combine(Application.dataPath, "..", androidSetting.KeystorePath));

            PlayerSettings.Android.keystoreName = resolvedKeystorePath;
            PlayerSettings.Android.keystorePass = androidSetting.KeystorePass;
            PlayerSettings.Android.keyaliasName = androidSetting.KeyaliasName;
            PlayerSettings.Android.keyaliasPass = androidSetting.KeyaliasPass;
            PlayerSettings.Android.useCustomKeystore = true;
        }
    }
}
