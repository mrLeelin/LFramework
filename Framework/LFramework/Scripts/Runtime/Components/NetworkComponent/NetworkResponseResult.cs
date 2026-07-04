namespace LFramework.Runtime
{
    public sealed class NetworkResponseResult
    {
        public NetworkResponseResult(
            int clientMessageID,
            ulong serverTime,
            bool isLoginMessage,
            object commonData,
            NetResponsePackage package,
            object receive)
        {
            ClientMessageID = clientMessageID;
            ServerTime = serverTime;
            IsLoginMessage = isLoginMessage;
            CommonData = commonData;
            Package = package;
            Receive = receive;
        }

        public int ClientMessageID { get; }

        public ulong ServerTime { get; }

        public bool IsLoginMessage { get; }

        public object CommonData { get; }

        public NetResponsePackage Package { get; }

        public object Receive { get; }
    }
}
