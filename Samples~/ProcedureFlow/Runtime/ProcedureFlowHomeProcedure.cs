using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Runtime.Procedure;
using UnityEngine;

namespace LFramework.Samples.ProcedureFlow
{
    public sealed class ProcedureFlowHomeProcedure : RuntimeBaseProcedure
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Debug.Log("[ProcedureFlow] Enter Home procedure.");
        }
    }
}
