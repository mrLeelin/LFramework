namespace LFramework.Runtime
{
    /// <summary>
    /// 在底层组件关闭前结束应用业务，保证清理时资源和实体组件仍然可用。
    /// </summary>
    public interface IApplicationShutdown
    {
        /// <summary>停止 World、Provider 和跨 World 驻留对象。</summary>
        void ShutdownApplication();
    }
}
