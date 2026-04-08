using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Runtime.Procedure;
using UnityEngine;

namespace LFramework.Samples.MinimalBootstrap
{
    public sealed class MinimalBootstrapProcedure : RuntimeBaseProcedure
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Debug.Log("[MinimalBootstrap] Procedure entered successfully.");
        }
    }
}
