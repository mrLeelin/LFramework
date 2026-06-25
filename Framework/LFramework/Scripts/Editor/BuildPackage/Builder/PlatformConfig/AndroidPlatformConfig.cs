using System;
using System.Collections.Generic;
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
        private readonly AndroidSetting _androidSetting;

        public AndroidPlatformConfig(BuildSetting buildSetting)
        {
            _buildSetting = buildSetting ?? throw new ArgumentNullException(nameof(buildSetting));
            _androidSetting = SettingManager.GetSetting<AndroidSetting>();
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
                /*
                options.options |= BuildOptions.Development;
                */
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
            
            
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android),
                _androidSetting.BundleIdentifier);
            
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = buildSetting.buildAndroidAppType == BuildAndroidAppType.AppBundle;
            EditorUserBuildSettings.exportAsGoogleAndroidProject =
                buildSetting.buildAndroidAppType == BuildAndroidAppType.ExportAndroidProject;

            // 配置脚本后端和架构
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.Android),
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.buildApkPerCpuArchitecture = false;

            // 配置版本号
            PlayerSettings.bundleVersion = buildSetting.appVersion + "." + buildSetting.versionCode;
            PlayerSettings.Android.bundleVersionCode = GetVersionCode(buildSetting);

            // 配置签名（非导出项目时，按 Debug/Release 读取对应配置）
            if (buildSetting.buildAndroidAppType != BuildAndroidAppType.ExportAndroidProject)
            {
                var androidSetting = SettingManager.GetSetting<AndroidSetting>();
                if (androidSetting == null)
                {
                    throw new InvalidOperationException(
                        "AndroidSetting not found in ProjectSettingSelector, unable to configure Android keystore.");
                }

                if (!androidSetting.ValidateForBuild(buildSetting.isRelease, out var errorMessage))
                {
                    throw new InvalidOperationException(
                        $"AndroidSetting validation failed, unable to configure Android keystore. {errorMessage}");
                }

                ConfigureKeystore(androidSetting, buildSetting.isRelease);
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

        private void ConfigureKeystore(AndroidSetting androidSetting, bool isRelease)
        {
            if (!androidSetting.UseCustomKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.Log("[AndroidPlatformConfig] Custom keystore disabled by AndroidSetting.");
                return;
            }

            AndroidKeystoreConfig keystoreConfig = androidSetting.GetKeystoreConfig(isRelease);
            string resolvedKeystorePath = AndroidSetting.ResolveKeystorePath(keystoreConfig.KeystorePath);

            PlayerSettings.Android.keystoreName = resolvedKeystorePath;
            PlayerSettings.Android.keystorePass = keystoreConfig.KeystorePass;
            PlayerSettings.Android.keyaliasName = keystoreConfig.KeyaliasName;
            PlayerSettings.Android.keyaliasPass = keystoreConfig.KeyaliasPass;
            PlayerSettings.Android.useCustomKeystore = true;
            Debug.Log($"[AndroidPlatformConfig] Using {keystoreConfig.BuildMode} Android keystore: {resolvedKeystorePath}");
        }
    }
}
