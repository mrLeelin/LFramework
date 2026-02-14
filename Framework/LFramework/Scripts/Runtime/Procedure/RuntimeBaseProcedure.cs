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
            LFrameworkAspect.Instance.DiContainer.Inject(this);
        }
    }
}