namespace GameFramework.Resource
{
    /// <summary>
    /// YooAsset 运行模式
    /// </summary>
    public enum YooAssetPlayMode : byte
    {
        /// <summary>
        /// 编辑器模拟模式（快速开发）
        /// </summary>
        EditorSimulateMode = 0,

        /// <summary>
        /// 单机运行模式（内置资源包）
        /// </summary>
        OfflinePlayMode,

        /// <summary>
        /// 联机运行模式（支持热更新）
        /// </summary>
        HostPlayMode,

        /// <summary>
        /// WebGL 运行模式
        /// </summary>
        WebPlayMode,
    }
}
