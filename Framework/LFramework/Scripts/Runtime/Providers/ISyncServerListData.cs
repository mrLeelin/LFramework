using System.Collections.Generic;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    ///     同步服务器数据接口
    /// </summary>
    [IgnoreInterface]
    public interface ISyncServerData
    {
        public void SyncServer(object message, bool isBasedLogin);
    }

    /// <summary>
    ///     同步服务器数据接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [IgnoreInterface]
    public interface ISyncServerData<in T> : ISyncServerData
    {
        void ISyncServerData.SyncServer(object message, bool isBasedLogin)
        {
            if (message is not T response)
            {
                Log.Error($"SyncServer message type error, need {typeof(T)}, but {message.GetType()}");
                return;
            }

            SyncServerData(response, isBasedLogin);
        }

        void SyncServerData(T serverData, bool isBasedLogin);
    }

    /// <summary>
    ///     同步服务器列表数据接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [IgnoreInterface]
    public interface ISyncServerListData<T> : ISyncServerData
    {
        void ISyncServerData.SyncServer(object message, bool isBasedLogin)
        {
            if (message is not IList<T> response)
            {
                Log.Error($"SyncServer message type error, need {typeof(T)}, but {message.GetType()}");
                return;
            }

            if (response.Count == 0)
            {
                return;
            }
            SyncServerData(response, isBasedLogin);
        }

        public void SyncServerData(IList<T> serverData, bool isBasedLogin);
    }
}