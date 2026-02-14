using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

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

        [Inject] private HotfixComponent HotfixComponent { get; }
        private readonly GameFrameworkLinkedList<GameFrameworkComponent> _hotfixComponents = new();
        
        
       

     

        public LSystemApplication()
        {
            LFrameworkAspect.Instance.DiContainer.Inject(this);
            LFrameworkAspect.Instance.DiContainer.Bind<ISystemProviderRegister>().FromInstance(this).AsSingle();
            LFrameworkAspect.Instance.DiContainer.Bind<IWorldRegister>().FromInstance(this).AsSingle();
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
            LFrameworkAspect.Instance.DiContainer.Unbind<ISystemProviderRegister>();
            LFrameworkAspect.Instance.DiContainer.Unbind<IWorldRegister>();
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
            foreach (var type in GetHotfixComponentTypes(hotfixComponent))
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
                while (current != null)
                {
                    if (current.Value.GetType() == type)
                    {
                        Log.Error("Game Framework hotfix component type '{0}' is already exist.", type.FullName);
                        return;
                    }

                    current = current.Next;
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
                        // instance fo Hotfix component
                        LFrameworkAspect.Instance.DiContainer.Bind(interfaceType).FromInstance(instance);
                    }
                    else
                    {
                        Log.Fatal("Hotfix component type '{0}' is none bind type.");
                    }
                }
                else
                {
                    LFrameworkAspect.Instance.DiContainer.Bind(hotfixComponentAttribute.BindType)
                        .FromInstance(instance);
                }

                _hotfixComponents.AddLast(instance);
            }

            foreach (var component in _hotfixComponents)
            {
                LFrameworkAspect.Instance.DiContainer.Inject(component);
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
        
        /// <summary>
        /// 获取热更Components
        /// </summary>
        /// <param name="hotfixComponent"></param>
        /// <returns></returns>
        private GameFrameworkLinkedListRange<Type> GetHotfixComponentTypes(HotfixComponent hotfixComponent)
        {
            var result = hotfixComponent.GetTypesFromAttribute<HotfixComponentAttribute>();
            return result ?? default;
        }
    }
}