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
        private bool _leaveInProgress;
        private bool _leaveCompleted;
        private bool _leaveCallbackCompleted;

        /// <summary>
        /// 当前流程
        /// </summary>
        protected abstract int ProcedureState { get; }


        public int ProcedureStatePublic => ProcedureState;

        protected sealed override void OnEnter(IFsm<IProcedureManager> procedureOwner)
        {
            base.OnEnter(procedureOwner);
            _leaveInProgress = false;
            _leaveCompleted = false;
            _leaveCallbackCompleted = false;
            if (!LServices.TryGet<ISystemProviderRegister>(out var providerRegister) ||
                !LServices.TryGet<IWorldRegister>(out var worldRegister))
            {
                Log.Fatal($"Hotfix procedure registers are not ready in '{ProcedureState}'");
                return;
            }

            var residentRegister = worldRegister as IWorldResidentRegister;
            try
            {
                residentRegister?.PrepareWorldResidents(ProcedureState);
                providerRegister.TryRegisterProvider(ProcedureState);
                var world = worldRegister.TryRegisterWorld(ProcedureState);
                if (world != null)
                {
                    _linkWorld = world;
                    _linkWorld.LinkProcedure(this);
                }

                Injection.Inject(this);
                residentRegister?.CompleteWorldResidentTransition();
            }
            catch (Exception enterException)
            {
                var errors = new List<Exception> { enterException };
                var worldCleanupSucceeded = false;
                try
                {
                    worldRegister.TryUnRegisterWorld();
                    worldCleanupSucceeded = true;
                }
                catch (Exception cleanupException)
                {
                    errors.Add(cleanupException);
                }

                if (worldCleanupSucceeded)
                {
                    _linkWorld = null;
                    try
                    {
                        residentRegister?.CancelWorldResidentTransition();
                    }
                    catch (Exception cleanupException)
                    {
                        errors.Add(cleanupException);
                    }
                }

                if (errors.Count > 1)
                {
                    throw new AggregateException("Procedure entry and cleanup failed.", errors);
                }

                throw;
            }

            // 业务入口可以立即切换到下一流程，不能让事务跨越该回调。
            OnEnterProcedure(procedureOwner);
        }

        protected sealed override void OnLeave(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
        {
            if (LServices.TryGet<WorldResidentHost>(out var host)) host.EnsureLifecycleIdle();
            if (_leaveInProgress || _leaveCompleted)
            {
                return;
            }

            _leaveInProgress = true;
            try
            {
                var errors = new List<Exception>();
                if (!_leaveCallbackCompleted)
                {
                    _leaveCallbackCompleted = TryLeave(() => OnLeaveProcedure(procedureOwner, isShutdown), errors);
                }

                // 正常切换失败时保留业务依赖，允许 FSM 再次执行退出。
                if (errors.Count > 0 && !isShutdown)
                {
                    throw new AggregateException("Procedure leave failed.", errors);
                }

                TryLeave(() => base.OnLeave(procedureOwner, isShutdown), errors);
                var residentExitSucceeded = true;
                var providerCleanupSucceeded = true;
                var worldCleanupSucceeded = true;
                if (LFrameworkAspect.Instance != null)
                {
                    if (LServices.TryGet<IWorldRegister>(out var residentWorldRegister) &&
                        residentWorldRegister is IWorldResidentRegister residentRegister)
                    {
                        residentExitSucceeded = TryLeave(residentRegister.ExitWorldResidents, errors);
                    }

                    if (residentExitSucceeded && LServices.TryGet<ISystemProviderRegister>(out var providerRegister))
                    {
                        providerCleanupSucceeded = TryLeave(
                            () => providerRegister.TryUnRegisterProvider(ProcedureState), errors);
                    }

                    // Provider 清理失败时保留 World，供下一次退出重试使用。
                    if (residentExitSucceeded && providerCleanupSucceeded && LServices.TryGet<IWorldRegister>(out var worldRegister))
                    {
                        worldCleanupSucceeded = TryLeave(worldRegister.TryUnRegisterWorld, errors);
                        if (worldCleanupSucceeded && worldRegister is IWorldResidentRegister pendingRegister)
                        {
                            // 进入失败且补偿退出也失败时，重试退出后还需结束未提交的事务。
                            worldCleanupSucceeded = TryLeave(pendingRegister.CancelWorldResidentTransition, errors);
                        }
                    }
                }

                if (residentExitSucceeded && providerCleanupSucceeded && worldCleanupSucceeded)
                {
                    _linkWorld = null;
                }

                _leaveCompleted = errors.Count == 0;
                if (isShutdown)
                {
                    // FSM 可以销毁；遗留 Provider 由应用的统一关闭路径继续清理。
                    foreach (var error in errors)
                    {
                        Log.Error(error);
                    }
                }
                else if (errors.Count > 0)
                {
                    throw new AggregateException("Procedure leave failed.", errors);
                }
            }
            finally
            {
                _leaveInProgress = false;
            }
        }

        private static bool TryLeave(Action action, List<Exception> errors)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception exception)
            {
                errors.Add(exception);
                return false;
            }
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
