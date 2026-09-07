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
        private readonly List<SystemProviderRegistration> _tempSystemProviders = new();

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
#if UNITY_EDITOR
                    systemProvider.SetDiagnosticsRegistration(
                        attribute.ProcedureState,
                        attribute.ProviderLifeCycle,
                        attribute.Sort);
#endif
                    var registration = new SystemProviderRegistration
                    {
                        Provider = systemProvider,
                        ProviderType = providerType,
                        Sort = attribute.Sort,
                        RegistrationOrder = _providerRegistrationOrder++
                    };
                    _tempSystemProviders.Add(registration);

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
                    _orderedSystemProviders.Add(registration);
                    _orderedSystemProviders.Sort(CompareSystemProviders);
#if UNITY_EDITOR
                    systemProvider.SetDiagnosticsStage(ProviderDiagnosticsStage.Registered);
#endif
                }

                _tempSystemProviders.Sort(CompareSystemProviders);

                foreach (var provider in _tempSystemProviders)
                {
                    currentUseProviderBase = provider.Provider;
                    currentProviderType = provider.ProviderType;
                    registerStage = "Inject";
                    Injection.Inject(provider.Provider);
#if UNITY_EDITOR
                    if (provider.Provider is SystemProviderBase injectedProvider)
                    {
                        injectedProvider.SetDiagnosticsStage(ProviderDiagnosticsStage.Injected);
                    }
#endif
                }

                foreach (var provider in _tempSystemProviders)
                {
                    currentUseProviderBase = provider.Provider;
                    currentProviderType = provider.ProviderType;
                    registerStage = "AwakeComponent";
                    provider.Provider.AwakeComponent();
#if UNITY_EDITOR
                    if (provider.Provider is SystemProviderBase awakenedProvider)
                    {
                        awakenedProvider.SetDiagnosticsStage(ProviderDiagnosticsStage.Awakened);
                    }
#endif
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
#if UNITY_EDITOR
                    if (provider.Provider is SystemProviderBase readyProvider)
                    {
                        readyProvider.SetDiagnosticsStage(ProviderDiagnosticsStage.Ready);
                    }
#endif
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
            foreach (var registration in _orderedSystemProviders.ToArray())
            {
                var attribute = registration.ProviderType.GetCustomAttribute<BelongToAttribute>();
                if (attribute == null)
                {
                    Log.Fatal($"None BelongToAttribute in '{registration.ProviderType.FullName}' Provider");
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

                registration.Provider.UnSubscribeEvent();
                UnRegisterProvider(registration.ProviderType, registration.Provider);
            }
        }

        private void UnRegisterProvider(Type providerType, ISystemProvider provider)
        {
            UnRegisterCommonDataSync(provider as SystemProviderBase);
            UnRegisterProviderService(providerType);
            var interfaceType = providerType.GetDerivedInterfaces(
                typeof(ISystemProvider),
                typeof(IReference),
                typeof(IDisposable));
            if (interfaceType != null)
            {
                LServices.Unregister(interfaceType);
            }

            Injection.ClearReflectionCache(providerType);
            ReferencePool.Release(provider);
            RemoveProviderRegistration(providerType, provider);
        }

        private void RemoveProviderRegistration(Type providerType, ISystemProvider provider)
        {
            if (_systemProviders.TryGetValue(providerType, out var registeredProvider) &&
                ReferenceEquals(registeredProvider, provider))
            {
                _systemProviders.Remove(providerType);
            }

            _orderedSystemProviders.RemoveAll(registration => ReferenceEquals(registration.Provider, provider));
        }

        private void RollbackCurrentProviderRegistration()
        {
            for (var providerIndex = _tempSystemProviders.Count - 1; providerIndex >= 0; providerIndex--)
            {
                var registeredProvider = _tempSystemProviders[providerIndex];
                try
                {
                    RemoveProviderRegistration(registeredProvider.ProviderType, registeredProvider.Provider);
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
