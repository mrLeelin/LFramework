namespace LFramework.Runtime
{
    public struct NetworkRequestParam
    {
        public ulong SerialID;

        public object Payload;

        public NetworkRequestInfo RequestInfo;

        public int MessageID;

        public int ServerVersion;
    }
}
