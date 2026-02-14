using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
   
    public class WorkNode : UnityEngine.MonoBehaviour
    {
        public object[] Data { get; set; }

        public WorkFlow WorkFlow { get; set; }
        
        protected WorkNodeStatus mNodeStatus = WorkNodeStatus.Enable;

        protected bool IsAvailableData
        {
            get { return Data != null && Data.Length > 0; }
        }

        public void OnEnable()
        {
            mNodeStatus = WorkNodeStatus.Enable;
        }

        public void OnDisable()
        {
            mNodeStatus = WorkNodeStatus.Disable;
        }

        public bool IsEnable => mNodeStatus == WorkNodeStatus.Enable;

        public virtual void OnStart()
        {
        }

        public virtual WorkFlowStatus OnUpdate()
        {
            return WorkFlowStatus.None;
        }

        public virtual void OnEnd()
        {
        } // 正常结束

        public virtual void OnStop()
        {
        } // 突然截止(非正常结束)

        public virtual void OnDispose()
        {
        }

        public virtual void OnResetWorkFlow()
        {
        }
        

        /// <summary>
        /// 是否跳过步骤
        /// </summary>
        public virtual bool OnJumpNode() => false;
        
        public virtual void OnMessageReceive(){}
    }
}