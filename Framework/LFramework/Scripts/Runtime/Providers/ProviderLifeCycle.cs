using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public enum ProviderLifeCycle 
    {
        /// <summary>
        /// 当前流程
        /// </summary>
        CurState,
        /// <summary>
        /// 第一次出来之后就永远存在
        /// </summary>
        Forever,
    }

}
