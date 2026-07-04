using System;
using UnityEngine;

namespace LFramework.Runtime
{
    public abstract class NetworkChannelBase : UnityEngine.MonoBehaviour, INetworkChannel
    {
        protected EventHandler TokenErrorCallBackField;

        public event EventHandler TokenErrorCallBack
        {
            add => TokenErrorCallBackField += value;
            remove => TokenErrorCallBackField -= value;
        }

        public abstract void Initialize();

        public abstract void OnUpdate(float elapseSeconds, float realElapseSeconds);

        public abstract void Connect(string url);

        public abstract void SetTimeOut(int loginTimeOut, int timeOut);

        public abstract void Clear();

        public abstract void Post(
            NetworkRequestParam requestParam,
            NetworkMessageCallBack callBack,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack,
            NetworkRequestFailedCallBack requestFailedCallBack);
    }
}
