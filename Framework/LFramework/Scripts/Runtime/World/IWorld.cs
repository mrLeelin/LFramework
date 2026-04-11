using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Procedure;
using UnityEngine;

namespace LFramework.Runtime
{
    /// <summary>
    /// 世界的Interface 继承
    /// </summary>
    public interface IWorld : IReference
    {

        /// <summary>
        /// 设置当前世界的流程
        /// </summary>
        /// <param name="procedure"></param>
        void LinkProcedure(ProcedureBase procedure);
        
        /// <summary>
        /// 获取链接的流程
        /// </summary>
        /// <typeparam name="TProcedure"></typeparam>
        /// <returns></returns>
        public TProcedure GetLinkProcedure<TProcedure>() where TProcedure : ProcedureBase;

        /// <summary>
        /// 获取世界帮助类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetWorldHelper<T>() where T : class, IWorldHelper;
    }

}
