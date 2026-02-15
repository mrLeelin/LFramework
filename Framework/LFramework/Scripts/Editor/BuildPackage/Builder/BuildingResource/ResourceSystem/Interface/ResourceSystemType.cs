namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 资源系统类型枚举
    /// 定义项目支持的资源管理系统类型
    /// </summary>
    public enum ResourceSystemType
    {
        /// <summary>
        /// Unity Addressable 资源系统
        /// 官方推荐的资源管理方案，支持异步加载、依赖管理和热更新
        /// </summary>
        Addressable = 0,

        /// <summary>
        /// YooAssets 资源系统
        /// 第三方资源管理方案，专为热更新设计，支持多种加载模式
        /// </summary>
        YooAssets = 1
    }
}
