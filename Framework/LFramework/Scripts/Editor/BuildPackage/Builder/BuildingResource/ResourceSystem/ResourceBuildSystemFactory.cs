using System;
using GameFramework.Resource;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 资源构建系统工厂类
    /// 根据资源系统类型创建对应的构建系统实例
    /// </summary>
    public static class ResourceBuildSystemFactory
    {
        /// <summary>
        /// 创建资源构建系统实例
        /// </summary>
        /// <param name="type">资源系统类型</param>
        /// <returns>资源构建系统实例</returns>
        /// <exception cref="ArgumentException">不支持的资源系统类型</exception>
        public static IResourceBuildSystem Create(ResourceMode type)
        {
            switch (type)
            {
                
                case ResourceMode.Addressable:
#if ADDRESSABLE_SUPPORT
                      return new AddressableBuildSystem();
#else
                    throw new NotSupportedException("Addressable is not enabled. Please define ADDRESSABLE_SUPPORT in Player Settings -> Scripting Define Symbols.");
#endif
                
                case ResourceMode.YooAsset:
#if YOOASSET_SUPPORT
                    return new YooAssetsBuildSystem();
#else
                    throw new NotSupportedException("YooAssets is not enabled. Please define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
#endif

                default:
                    throw new ArgumentException($"Unsupported resource system type: {type}");
            }
        }

        /// <summary>
        /// 检查资源系统类型是否受支持
        /// </summary>
        /// <param name="type">资源系统类型</param>
        /// <returns>是否受支持</returns>
        public static bool IsSupported(ResourceMode type)
        {
            switch (type)
            {
                case ResourceMode.Addressable:
                    return true;

                case ResourceMode.YooAsset:
#if YOOASSET_SUPPORT
                    return true;
#else
                    return false;
#endif

                default:
                    return false;
            }
        }

        /// <summary>
        /// 获取资源系统类型的显示名称
        /// </summary>
        /// <param name="type">资源系统类型</param>
        /// <returns>显示名称</returns>
        public static string GetDisplayName(ResourceMode type)
        {
            switch (type)
            {
                case ResourceMode.Addressable:
                    return "Addressable (Unity官方)";

                case ResourceMode.YooAsset:
#if YOOASSET_SUPPORT
                    return "YooAssets (第三方)";
#else
                    return "YooAssets (未启用)";
#endif

                default:
                    return type.ToString();
            }
        }
    }
}
