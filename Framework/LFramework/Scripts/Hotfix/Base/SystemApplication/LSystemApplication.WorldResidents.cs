using System;
using System.Collections.Generic;
using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix
{
    public partial class LSystemApplication
    {
        private WorldResidentHost _residentHost;
        private WorldResidentTransition _residentTransition;
        private HashSet<Type> _providersBeforeResidentTransition;
        private bool _residentTransitionPrepared;
        private int _residentProcedureState;
        private Type _residentWorldType;
        private bool _residentTypesDiscovered;

        /// <summary>框架在新流程注入前发现并激活匹配的驻留对象。</summary>
        public void PrepareWorldResidents(int procedureState)
        {
            if (_applicationStopping)
            {
                throw new ObjectDisposedException(nameof(LSystemApplication));
            }

            if (_residentTransitionPrepared)
            {
                if (_residentProcedureState != procedureState)
                {
                    throw new InvalidOperationException("A World transition is already in progress.");
                }

                return;
            }

            _residentWorldType = FindWorldType(procedureState);
            _residentHost ??= LServices.TryGet<WorldResidentHost>(out var host) ? host : null;
            var residentTypes = HotfixComponent.GetTypesFromAttribute<WorldResidentAttribute>();
            if (_residentHost == null && residentTypes is { Count: > 0 })
            {
                throw new InvalidOperationException("WorldResidentHost is missing from the application bootstrap.");
            }

            if (_residentHost != null && !_residentTypesDiscovered)
            {
                if (residentTypes.HasValue)
                {
                    _residentHost.RegisterTypes(residentTypes.Value);
                }

                _residentTypesDiscovered = true;
            }

            _residentTransition = _residentHost?.BeginTransition(_residentWorldType);
            _providersBeforeResidentTransition = new HashSet<Type>(_systemProviders.Keys);
            _residentProcedureState = procedureState;
            _residentTransitionPrepared = true;
        }

        /// <summary>提交已完成初始化的 World 切换。</summary>
        public void CompleteWorldResidentTransition()
        {
            var transition = _residentTransition;
            try
            {
                transition?.Validate();
                transition?.EnterWorld(_worldBase);
                transition?.Commit();
            }
            catch (Exception exception) when (transition != null && transition.IsCompleted)
            {
                // 提交后旧对象的清理失败不能撤销已生效的新 World。
                Log.Error(exception);
            }
            // 提交前失败时保留事务，由调用方在清理 World/外部注册后调用 CancelWorldResidentTransition 回滚；
            // 此处立即回滚会在外部注册仍占用服务时丢失保留对象的绑定。
            finally
            {
                if (transition == null || transition.IsCompleted) ClearResidentTransitionState();
            }
        }

        /// <summary>旧 World 和 Provider 清理前通知驻留对象退出；失败项保留供重试。</summary>
        public void ExitWorldResidents() => _residentHost?.ExitWorld();

        /// <summary>回滚本次新增 Provider 和驻留对象；已离开的旧 World 不会重建。</summary>
        public void CancelWorldResidentTransition()
        {
            _residentHost?.EnsureLifecycleIdle();
            if (!_residentTransitionPrepared) return;
            ExitWorldResidents();
            TryUnRegisterWorld();
            var transition = _residentTransition;
            var originalProviders = _providersBeforeResidentTransition;
            var errors = new List<Exception>();
            if (originalProviders != null)
            {
                var providers = _orderedSystemProviders.ToArray();
                for (var i = providers.Length - 1; i >= 0; i--)
                {
                    var provider = providers[i];
                    if (!originalProviders.Contains(provider.ProviderType))
                    {
                        TryCleanup(() => UnRegisterProvider(provider.ProviderType, provider.Provider), errors);
                    }
                }
            }

            if (errors.Count > 0)
                throw new AggregateException("World resident rollback failed.", errors);
            try
            {
                transition?.Dispose();
            }
            finally
            {
                if (transition == null || transition.IsCompleted) ClearResidentTransitionState();
            }
        }

        private void ClearResidentTransitionState()
        {
            _residentTransition = null;
            _providersBeforeResidentTransition = null;
            _residentTransitionPrepared = false;
            _residentWorldType = null;
        }

        private Type FindWorldType(int procedureState)
        {
            var types = HotfixComponent.GetTypesFromAttribute<BelongToAttribute>();
            if (!types.HasValue)
            {
                return null;
            }

            Type result = null;
            foreach (var type in types.Value)
            {
                if (!typeof(WorldBase).IsAssignableFrom(type) || type.IsAbstract)
                {
                    continue;
                }

                var attribute = type.GetCustomAttribute<BelongToAttribute>();
                if (attribute.ProcedureState != procedureState)
                {
                    continue;
                }

                if (result != null)
                {
                    throw new InvalidOperationException(
                        $"Procedure '{procedureState}' has multiple Worlds: '{result.FullName}' and '{type.FullName}'.");
                }

                result = type;
            }

            return result;
        }
    }
}
