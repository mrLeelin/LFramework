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
        private readonly List<Type> _tempRegisteredProviderTypes = new();
        private readonly List<TempSystemProviderContainer> _tempSystemProviders = new();

        public void TryRegisterProvider(int procedureState)
        {
            var allBelongToProcedureProviders = HotfixComponent.GetTypesFromAttribute<BelongToAttribute>();
            if (allBelongToProcedureProviders is not { Count: > 0 })
            {
                return;
            }

            _tempSystemProviders.Clear();
            _tempRegisteredProviderTypes.Clear();
            try
            {
                foreach (var providerType in allBelongToProcedureProviders.Value)
                {
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
                        continue;
                    }

                    if (attribute.ProcedureState != procedureState)
                    {
                        continue;
                    }

                    var instance = ReferencePool.Acquire(providerType);
                    if (instance == null)
                    {
                        continue;
                    }

                    if (instance is not SystemProviderBase systemProvider)
                    {
                        Log.Fatal($"BelongToAttribute '{providerType.FullName}' is none impl ISystemProvider");
                        if (instance is IReference reference)
                        {
                            ReferencePool.Release(reference);
                        }

                        continue;
                    }

                    RegisterCommonDataSync(systemProvider);
                    var interfaceType = providerType.GetDerivedInterfaces(
                        typeof(ISystemProvider),
                        typeof(IReference),
                        typeof(IDisposable));
                    if (interfaceType != null)
                    {
                        LServices.Register(interfaceType, systemProvider);
                    }

                    LServices.Register(providerType, systemProvider);
                    _systemProviders.Add(providerType, systemProvider);
                    _tempRegisteredProviderTypes.Add(providerType);
                    _tempSystemProviders.Add(new TempSystemProviderContainer
                    {
                        Provider = systemProvider,
                        Sort = attribute.Sort
                    });
                }

                _tempSystemProviders.Sort((x, y) => y.Sort.CompareTo(x.Sort));

                foreach (var provider in _tempSystemProviders)
                {
                    Injection.Inject(provider.Provider);
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
            }
            catch (Exception)
            {
                RollbackCurrentProviderRegistration();
                throw;
            }
            finally
            {
                _tempSystemProviders.Clear();
                _tempRegisteredProviderTypes.Clear();
            }
        }

        public void TryUnRegisterProvider(int procedureState)
        {
            _tempRemoveKeys.Clear();
            foreach (var kPair in _systemProviders)
            {
                var attribute = kPair.Key.GetCustomAttribute<BelongToAttribute>();
                if (attribute == null)
                {
                    Log.Fatal($"None BelongToAttribute in '{kPair.Key.FullName}' Provider");
                    continue;
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
            UnRegisterCommonDataSync(v as SystemProviderBase);
            UnRegisterProviderService(t);
            var interfaceType = t.GetDerivedInterfaces(
                typeof(ISystemProvider),
                typeof(IReference),
                typeof(IDisposable));
            if (interfaceType != null)
            {
                LServices.Unregister(interfaceType);
            }

            Injection.ClearReflectionCache(t);
            ReferencePool.Release(v);
        }

        private void RollbackCurrentProviderRegistration()
        {
            for (int i = _tempRegisteredProviderTypes.Count - 1; i >= 0; i--)
            {
                Type providerType = _tempRegisteredProviderTypes[i];
                if (!_systemProviders.TryGetValue(providerType, out var provider))
                {
                    continue;
                }

                UnRegisterProvider(providerType, provider);
                _systemProviders.Remove(providerType);
            }
        }

        private static void UnRegisterProviderService(Type providerType)
        {
            if (providerType == null)
            {
                return;
            }

            LServices.Unregister(providerType);
            var interfaceType = providerType.GetDerivedInterfaces(
                typeof(ISystemProvider),
                typeof(IReference),
                typeof(IDisposable));
            if (interfaceType != null)
            {
                LServices.Unregister(interfaceType);
            }
        }
    }
}
