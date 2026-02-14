using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;


namespace LFramework.Runtime
{
    public class WorkFlowComponent : GameFrameworkComponent
    {
        public override void AwakeComponent()
        {
            base.AwakeComponent();
            CreateInstance();
        }

        public WorkFlow Fork(IWorkFlowEngine workFlowEngine)
        {
            if (workFlowEngine == null)
            {
                Log.Fatal("The work flow engine is null.");
                return null;
            }
            workFlowEngine.SetIsRunOnStart(false);
            workFlowEngine.SetWhenFinishIsRestart(false);
            WorkFlow result = null;
            workFlowEngine.Fork(ref result);
            workFlowEngine.Dispose();
            result.transform.SetParent(Instance);
            return result;
        }
        
        
        
    }
}


