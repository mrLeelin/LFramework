using System;
using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder
{
    public class BuildPackageWindow
    {
        public BuildPackageWindow()
        {
            BuilderTarget = ConvertToBuilderTarget(EditorUserBuildSettings.activeBuildTarget);
        }

        public BuilderTarget BuilderTarget;

        [ShowIf("BuilderTarget", BuilderTarget.Windows)]
        public BuildWindowsChannel BuildWindowsChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Android)]
        public BuildAndroidChannel BuildAndroidChannel;

        [ShowIf("BuilderTarget", BuilderTarget.iOS)]
        public BuildIOSChannel BuildIOSChannel;

        public BuildType BuildType;
        public bool IsBuildDll;
        public bool IsDeepProfiler;
        public bool IsRelease;

        public string AppVersion;
        public string VersionCode;
       

        [ShowIf("BuildType", BuildType.APP)] public bool IsBuildResources;
        [ShowIf("BuildType", BuildType.APP)] public bool IsResourcesBuildIn;

        [HideIf("IsResourcesBuildIn")] public string ResourcesVersion;
        public CdnType CdnType;

        [ShowIf("BuildType", BuildType.ResourcesUpdate)]
        public bool IsForceUpdate;


        [Button("打包")]
        public void Build()
        {
            var setting = new BuildSetting()
            {
                builderTarget = BuilderTarget,
                windowsChannel = BuildWindowsChannel,
                androidChannel = BuildAndroidChannel,
                iosChannel = BuildIOSChannel,
                buildType = BuildType,
                isBuildDll = IsBuildDll,
                isDeepProfiler = IsDeepProfiler,
                isRelease = IsRelease,
                appVersion = AppVersion,
                versionCode = int.Parse(VersionCode),//VersionCode,
                isBuildResources = IsBuildResources,
                isResourcesBuildIn = IsResourcesBuildIn,
                resourcesVersion = ResourcesVersion,
                cdnType = CdnType,
                isForceUpdate = IsForceUpdate
            };

            ProjectBuilder.Build(setting);
        }

        // 映射方法，将 BuildTarget 转换为自定义的 BuilderTarget 枚举
        public static BuilderTarget ConvertToBuilderTarget(BuildTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return BuilderTarget.Windows;

                case BuildTarget.Android:
                    return BuilderTarget.Android;

                case BuildTarget.iOS:
                    return BuilderTarget.iOS;

                default:
                    Log.Error($"Unsupported BuildTarget: {buildTarget}. Defaulting to Windows.");
                    return BuilderTarget.Windows; // 默认返回 Windows
            }
        }

        public static BuildTarget ConvertToBuilderTarget(BuilderTarget buildTarget)
        {
            switch (buildTarget)
            {
                case BuilderTarget.Android:
                    return BuildTarget.Android;
                case BuilderTarget.iOS:
                    return BuildTarget.iOS;
                case BuilderTarget.Windows:
                    return BuildTarget.StandaloneWindows64;
                default:
                {
                    Log.Error($"Unsupported BuildTarget: {buildTarget}. Defaulting to Windows.");
                    throw new ArgumentOutOfRangeException(nameof(buildTarget), buildTarget, null);
                }
            }
        }
    }
}