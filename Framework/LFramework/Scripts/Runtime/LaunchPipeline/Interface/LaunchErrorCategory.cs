namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动期错误分类，用于重试与错误展示。
    /// </summary>
    public enum LaunchErrorCategory
    {
        None = 0,
        Network = 1,
        Timeout = 2,
        Server = 3,
        Config = 4,
        Parse = 5,
        Canceled = 6,
        Unknown = 7
    }
}
