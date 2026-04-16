using GameFramework.Fsm;
using GameFramework.Procedure;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.Procedure
{
    public abstract class RuntimeBaseProcedure : ProcedureBase
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);

            if (LFrameworkAspect.Instance?.ProcedureScopeRegistry == null)
            {
                Log.Error("RuntimeBaseProcedure.OnEnter: ProcedureScopeRegistry is null. Cannot inject procedure '{0}'.", GetType().Name);
                return;
            }

            LFrameworkAspect.Instance.ProcedureScopeRegistry.EnterProcedureScope(this);
            LFrameworkAspect.Instance.FrameworkInjector.Inject(this);
        }
    }
}
