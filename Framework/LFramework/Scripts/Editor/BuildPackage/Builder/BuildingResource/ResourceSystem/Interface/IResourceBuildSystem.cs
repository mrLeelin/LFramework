using UnityEditor.AddressableAssets.Settings;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 资源构建系统接口
    /// 定义了资源构建的核心方法，支持不同的资源管理系统（Addressable、YooAssets等）
    /// </summary>
    public interface IResourceBuildSystem
    {
        /// <summary>
        /// 构建资源
        /// </summary>
        /// <param name="buildResourcesData">构建资源数据配置</param>
        /// <param name="settings">Addressable资源设置（仅Addressable系统使用）</param>
        /// <param name="gameSetting">游戏设置</param>
        void Build(BuildResourcesData buildResourcesData, AddressableAssetSettings settings, LFramework.Runtime.GameSetting gameSetting);

        /// <summary>
        /// 构建内置资源包
        /// 将资源打包到应用程序内部，不支持热更新
        /// </summary>
        /// <param name="settings">Addressable资源设置（仅Addressable系统使用）</param>
        void BuildInPackage(AddressableAssetSettings settings);

        /// <summary>
        /// 获取资源构建路径
        /// </summary>
        /// <param name="data">构建资源数据配置</param>
        /// <returns>构建路径</returns>
        string GetBuildPath(BuildResourcesData data);

        /// <summary>
        /// 获取资源加载路径
        /// </summary>
        /// <param name="data">构建资源数据配置</param>
        /// <returns>加载路径（通常是CDN地址或本地路径）</returns>
        string GetLoadPath(BuildResourcesData data);
    }
}
