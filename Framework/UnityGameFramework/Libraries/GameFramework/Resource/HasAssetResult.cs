namespace GameFramework.Resource
{
    /// <summary>
    /// 资源查询结果
    /// </summary>
    public enum HasAssetResult : byte
    {
        /// <summary>
        /// 资源不存在
        /// </summary>
        NotExist = 0,

        /// <summary>
        /// 资源存在但未就绪
        /// </summary>
        NotReady,

        /// <summary>
        /// 资源存在且可加载
        /// </summary>
        Exist,
    }
}
