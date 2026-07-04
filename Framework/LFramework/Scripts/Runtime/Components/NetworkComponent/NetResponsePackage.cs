using System;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class NetResponsePackage : IReference
    {
        private object _cacheResponse;
        private bool _isPooled;

        public static NetResponsePackage Create(int messageID, ulong serverTime, int errorCode, object responseInstance)
        {
            var result = ReferencePool.Acquire<NetResponsePackage>();
            result._isPooled = true;
            result.ErrorCode = errorCode;
            result.ResponseInstance = responseInstance;
            result.ServerTime = serverTime;
            result.MessageID = messageID;
            return result;
        }

        public static NetResponsePackage CreateUnpooled(
            int messageID,
            ulong serverTime,
            int errorCode,
            object responseInstance)
        {
            return new NetResponsePackage
            {
                _isPooled = false,
                ErrorCode = errorCode,
                ResponseInstance = responseInstance,
                ServerTime = serverTime,
                MessageID = messageID
            };
        }

        public int MessageID { get; protected set; }

        public int ErrorCode { get; protected set; }

        public bool HasError => ErrorCode != 0;

        public object ResponseInstance { get; protected internal set; }

        public ulong ServerTime { get; protected internal set; }

        public bool EqualMessageID(int messageID)
        {
            return messageID == MessageID;
        }

        public object GetData(Type responseType)
        {
            if (_cacheResponse != null)
            {
                return _cacheResponse;
            }

            if (ResponseInstance == null || ErrorCode != 0)
            {
                return default;
            }

            if (responseType != null && !responseType.IsInstanceOfType(ResponseInstance))
            {
                Log.Error(
                    "Get network data error. type '{0}' is not of '{1}'",
                    responseType.FullName,
                    ResponseInstance.GetType().FullName);
                return default;
            }

            _cacheResponse = ResponseInstance;
            return _cacheResponse;
        }

        public TResponse GetData<TResponse>() where TResponse : class
        {
            return GetData(typeof(TResponse)) as TResponse;
        }

        public virtual void Clear()
        {
            _cacheResponse = null;
            _isPooled = false;
            ErrorCode = 0;
            ResponseInstance = null;
            ServerTime = 0;
            MessageID = 0;
        }

        public void Release()
        {
            if (_isPooled)
            {
                ReferencePool.Release(this);
                return;
            }

            Clear();
        }

        protected void SetValues(
            int messageID,
            ulong serverTime,
            int errorCode,
            object responseInstance,
            bool isPooled)
        {
            ErrorCode = errorCode;
            ResponseInstance = responseInstance;
            ServerTime = serverTime;
            MessageID = messageID;
            _isPooled = isPooled;
        }
    }

    public sealed class NetResponsePackage<TResponse> : NetResponsePackage
        where TResponse : class
    {
        private TResponse _cacheTypedResponse;

        public new static NetResponsePackage<TResponse> Create(
            int messageID,
            ulong serverTime,
            int errorCode,
            object responseInstance)
        {
            var result = ReferencePool.Acquire<NetResponsePackage<TResponse>>();
            result._cacheTypedResponse = null;
            result.SetValues(messageID, serverTime, errorCode, responseInstance, true);
            return result;
        }

        public new static NetResponsePackage<TResponse> CreateUnpooled(
            int messageID,
            ulong serverTime,
            int errorCode,
            object responseInstance)
        {
            var result = new NetResponsePackage<TResponse>();
            result.SetValues(messageID, serverTime, errorCode, responseInstance, false);
            return result;
        }

        public TResponse GetData()
        {
            if (_cacheTypedResponse != null)
            {
                return _cacheTypedResponse;
            }

            _cacheTypedResponse = base.GetData<TResponse>();
            return _cacheTypedResponse;
        }

        public override void Clear()
        {
            base.Clear();
            _cacheTypedResponse = null;
        }
    }
}
