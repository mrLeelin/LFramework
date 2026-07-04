using GameFramework;

namespace LFramework.Runtime
{
    public sealed class RequestMsg : IReference
    {
        public static RequestMsg Create(
            NetworkRequestParam requestParam,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageCallBack callBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            var msg = ReferencePool.Acquire<RequestMsg>();
            msg.RequestParam = requestParam;
            msg.SuccessfulCallBack = successfulCallBack;
            msg.Callback = callBack;
            msg.ErrorCallBack = errorCallBack;
            msg.ReSendTimes = 0;
            return msg;
        }

        public NetworkRequestParam RequestParam;

        public NetworkMessageSuccessfulCallBack SuccessfulCallBack;

        public NetworkMessageCallBack Callback;

        public NetworkMessageErrorCallBack ErrorCallBack;

        public long SendTime;

        public long PushTime;

        public int ReSendTimes;

        public void Clear()
        {
            RequestParam = default;
            SuccessfulCallBack = null;
            Callback = null;
            ErrorCallBack = null;
            SendTime = 0;
            PushTime = 0;
            ReSendTimes = 0;
        }
    }

    public sealed class ResponseMsg : IReference
    {
        public static ResponseMsg Create(
            ulong serialID,
            int messageID,
            byte[] bytes,
            NetworkMessageCallBack callBack,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            var msg = ReferencePool.Acquire<ResponseMsg>();
            msg.SerialID = serialID;
            msg.MessageID = messageID;
            msg.Bytes = bytes;
            msg.Callback = callBack;
            msg.SuccessfulCallBack = successfulCallBack;
            msg.ErrorCallBack = errorCallBack;
            return msg;
        }

        public ulong SerialID;

        public int MessageID;

        public byte[] Bytes;

        public NetworkMessageCallBack Callback;

        public NetworkMessageSuccessfulCallBack SuccessfulCallBack;

        public NetworkMessageErrorCallBack ErrorCallBack;

        public void Clear()
        {
            SerialID = 0;
            MessageID = 0;
            Bytes = null;
            Callback = null;
            SuccessfulCallBack = null;
            ErrorCallBack = null;
        }
    }

    public sealed class ThrowFailMsg : IReference
    {
        public static ThrowFailMsg Create(
            ulong serialID,
            int messageID,
            NetworkMessageCallBack callBack,
            NetworkMessageErrorCallBack errorCallBack)
        {
            var msg = ReferencePool.Acquire<ThrowFailMsg>();
            msg.SerialID = serialID;
            msg.MessageID = messageID;
            msg.Callback = callBack;
            msg.ErrorCallBack = errorCallBack;
            return msg;
        }

        public ulong SerialID;

        public int MessageID;

        public NetworkMessageCallBack Callback;

        public NetworkMessageErrorCallBack ErrorCallBack;

        public void Clear()
        {
            SerialID = 0;
            MessageID = 0;
            Callback = null;
            ErrorCallBack = null;
        }
    }
}
