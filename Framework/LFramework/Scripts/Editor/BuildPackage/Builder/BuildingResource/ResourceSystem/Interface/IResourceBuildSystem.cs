namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 资源构建系统接口
    /// 定义了资源构建的核心方法，支持不同的资源管理系统（Addressable、YooAssets等）
    /// 每个系统自己负责获取所需的配置，不依赖特定系统的参数
    /// </summary>
    public interface IResourceBuildSystem
    {
        /// <summary>
        /// 构建资源
        /// 每个系统自己负责获取所需的配置（如 AddressableAssetSettings）
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        void Build(BuildSetting buildSetting);

        /// <summary>
        /// 构建内置资源包
        /// 将资源打包到应用程序内部，不支持热更新
        /// </summary>
        void BuildInPackage();

        /// <summary>
        /// 获取资源构建路径
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <returns>构建路径</returns>
        string GetBuildPath(BuildSetting buildSetting);

        /// <summary>
        /// 获取资源加载路径
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <returns>加载路径（通常是CDN地址或本地路径）</returns>
        string GetLoadPath(BuildSetting buildSetting);
    }
}
