using System;
using System.Collections;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Hotfix
{
    [AttributeUsage(AttributeTargets.Class )]
    public class BelongToAttribute : GameAttribute
    {
        /// <summary>
        /// 属于哪个流程
        /// </summary>
        internal int ProcedureState { get; }

        /// <summary>
        /// 出现之后不卸载
        /// </summary>
        internal ProviderLifeCycle ProviderLifeCycle { get; }
        
        /// <summary>
        /// 排序
        /// </summary>
        internal int Sort { get; }

        public BelongToAttribute(int procedureState,
            ProviderLifeCycle lifeCycle = ProviderLifeCycle.CurState,
            int sort = 0
            )
        {
            this.ProcedureState = procedureState;
            this.ProviderLifeCycle = lifeCycle;
            this.Sort = sort;
        }
    }
}