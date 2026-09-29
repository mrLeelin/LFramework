using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework;
using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix
{
    /// <summary>
    /// Runtime owner for hotfix components, providers, and the active world.
    /// </summary>
    public partial class LSystemApplication :
        Singleton<LSystemApplication>,
        ISystemProviderRegister,
        IWorldRegister,
        IWorldResidentRegister,
        IApplicationLifecycle,
        IApplicationShutdown,
        ISingletonUpdate,
        ISingletonLateUpdate
    {
        private struct SystemProviderRegistration
        {
            public ISystemProvider Provider;
            public Type ProviderType;
            public int Sort;
            public long RegistrationOrder;
        }

        [Inject] private HotfixComponent HotfixComponent { get; set; }

        private readonly GameFrameworkLinkedList<GameFrameworkComponent> _hotfixComponents = new();
        private readonly List<Type> _hotfixComponentServiceTypes = new();
        private readonly List<SystemProviderRegistration> _orderedSystemProviders = new();
        private long _providerRegistrationOrder;
        private bool _applicationDisposed;
        private bool _applicationStopping;
        private bool _applicationDisposing;

        private static int CompareSystemProviders(SystemProviderRegistration left, SystemProviderRegistration right)
        {
            var sortComparison = right.Sort.CompareTo(left.Sort);
            return sortComparison != 0
                ? sortComparison
                : left.RegistrationOrder.CompareTo(right.RegistrationOrder);
        }

        public LSystemApplication()
        {
            LServices.Inject(this);
            LServices.Register<ISystemProviderRegister>(this);
            LServices.Register<IWorldRegister>(this);
            LServices.Register<IApplicationLifecycle>(this);
            LServices.Register<IApplicationShutdown>(this);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            if (_applicationStopping)
            {
                return;
            }

            foreach (var component in _hotfixComponents)
            {
                component.UpdateComponent(elapseSeconds, realElapseSeconds);
            }

            foreach (var systemProvider in _orderedSystemProviders)
            {
                systemProvider.Provider.UpdateComponent(elapseSeconds, realElapseSeconds);
            }

            _worldBase?.Update(elapseSeconds, realElapseSeconds);
        }

        public void LateUpdate()
        {
            if (!_applicationStopping)
            {
                _worldBase?.LateUpdate();
            }
        }

        public void OnApplicationFocus(bool hasFocus)
        {
            foreach (var component in _hotfixComponents)
            {
                component.RuntimeOnApplicationFocus(hasFocus);
            }
        }

        public void OnApplicationPause(bool pauseStatus)
        {
            foreach (var component in _hotfixComponents)
            {
                component.RuntimeOnApplicationPause(pauseStatus);
            }
        }

        public void OnApplicationQuit()
        {
            foreach (var component in _hotfixComponents)
            {
                component.RuntimeOnApplicationQuit();
            }
        }

        public override void Dispose()
        {
            if (_applicationDisposed || _applicationDisposing)
            {
                return;
            }

            _residentHost?.EnsureLifecycleIdle();
            _applicationStopping = true;
            _applicationDisposing = true;
            try
            {
                var errors = new List<Exception>();
                TryCleanup(ExitWorldResidents, errors);
                if (errors.Count == 0)
                {
                    TryCleanup(DisposeAllActiveProviders, errors);
                }
                if (errors.Count == 0)
                {
                    TryCleanup(TryUnRegisterWorld, errors);
                }

                if (errors.Count == 0)
                {
                    TryCleanup(CancelWorldResidentTransition, errors);
                }

                // 失败的 Provider/World 尚需依赖，重试成功后再关闭 Resident 和组件。
                if (errors.Count > 0)
                {
                    throw new AggregateException("Application cleanup failed.", errors);
                }

                if (_residentHost != null)
                {
                    TryCleanup(_residentHost.ClearResidents, errors);
                }

                var current = _hotfixComponents.First;
                while (current != null)
                {
                    var next = current.Next;
                    if (TryCleanup(current.Value.ShutDown, errors))
                    {
                        _hotfixComponents.Remove(current);
                    }
                    current = next;
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException("Application cleanup failed.", errors);
                }

                _hotfixComponents.Clear();
                UnregisterHotfixComponentServices();
                LServices.Unregister<ISystemProviderRegister>();
                LServices.Unregister<IWorldRegister>();
                LServices.Unregister<IApplicationLifecycle>();
                LServices.Unregister<IApplicationShutdown>();
                _residentHost = null;
                _residentTypesDiscovered = false;
                base.Dispose();
                _applicationDisposed = true;
            }
            finally
            {
                _applicationDisposing = false;
            }
        }

        /// <summary>在 Runtime 基础组件关闭前执行应用清理。</summary>
        public void ShutdownApplication()
        {
            if (_applicationDisposing)
            {
                throw new InvalidOperationException("Application cleanup is already in progress.");
            }

            Dispose();
        }

        private static bool TryCleanup(Action action, List<Exception> errors)
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

        public void DisposeAllActiveProviders()
        {
            _residentHost?.EnsureLifecycleIdle();
            var errors = new List<Exception>();
            foreach (var provider in _orderedSystemProviders.ToArray())
            {
                TryCleanup(() =>
                {
                    // Make the shutdown contract explicit before removing the service and releasing the reference.
                    provider.Provider.UnSubscribeEvent();
                    UnRegisterProvider(provider.ProviderType, provider.Provider);
                }, errors);
            }

            if (errors.Count == 1)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
            }

            if (errors.Count > 0)
            {
                throw new AggregateException("Provider cleanup failed.", errors);
            }
        }

        public void RegisterHotfixComponents(HotfixComponent hotfixComponent)
        {
            if (!GetHotfixComponentTypes(hotfixComponent, out var hotfixTypes))
            {
                return;
            }

            foreach (var type in hotfixTypes)
            {
                if (type == null || type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (!typeof(GameFrameworkComponent).IsAssignableFrom(type))
                {
                    continue;
                }

                var current = _hotfixComponents.First;
                var isDuplicate = false;
                while (current != null)
                {
                    if (current.Value.GetType() == type)
                    {
                        Log.Error("Game Framework hotfix component type '{0}' is already exist.", type.FullName);
                        isDuplicate = true;
                        break;
                    }

                    current = current.Next;
                }

                if (isDuplicate)
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is not GameFrameworkComponent instance)
                {
                    continue;
                }

                instance.Parent = hotfixComponent.Parent;
                var hotfixComponentAttribute = type.GetCustomAttribute<HotfixComponentAttribute>();
                if (hotfixComponentAttribute.BindType == null)
                {
                    var interfaceType = type.GetDerivedInterfaces();
                    if (interfaceType != null)
                    {
                        LServices.Register(interfaceType, instance);
                        _hotfixComponentServiceTypes.Add(interfaceType);
                    }
                    else
                    {
                        Log.Fatal("Hotfix component type '{0}' is none bind type.", type.FullName);
                    }
                }
                else
                {
                    LServices.Register(hotfixComponentAttribute.BindType, instance);
                    _hotfixComponentServiceTypes.Add(hotfixComponentAttribute.BindType);
                }

                _hotfixComponents.AddLast(instance);
            }

            foreach (var component in _hotfixComponents)
            {
                LServices.Inject(component);
            }

            foreach (var component in _hotfixComponents)
            {
                component.AwakeComponent();
            }

            foreach (var component in _hotfixComponents)
            {
                component.StartComponent();
            }

            foreach (var component in _hotfixComponents)
            {
                component.SetUpComponent();
            }
        }

        private bool GetHotfixComponentTypes(
            HotfixComponent hotfixComponent,
            out GameFrameworkLinkedListRange<Type> result)
        {
            var attributes = hotfixComponent.GetTypesFromAttribute<HotfixComponentAttribute>();
            if (attributes.HasValue)
            {
                result = attributes.Value;
            }
            else
            {
                result = default;
            }

            return attributes.HasValue;
        }

        private void UnregisterHotfixComponentServices()
        {
            foreach (var serviceType in _hotfixComponentServiceTypes)
            {
                LServices.Unregister(serviceType);
            }

            _hotfixComponentServiceTypes.Clear();
        }
    }
}
