using System;
using System.Collections.Generic;
using GameFramework;
using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix
{
    public partial class LSystemApplication
    {
        private readonly Dictionary<Type, ISystemProvider> _systemProviders = new();
        private readonly List<Type> _tempRemoveKeys = new();
        private readonly List<TempSystemProviderContainer> _tempSystemProviders = new();

        /// <summary>
        /// 注册Provider
        /// </summary>
        /// <param name="procedureState"></param>
        public void TryRegisterProvider(int procedureState)
        {
            var allBelongToProcedureProviders = HotfixComponent.GetTypesFromAttribute<BelongToAttribute>();
            if (allBelongToProcedureProviders is not { Count: > 0 })
            {
                return;
            }

            _tempSystemProviders.Clear();
            foreach (var providerType in allBelongToProcedureProviders.Value)
            {
                //如果已经加载过了那么就不再继续加载
                if (_systemProviders.ContainsKey(providerType))
                {
                    continue;
                }

                if (!typeof(SystemProviderBase).IsAssignableFrom(providerType))
                {
                    continue;
                }

                var attribute = providerType.GetCustomAttribute<BelongToAttribute>();
                if (attribute == null)
                {
                    Log.Fatal($"None BelongToAttribute in '{providerType.FullName}' Provider");
                    return;
                }

                if (attribute.ProcedureState != procedureState)
                {
                    continue;
                }

                var instance = /*Activator.CreateInstance(providerType);*/ ReferencePool.Acquire(providerType);
                if (instance == null)
                {
                    return;
                }

                if (!(instance is SystemProviderBase systemProvider))
                {
                    Log.Fatal($"BelongToAttribute '{providerType.FullName}' is none impl ISystemProvider");
                    return;
                }

                RegisterCommonDataSync(systemProvider);
                var interfaceType = providerType.GetDerivedInterfaces(typeof(ISystemProvider), typeof(IReference),
                    typeof(IDisposable));
                if (interfaceType != null)
                {
                    LFrameworkAspect.Instance.DiContainer.Bind(interfaceType).FromInstance(systemProvider);
                }

                _systemProviders.Add(providerType, systemProvider);
                var sort = attribute.Sort;
                _tempSystemProviders.Add(new TempSystemProviderContainer()
                {
                    Provider = systemProvider,
                    Sort = sort
                });
            }

            _tempSystemProviders.Sort((x, y) => y.Sort.CompareTo(x.Sort));

            foreach (var provider in _tempSystemProviders)
            {
                LFrameworkAspect.Instance.DiContainer.Inject(provider.Provider);
            }

            foreach (var provider in _tempSystemProviders)
            {
                provider.Provider.AwakeComponent();
            }

            foreach (var provider in _tempSystemProviders)
            {
                provider.Provider.SubscribeEvent();
            }

            foreach (var provider in _tempSystemProviders)
            {
                provider.Provider.SetUp();
            }

            _tempSystemProviders.Clear();
        }

        /// <summary>
        /// 取消注册Provider
        /// </summary>
        /// <param name="procedureState"></param>
        public void TryUnRegisterProvider(int procedureState)
        {
            _tempRemoveKeys.Clear();
            foreach (var kPair in _systemProviders)
            {
                var attribute = kPair.Key.GetCustomAttribute<BelongToAttribute>();
                if (attribute == null)
                {
                    Log.Fatal($"None BelongToAttribute in '{kPair.Key.FullName}' Provider");
                    return;
                }

                if (attribute.ProviderLifeCycle == ProviderLifeCycle.Forever)
                {
                    continue;
                }

                if (attribute.ProcedureState != procedureState)
                {
                    continue;
                }

                kPair.Value.UnSubscribeEvent();
                UnRegisterProvider(kPair.Key, kPair.Value);
                _tempRemoveKeys.Add(kPair.Key);
            }

            foreach (var key in _tempRemoveKeys)
            {
                _systemProviders.Remove(key);
            }
        }

        private void UnRegisterProvider(Type t, ISystemProvider v)
        {
            ReferencePool.Release(v);
            //v.Dispose();
            UnRegisterCommonDataSync(v as SystemProviderBase);
            v = null;
            var interfaceType =
                t.GetDerivedInterfaces(typeof(ISystemProvider), typeof(IReference), typeof(IDisposable));
            if (interfaceType != null)
            {
                if (!LFrameworkAspect.Instance.DiContainer.Unbind(interfaceType))
                {
                    Log.Fatal($"Un bind '{interfaceType}' 'Provider' error.");
                }
            }
        }
    }
}