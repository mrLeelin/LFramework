using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

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
            var container = LFrameworkAspect.Instance.DiContainer;
            if (container == null)
            {
                Log.Fatal($"The  DiContainer is null in '{ProcedureState}'");
                return;
            }

            container.Resolve<ISystemProviderRegister>()
                .TryRegisterProvider(ProcedureState);
            var world = container.Resolve<IWorldRegister>().TryRegisterWorld(ProcedureState);
            if (world != null)
            {
                _linkWorld = world;
                _linkWorld.LinkProcedure(this);
            }

            container.Inject(this);
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

            var container = LFrameworkAspect.Instance.DiContainer;
            if (container == null)
            {
                return;
            }

            container.Resolve<ISystemProviderRegister>()
                .TryUnRegisterProvider(ProcedureState);
            container.Resolve<IWorldRegister>().TryUnRegisterWorld();
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