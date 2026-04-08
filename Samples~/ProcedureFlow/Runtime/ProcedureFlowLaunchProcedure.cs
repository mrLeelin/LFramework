using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Runtime.Procedure;
using UnityEngine;

namespace LFramework.Samples.ProcedureFlow
{
    public sealed class ProcedureFlowLaunchProcedure : RuntimeBaseProcedure
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Debug.Log("[ProcedureFlow] Enter Launch procedure.");
        }

        protected override void OnUpdate(IFsm<IProcedureManager> procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            Debug.Log("[ProcedureFlow] Transition Launch -> Home.");
            ChangeState<ProcedureFlowHomeProcedure>(procedureOwner);
        }
    }
}
