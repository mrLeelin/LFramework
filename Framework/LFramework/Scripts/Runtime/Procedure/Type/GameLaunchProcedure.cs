using System.Collections;
using System.Collections.Generic;
using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Runtime.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// 游戏启动流程
    /// </summary>
    public class GameLaunchProcedure : RuntimeBaseProcedure
    {
        protected override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            Log.Info("Enter GameLaunchProcedure.");
        }
    }
}