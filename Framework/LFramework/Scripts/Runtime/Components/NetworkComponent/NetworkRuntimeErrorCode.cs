namespace LFramework.Runtime
{
    public enum NetworkRuntimeErrorCode
    {
        /// <summary>
        /// Helper没有注册
        /// </summary>
        HelperNotRegistered = -110000,
        /// <summary>
        /// 队列没有初始化
        /// </summary>
        RequestQueueNotInitialized = -110001,
        ChannelNotRegistered = -110002
    }
}
