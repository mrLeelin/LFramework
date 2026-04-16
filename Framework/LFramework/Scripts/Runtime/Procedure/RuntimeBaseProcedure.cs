using GameFramework.Fsm;
using GameFramework.Procedure;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace LFramework.Runtime.Procedure
{
    public abstract class RuntimeBaseProcedure : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            if (LFrameworkAspect.Instance?.ProcedureScopeRegistry != null)
            {
                LFrameworkAspect.Instance.ProcedureScopeRegistry.EnterProcedureScope(this);
                LFrameworkAspect.Instance.FrameworkInjector.Inject(this);
                return;
            }

            LFrameworkAspect.Instance.DiContainer.Inject(this);
        }
    }
}
