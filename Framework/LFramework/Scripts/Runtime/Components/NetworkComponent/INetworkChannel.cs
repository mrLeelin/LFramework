using System;

namespace LFramework.Runtime
{
    public interface INetworkChannel
    {
        event EventHandler TokenErrorCallBack;

        void Initialize();

        void OnUpdate(float elapseSeconds, float realElapseSeconds);

        void Connect(string url);

        void SetTimeOut(int loginTimeOut, int timeOut);

        void Clear();

        void Post(
            NetworkRequestParam requestParam,
            NetworkMessageCallBack callBack,
            NetworkMessageSuccessfulCallBack successfulCallBack,
            NetworkMessageErrorCallBack errorCallBack,
            NetworkRequestFailedCallBack requestFailedCallBack);
    }
}
