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

        public void TryRegisterProvider(int procedureState)
        {
            var allBelongToProcedureProviders = HotfixComponent.GetTypesFromAttribute<BelongToAttribute>();
            if (allBelongToProcedureProviders is not { Count: > 0 })
            {
                return;
            }

            _tempSystemProviders.Clear();
            ISystemProvider currentUseProviderBase = null;
            Type currentProviderType = null;
            string registerStage = "Scan";
            try
            {
                foreach (var providerType in allBelongToProcedureProviders.Value)
                {
                    currentProviderType = providerType;
                    registerStage = "Filter";

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

                    registerStage = "Acquire";
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

                    currentUseProviderBase = systemProvider;
                    _tempSystemProviders.Add(new TempSystemProviderContainer
                    {
                        Provider = systemProvider,
                        ProviderType = providerType,
                        Sort = attribute.Sort
                    });

                    registerStage = "Register";
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
                }

                _tempSystemProviders.Sort((x, y) => y.Sort.CompareTo(x.Sort));

                foreach (var provider in _tempSystemProviders)
                {
                    currentUseProviderBase = provider.Provider;
                    currentProviderType = provider.ProviderType;
                    registerStage = "Inject";
                    Injection.Inject(provider.Provider);
                }

                foreach (var provider in _tempSystemProviders)
                {
                    currentUseProviderBase = provider.Provider;
                    currentProviderType = provider.ProviderType;
                    registerStage = "AwakeComponent";
                    provider.Provider.AwakeComponent();
                }

                foreach (var provider in _tempSystemProviders)
                {
                    currentUseProviderBase = provider.Provider;
                    currentProviderType = provider.ProviderType;
                    registerStage = "SubscribeEvent";
                    provider.Provider.SubscribeEvent();
                }

                foreach (var provider in _tempSystemProviders)
                {
                    currentUseProviderBase = provider.Provider;
                    currentProviderType = provider.ProviderType;
                    registerStage = "SetUp";
                    provider.Provider.SetUp();
                }
            }
            catch (Exception e)
            {
                //加载Provider错误
                var providerTypeName = currentProviderType == null ? "null" : currentProviderType.FullName;
                var providerInstanceState = currentUseProviderBase == null ? "null" : "created";
                Log.Error(
                    "[LSystemApplication.RegisterProvider] Error while registering provider. ProcedureState: {0}, Stage: {1}, ProviderType: {2}, ProviderInstance: {3}.",
                    procedureState,
                    registerStage,
                    providerTypeName,
                    providerInstanceState);
                Log.Fatal(e);
                RollbackCurrentProviderRegistration();
                throw;
            }
            finally
            {
                _tempSystemProviders.Clear();
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
            for (int i = _tempSystemProviders.Count - 1; i >= 0; i--)
            {
                var registeredProvider = _tempSystemProviders[i];
                try
                {
                    _systemProviders.Remove(registeredProvider.ProviderType);
                    UnRegisterProvider(registeredProvider.ProviderType, registeredProvider.Provider);
                }
                catch (Exception rollbackException)
                {
                    Log.Error(
                        "[LSystemApplication.RegisterProvider] Failed to roll back provider '{0}'. {1}",
                        registeredProvider.ProviderType.FullName,
                        rollbackException);
                }
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
