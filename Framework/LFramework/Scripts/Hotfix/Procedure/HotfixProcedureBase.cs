using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix.Procedure
{
    public abstract class HotfixProcedureBase : ProcedureBase
    {
        private IWorld _linkWorld;

        /// <summary>
        /// 当前流程
        /// </summary>
        protected abstract int ProcedureState { get; }


        public int ProcedureStatePublic => ProcedureState;

        protected sealed override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            if (!LServices.TryGet<ISystemProviderRegister>(out var providerRegister) ||
                !LServices.TryGet<IWorldRegister>(out var worldRegister))
            {
                Log.Fatal($"Hotfix procedure registers are not ready in '{ProcedureState}'");
                return;
            }

            providerRegister.TryRegisterProvider(ProcedureState);
            var world = worldRegister.TryRegisterWorld(ProcedureState);
            if (world != null)
            {
                _linkWorld = world;
                _linkWorld.LinkProcedure(this);
            }

            Injection.Inject(this);
            OnEnterProcedure(procedureOwner);
        }

        protected sealed override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            OnLeaveProcedure(procedureOwner, isShutdown);
            base.OnLeave(procedureOwner, isShutdown);
            if (LFrameworkAspect.Instance == null)
            {
                return;
            }

            if (!LServices.TryGet<ISystemProviderRegister>(out var providerRegister) ||
                !LServices.TryGet<IWorldRegister>(out var worldRegister))
            {
                return;
            }

            providerRegister.TryUnRegisterProvider(ProcedureState);
            worldRegister.TryUnRegisterWorld();
            _linkWorld = null;
        }


        protected virtual void OnEnterProcedure(IFsm<IProcedureManager> procedureOwner)
        {
        }

        protected virtual void OnLeaveProcedure(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
        }

        /// <summary>
        /// 获取链接的世界
        /// </summary>
        /// <typeparam name="TWorld"></typeparam>
        /// <returns></returns>
        protected virtual TWorld GetLinkWorld<TWorld>() where TWorld : class, IWorld
        {
            if (_linkWorld == null)
            {
                return null;
            }

            if (_linkWorld is not TWorld world)
            {
                Log.Error("The LinkWorld is not '{0}'", typeof(TWorld).FullName);
                return null;
            }

            return world;
        }
    }
}
