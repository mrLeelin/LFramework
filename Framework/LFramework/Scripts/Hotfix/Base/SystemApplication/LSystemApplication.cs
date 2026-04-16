using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using VContainer;

namespace LFramework.Hotfix
{
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


        public LSystemApplication()
        {
            LFrameworkAspect.Instance.HotfixScopeRegistry.EnterHotfixScope(builder =>
            {
                builder.RegisterInstance<ISystemProviderRegister>(this);
                builder.RegisterInstance<IWorldRegister>(this);
            });

            LFrameworkAspect.Instance.FrameworkInjector.Inject(this);
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
            foreach (var provider in _systemProviders)
            {
                ReferencePool.Release(provider.Value);
                //provider.Value.Dispose();
            }

            _systemProviders.Clear();

            if (_worldBase != null)
            {
                ReferencePool.Release(_worldBase);
                //_worldBase.Dispose();
            }

            _worldBase = null;
            LFrameworkAspect.Instance?.HotfixScopeRegistry?.ExitHotfixScope();
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

            // Collect bind type → instance pairs for scope registration
            var bindEntries = new List<(Type bindType, GameFrameworkComponent instance)>();

            foreach (var type in hotfixTypes)
            {
                if (type == null)
                {
                    continue;
                }

                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                if (!typeof(GameFrameworkComponent).IsAssignableFrom(type))
                {
                    continue;
                }

                LinkedListNode<GameFrameworkComponent> current = _hotfixComponents.First;
                bool isDuplicate = false;
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

                var instance = Activator.CreateInstance(type) as GameFrameworkComponent;
                if (instance == null)
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
                        bindEntries.Add((interfaceType, instance));
                    }
                    else
                    {
                        Log.Fatal("Hotfix component type '{0}' is none bind type.");
                    }
                }
                else
                {
                    bindEntries.Add((hotfixComponentAttribute.BindType, instance));
                }

                _hotfixComponents.AddLast(instance);
            }

            // Re-enter Hotfix Scope with all registrations (base interfaces + hotfix components)
            if (bindEntries.Count > 0)
            {
                LFrameworkAspect.Instance.HotfixScopeRegistry.EnterHotfixScope(builder =>
                {
                    builder.RegisterInstance<ISystemProviderRegister>(this);
                    builder.RegisterInstance<IWorldRegister>(this);
                    foreach (var (bindType, instance) in bindEntries)
                    {
                        builder.RegisterInstance(instance).As(bindType);
                    }
                });
            }

            // Inject all hotfix components
            foreach (var component in _hotfixComponents)
            {
                LFrameworkAspect.Instance.FrameworkInjector.Inject(component);
            }

            // Lifecycle callbacks in order
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

        /// <summary>
        /// 获取热更Components
        /// </summary>
        /// <param name="hotfixComponent"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private bool GetHotfixComponentTypes(HotfixComponent hotfixComponent,
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
    }
}