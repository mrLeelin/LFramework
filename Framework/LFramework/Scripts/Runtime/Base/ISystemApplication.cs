using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public interface ISystemApplication
    {
        /// <summary>
        /// 停止架构
        /// </summary>
        void StopApplication(ShutdownType shutdownType);

    }
}