using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LFramework.Editor.Builder;
using LFramework.Runtime;
using UnityEngine;

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
        /// 打包的目标
        /// </summary>
        public BuilderTarget builderTarget;
        /// <summary>
        /// window 渠道
        /// </summary>
        public BuildWindowsChannel windowsChannel = BuildWindowsChannel.WindowStore;
        /// <summary>
        /// android 渠道
        /// </summary>
        public BuildAndroidChannel androidChannel = BuildAndroidChannel.GoogleStore;
        /// <summary>
        /// ios 渠道
        /// </summary>
        public BuildIOSChannel iosChannel = BuildIOSChannel.AppStore;
        
        /// <summary>
        /// android 打包类型
        /// </summary>
        public BuildAndroidAppType buildAndroidAppType = BuildAndroidAppType.APK;
        /// <summary>
        /// 资源更新还是出包
        /// </summary>
        public BuildType buildType = BuildType.APP;
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
        public bool isBuildDll = false;

        public override string ToString()
        {
                
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"ip:{ip}");
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
            stringBuilder.AppendLine($"isBuildDll:{isBuildDll}");
            return stringBuilder.ToString();
        }
    }
    
}