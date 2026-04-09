using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using GameFramework.Resource;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Runtime;
using UnityEngine;
using UnityEngine.Serialization;

namespace LFramework.Editor
{
    /// <summary>
    /// 打包设置
    /// </summary>
    [Serializable]
    public class BuildSetting
    {


    
        public string ip;

        /// <summary>
        /// 资源系统类型（Addressable 或 YooAssets）
        /// </summary>
        public ResourceMode ResourceSystem { get; set; }

        /// <summary>
        /// 打包的目标
        /// </summary>
        public BuilderTarget builderTarget;
        /// <summary>
        /// window 渠道
        /// </summary>
        public string windowsChannel = "WindowStore";
        /// <summary>
        /// android 渠道
        /// </summary>
        public string androidChannel = "GoogleStore";
        /// <summary>
        /// ios 渠道
        /// </summary>
        public string iosChannel = "AppStore";
        
        /// <summary>
        /// android 打包类型
        /// </summary>
        public BuildAndroidAppType buildAndroidAppType = BuildAndroidAppType.APK;
        /// <summary>
        /// 资源更新还是出包
        /// </summary>
        public BuildType buildType = BuildType.App;
        public bool isDeepProfiler = false;
        public bool isRelease = false;
        public string appVersion = "1.0.0";
        public int versionCode = 1;
        public bool isBuildResources = true;
        public bool isResourcesBuildIn = true;
        public string resourcesVersion = "1.0.0";
        public CdnType cdnType = CdnType.Debug;
        public bool isForceUpdate = false;

        /// <summary>
        /// 是否打包Dll
        /// </summary>
        public bool isbuildDll = false;

        public string GetAppVersion() => appVersion + "." + versionCode;

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"ip:{ip}");
            stringBuilder.AppendLine($"resourceSystem:{ResourceSystem}");
            stringBuilder.AppendLine($"builderTarget:{builderTarget}");
            stringBuilder.AppendLine($"windowsChannel:{windowsChannel}");
            stringBuilder.AppendLine($"androidChannel:{androidChannel}");
            stringBuilder.AppendLine($"iosChannel:{iosChannel}");
            stringBuilder.AppendLine($"buildAndroidAppType:{buildAndroidAppType}");
            stringBuilder.AppendLine($"buildType:{buildType}");
            stringBuilder.AppendLine($"isDeepProfiler:{isDeepProfiler}");
            stringBuilder.AppendLine($"isRelease:{isRelease}");
            stringBuilder.AppendLine($"appVersion:{appVersion}");
            stringBuilder.AppendLine($"versionCode:{versionCode}");
            stringBuilder.AppendLine($"isBuildResources:{isBuildResources}");
            stringBuilder.AppendLine($"isResourcesBuildIn:{isResourcesBuildIn}");
            stringBuilder.AppendLine($"resourcesVersion:{resourcesVersion}");
            stringBuilder.AppendLine($"cdnType:{cdnType}");
            stringBuilder.AppendLine($"isForceUpdate:{isForceUpdate}");
            stringBuilder.AppendLine($"isBuildDll:{isbuildDll}");
            return stringBuilder.ToString();
        }
    }
    
}
