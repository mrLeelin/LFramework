namespace LFramework.Runtime
{
    public sealed class NetworkCompleteArgs : GameEventArgs<NetworkCompleteArgs>
    {
        public static NetworkCompleteArgs Create(ulong serverTime, NetResponsePackage netResponsePackage)
        {
            var args = CreateEmpty();
            args.ServerTime = serverTime;
            args.NetResponsePackage = netResponsePackage;
            return args;
        }

        public NetResponsePackage NetResponsePackage;

        public ulong ServerTime;

        public override void Clear()
        {
            NetResponsePackage = null;
            ServerTime = 0;
        }
    }

    public sealed class NetworkLoginCompleteArgs : GameEventArgs<NetworkLoginCompleteArgs>
    {
        public static NetworkLoginCompleteArgs Create(NetResponsePackage netResponsePackage)
        {
            var args = CreateEmpty();
            args.NetResponsePackage = netResponsePackage;
            return args;
        }

        public NetResponsePackage NetResponsePackage;

        public override void Clear()
        {
            NetResponsePackage = null;
        }
    }

    public sealed class NetworkLoginCompleteAfterOnUpdateArgs
        : GameEventArgs<NetworkLoginCompleteAfterOnUpdateArgs>
    {
        public static NetworkLoginCompleteAfterOnUpdateArgs Create(NetResponsePackage netResponsePackage)
        {
            var args = CreateEmpty();
            args.NetResponsePackage = netResponsePackage;
            return args;
        }

        public NetResponsePackage NetResponsePackage;

        public override void Clear()
        {
            NetResponsePackage = null;
        }
    }

    public sealed class NetworkTokenErrorArgs : GameEventArgs<NetworkTokenErrorArgs>
    {
        public static NetworkTokenErrorArgs Create()
        {
            return CreateEmpty();
        }

        public override void Clear()
        {
        }
    }
}
