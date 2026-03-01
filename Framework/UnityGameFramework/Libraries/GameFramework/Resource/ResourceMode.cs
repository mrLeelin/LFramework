namespace GameFramework.Resource
{
    /// <summary>
    /// 资源模式
    /// </summary>
    public enum ResourceMode : byte
    {
        /// <summary>
        /// 未指定
        /// </summary>
        Unspecified = 0,

        /// <summary>
        /// Unity Addressables 资源系统
        /// </summary>
        Addressable,

        /// <summary>
        /// YooAsset 资源系统
        /// </summary>
        YooAsset,
    }
}
