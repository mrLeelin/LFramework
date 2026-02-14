using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    public interface IWorkFlowEngine : IDisposable
    {
        void SetFinishNotifyEvent(Action<object> callBack, object param);
        void SetIsRunOnStart(bool isRunOnStart);
        
        void SetWhenFinishIsRestart(bool whenFinishIsRestart);
        
        void Fork(ref WorkFlow workFlow);

        void Fork(ref WorkFlow workFlow, GameObject go);
    }
}