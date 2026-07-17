using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public interface ISystemApplication
    {
        /// <summary>
        /// 停止架构
        /// </summary>
        void StopApplication(ShutdownType shutdownType);

    }

    public interface IApplicationLifecycle
    {
        void OnApplicationFocus(bool hasFocus);

        void OnApplicationPause(bool pauseStatus);

        void OnApplicationQuit();
    }
}
