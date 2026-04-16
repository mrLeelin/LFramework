using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Hotfix;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using VContainer;

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
            if (LFrameworkAspect.Instance?.ProcedureScopeRegistry == null ||
                LFrameworkAspect.Instance.FrameworkInjector == null)
            {
                Log.Fatal("ProcedureScopeRegistry or FrameworkInjector is null in '{0}'", ProcedureState);
                return;
            }

            var resolver = LFrameworkAspect.Instance.ProcedureScopeRegistry.EnterProcedureScope(this);
            var providerRegister = resolver.Resolve(typeof(ISystemProviderRegister));
            providerRegister.GetType()
                .GetMethod(nameof(ISystemProviderRegister.TryRegisterProvider))
                ?.Invoke(providerRegister, new object[] { ProcedureState });
            var worldRegister = resolver.Resolve(typeof(IWorldRegister));
            var world = worldRegister.GetType()
                .GetMethod(nameof(IWorldRegister.TryRegisterWorld))
                ?.Invoke(worldRegister, new object[] { ProcedureState }) as IWorld;
            if (world != null)
            {
                _linkWorld = world;
                _linkWorld.LinkProcedure(this);
            }

            LFrameworkAspect.Instance.FrameworkInjector.Inject(this);
            OnEnterProcedure(procedureOwner);
        }

        protected sealed override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            OnLeaveProcedure(procedureOwner, isShutdown);
            base.OnLeave(procedureOwner, isShutdown);

            if (LFrameworkAspect.Instance?.ProcedureScopeRegistry == null ||
                LFrameworkAspect.Instance.ResolverContext?.ProcedureResolver == null)
            {
                _linkWorld = null;
                return;
            }

            var resolver = LFrameworkAspect.Instance.ResolverContext.ProcedureResolver;
            var providerRegister = resolver.Resolve(typeof(ISystemProviderRegister));
            providerRegister.GetType()
                .GetMethod(nameof(ISystemProviderRegister.TryUnRegisterProvider))
                ?.Invoke(providerRegister, new object[] { ProcedureState });
            var worldRegister = resolver.Resolve(typeof(IWorldRegister));
            worldRegister.GetType()
                .GetMethod(nameof(IWorldRegister.TryUnRegisterWorld))
                ?.Invoke(worldRegister, null);
            LFrameworkAspect.Instance.ProcedureScopeRegistry.ExitProcedureScope();
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
