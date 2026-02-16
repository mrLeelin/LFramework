using LFramework.Editor.Builder.BuildingResource;
using LFramework.Runtime;
using Sirenix.OdinInspector;
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
            ResourceSystem = BuildingResource.ResourceSystemType.Addressable; // 默认使用 Addressable
#if UNITY_ANDROID
            BuilderTarget = BuilderTarget.Android;
#elif UNITY_IOS
            BuilderTarget = BuilderTarget.iOS;
#endif
        }

        [Title("资源系统选择", null, TitleAlignments.Split, false)]
        public BuildingResource.ResourceSystemType ResourceSystem;

        public BuilderTarget BuilderTarget;

        [ShowIf("BuilderTarget", BuilderTarget.iOS)]
        public BuildIOSChannel IOSChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Windows)]
        public BuildWindowsChannel WindowsChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Android)]
        public BuildAndroidChannel AndroidChannel;

        /// <summary>
        /// 母包版本（例如：0.0.0.1）
        /// </summary>
        [Header("母包版本")]
        public string AppVersion;

        public string ResourcesVersion;
        public bool IsResourcesBuildIn;

        [Title("是否打包热更Dll", null, TitleAlignments.Split, false)]
        public bool IsBuildDll;

        [HideIf("IsResourcesBuildIn")]
        public bool IsForceUpdate;

        [HideIf("IsResourcesBuildIn")]
        public BuildType BuildType;

        [HideIf("IsResourcesBuildIn")]
        public BuildResourcesServerModel BuildResourcesServerModel;

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
                resourceSystem = data.ResourceSystem,
                builderTarget = data.BuilderTarget,
                windowsChannel = data.WindowsChannel,
                androidChannel = data.AndroidChannel,
                iosChannel = data.IOSChannel,
                appVersion = data.AppVersion,
                resourcesVersion = data.ResourcesVersion,
                isResourcesBuildIn = data.IsResourcesBuildIn,
                isBuildDll = data.IsBuildDll,
                isForceUpdate = data.IsForceUpdate,
                buildType = data.BuildType,
                cdnType = (CdnType)data.BuildResourcesServerModel
            };
        }
    }
}
