using System;
using System.Collections.Generic;
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

        public iOSPlatformConfig(BuildSetting buildSetting)
        {
            _buildSetting = buildSetting ?? throw new ArgumentNullException(nameof(buildSetting));
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
                options.options = BuildOptions.AllowDebugging |
                                  BuildOptions.Development |
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
            // 配置脚本后端
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.FromBuildTargetGroup(BuildTargetGroup.iOS),
                ScriptingImplementation.IL2CPP);

            // 配置 iOS 特定设置
            PlayerSettings.iOS.backgroundModes = iOSBackgroundMode.None;
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Custom;
            PlayerSettings.iOS.buildNumber = buildSetting.versionCode.ToString();
            PlayerSettings.iOS.targetOSVersionString = "12.0";
            PlayerSettings.iOS.deferSystemGesturesMode = SystemGestureDeferMode.All;
            PlayerSettings.iOS.hideHomeButton = false;

            // 配置签名
            PlayerSettings.iOS.appleEnableAutomaticSigning = false;
            //PlayerSettings.iOS.iOSManualProvisioningProfileID = ProjectBuilder_IOS_Data.MobileProvisionUUid;
            //PlayerSettings.iOS.appleDeveloperTeamID = ProjectBuilder_IOS_Data.AppleDevelopTeamId;
            PlayerSettings.iOS.iOSManualProvisioningProfileType = ProvisioningProfileType.Automatic;
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
