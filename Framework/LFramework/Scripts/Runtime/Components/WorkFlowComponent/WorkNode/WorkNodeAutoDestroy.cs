using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public class WorkNodeAutoDestroy : WorkNode
    {
        public override void OnStart()
        {
            base.OnStart();
            if (this.WorkFlow != null && this.WorkFlow.gameObject != null)
            {
                Object.Destroy(this.WorkFlow.gameObject);
            }
        }

        public override WorkFlowStatus OnUpdate()
        {
            return WorkFlowStatus.Successful;
        }
    }
}

