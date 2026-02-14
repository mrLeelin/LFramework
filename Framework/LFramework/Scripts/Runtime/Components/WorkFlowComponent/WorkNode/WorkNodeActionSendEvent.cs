using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace LFramework.Runtime
{
    public class WorkNodeActionSendEvent : WorkNode
    {

        private WorkFlowStatus _workFlowStatus;
        public override void OnStart()
        {
            base.OnStart();
            _workFlowStatus = WorkFlowStatus.Successful;
        }

        public override WorkFlowStatus OnUpdate()
        {
            return _workFlowStatus;
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        public void SetEvent()
        {
            
        }
    }

}

