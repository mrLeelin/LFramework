using GameFramework.Fsm;
using GameFramework.Procedure;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Procedure
{
    public abstract class RuntimeBaseProcedure : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            // Runtime 流程没有 Hotfix World，属于驻留范围之外。
            if (LServices.TryGet<WorldResidentHost>(out var residentHost))
            {
                using (var transition = residentHost.BeginTransition(null))
                {
                    try
                    {
                        transition.Commit();
                    }
                    catch (AggregateException exception)
                    {
                        // 目标已经提交；旧 Resident 的清理异常不能阻断新流程的注入。
                        Log.Error(exception);
                    }
                }
            }

            Injection.Inject(this);
        }
    }
}
