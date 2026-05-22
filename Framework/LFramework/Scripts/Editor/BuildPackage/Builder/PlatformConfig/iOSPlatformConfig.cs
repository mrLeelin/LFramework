using System;
using System.Collections.Generic;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.iOS;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// iOS 平台配置
    /// 提供 iOS 平台的构建配置
    /// </summary>
    public class iOSPlatformConfig : IPlatformConfig
    {
        private readonly BuildSetting _buildSetting;
        private readonly iOSSetting _iOSSetting;

        public iOSPlatformConfig(BuildSetting buildSetting)
        {
            _buildSetting = buildSetting ?? throw new ArgumentNullException(nameof(buildSetting));
            _iOSSetting = SettingManager.GetSetting<iOSSetting>();
            if (_iOSSetting == null)
            {
                Debug.LogError("[iOSPlatformConfig] iOS build settings not found]");
            }
        }

        public BuildTarget GetBuildTarget()
        {
            return BuildTarget.iOS;
        }

        public BuildTargetGroup GetBuildTargetGroup()
        {
            return BuildTargetGroup.iOS;
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

            if (buildSetting.isRelease)
            {
                options.options = BuildOptions.None;
            }
            else
            {
                options.options = BuildOptions.Development |
                                  BuildOptions.ConnectWithProfiler;

                if (buildSetting.isDeepProfiler)
                {
                    options.options |= BuildOptions.EnableDeepProfilingSupport;
                }
            }

            return options;
        }

        public void ConfigurePlatformSettings(BuildSetting buildSetting)
        {
            if (_iOSSetting == null)
            {
                throw new InvalidOperationException(
                    "iOSSetting not found in ProjectSettingSelector, unable to configure iOS build settings.");
            }

            if (!_iOSSetting.Validate(out string errorMessage))
            {
                throw new InvalidOperationException(
                    $"iOSSetting validation failed, unable to configure iOS build settings. {errorMessage}");
            }

            // 配置脚本后端
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS),
                ScriptingImplementation.IL2CPP);
            

            // 配置 iOS 特定设置
            PlayerSettings.iOS.backgroundModes = iOSBackgroundMode.None;
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Custom;
            PlayerSettings.iOS.buildNumber = buildSetting.versionCode.ToString();
            PlayerSettings.iOS.targetOSVersionString = _iOSSetting.TargetOSVersion;
            PlayerSettings.iOS.requiresFullScreen = _iOSSetting.RequiresFullScreen;
            PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.All;
            PlayerSettings.iOS.hideHomeButton = true;
            // 配置签名
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            PlayerSettings.iOS.iOSManualProvisioningProfileID = _iOSSetting.MobileProvisionUUid;
            PlayerSettings.iOS.appleDeveloperTeamID = _iOSSetting.AppleDevelopTeamId;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = buildSetting.isRelease
                ? ProvisioningProfileType.Distribution
                : ProvisioningProfileType.Development;
            
        }

        public string GetOutputPath(BuildSetting buildSetting)
        {
            return Application.dataPath + "/../Builds/IOS/Project";
        }

        public string GetBuildFolderPath()
        {
            return Application.dataPath + "/../Builds/IOS";
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
