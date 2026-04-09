using System.Linq;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEngine;

namespace LFramework.Editor.Builder
{
    /// <summary>
    /// 构建资源数据
    /// 纯数据容器，只包含资源构建所需的配置信息
    /// 构建逻辑已移至 BuildResourcesService 和相关 Task
    /// </summary>
    public class BuildResourcesData
    {
        public BuildResourcesData()
        {
            BuilderTarget = BuilderTarget.Windows;
#if UNITY_ANDROID
            BuilderTarget = BuilderTarget.Android;
#elif UNITY_IOS
            BuilderTarget = BuilderTarget.iOS;
#endif
        }


        public BuilderTarget BuilderTarget;

        [ShowIf("BuilderTarget", BuilderTarget.iOS)]
        public string IOSChannel = "AppStore";

        [ShowIf("BuilderTarget", BuilderTarget.Windows)]
        public string WindowsChannel = "WindowStore";

        [ShowIf("BuilderTarget", BuilderTarget.Android)]
        public string AndroidChannel = "GoogleStore";

        /// <summary>
        /// 母包版本（例如：0.0.0.1）
        /// </summary>
        [Header("母包版本")] public string AppVersion;
        [Header("母包版本")] public int VersionCode;
        public string ResourcesVersion;
        public bool IsResourcesBuildIn;

        [Title("是否打包热更Dll", null, TitleAlignments.Split, false)]
        public bool IsBuildDll;

        [HideIf("IsResourcesBuildIn")] public bool IsForceUpdate;

        [HideIf("IsResourcesBuildIn")] public BuildType BuildType;

        [HideIf("IsResourcesBuildIn")] public BuildResourcesServerModel BuildResourcesServerModel;

        /// <summary>
        /// 编辑器按钮：触发资源构建
        /// 实际构建逻辑在 BuildResourcesService 中
        /// </summary>
        [InfoBox("点击按钮打包")]
        [Button("打包")]
        public void Build()
        {
            BuildOrchestrator.BuildFromSetting(ConvertToBuildSetting(this));
        }

        /// <summary>
        /// 将 BuildResourcesData 转换为 BuildSetting
        /// </summary>
        private static BuildSetting ConvertToBuildSetting(BuildResourcesData data)
        {
            return new BuildSetting
            {
                builderTarget = data.BuilderTarget,
                windowsChannel = data.WindowsChannel,
                androidChannel = data.AndroidChannel,
                iosChannel = data.IOSChannel,
                appVersion = data.AppVersion,
                versionCode = data.VersionCode,
                resourcesVersion = data.ResourcesVersion,
                isResourcesBuildIn = data.IsResourcesBuildIn,
                isbuildDll = data.IsBuildDll,
                isForceUpdate = data.IsForceUpdate,
                buildType = data.BuildType,
                cdnType = (CdnType)data.BuildResourcesServerModel
            };
        }
        
    }
}
