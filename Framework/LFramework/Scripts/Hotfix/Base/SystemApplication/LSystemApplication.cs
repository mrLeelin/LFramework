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
        ISingletonUpdate,
        ISingletonLateUpdate
    {
        private struct TempSystemProviderContainer
        {
            public ISystemProvider Provider;
            public int Sort;
        }

        [Inject] private HotfixComponent HotfixComponent { get; set; }

        private readonly GameFrameworkLinkedList<GameFrameworkComponent> _hotfixComponents = new();
        private readonly List<Type> _hotfixComponentServiceTypes = new();

        public LSystemApplication()
        {
            LServices.Inject(this);
            LServices.Register<ISystemProviderRegister>(this);
            LServices.Register<IWorldRegister>(this);
        }

        public void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var component in _hotfixComponents)
            {
                component.UpdateComponent(elapseSeconds, realElapseSeconds);
            }

            foreach (var systemProvider in _systemProviders)
            {
                systemProvider.Value.UpdateComponent(elapseSeconds, realElapseSeconds);
            }

            _worldBase?.Update(elapseSeconds, realElapseSeconds);
        }

        public void LateUpdate()
        {
            _worldBase?.LateUpdate();
        }

        public override void Dispose()
        {
            base.Dispose();
            foreach (var component in _hotfixComponents)
            {
                component.ShutDown();
            }

            _hotfixComponents.Clear();
            UnregisterHotfixComponentServices();
            DisposeAllActiveProviders();
            TryUnRegisterWorld();
            LServices.Unregister<ISystemProviderRegister>();
            LServices.Unregister<IWorldRegister>();
        }

        public void DisposeAllActiveProviders()
        {
            foreach (var provider in _systemProviders)
            {
                UnRegisterProvider(provider.Key, provider.Value);
            }

            _systemProviders.Clear();
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
