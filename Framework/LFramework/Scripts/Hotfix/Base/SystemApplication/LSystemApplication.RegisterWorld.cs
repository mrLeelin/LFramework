using System;
using GameFramework;
using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix
{
    public partial class LSystemApplication
    {
        private WorldBase _worldBase;

        /// <summary>
        /// 注册世界基类
        /// </summary>
        /// <param name="procedureState"></param>
        public IWorld TryRegisterWorld(int procedureState)
        {
            var allBelongToProcedureProviders = HotfixComponent.GetTypesFromAttribute<BelongToAttribute>();
            if (allBelongToProcedureProviders is not { Count: > 0 })
            {
                return null;
            }

            foreach (var providerType in allBelongToProcedureProviders.Value)
            {
                if (!typeof(WorldBase).IsAssignableFrom(providerType))
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


                var instance = /*Activator.CreateInstance(providerType); */ ReferencePool.Acquire(providerType);
                if (instance == null)
                {
                    continue;
                }


                if (!(instance is WorldBase world))
                {
                    Log.Fatal($"BelongToAttribute '{providerType.FullName}' is none impl ISystemProvider");
                    continue;
                }

                Injection.Inject(world);
                var interfaceType =
                    providerType.GetDerivedInterfaces(typeof(IWorld), typeof(IReference), typeof(IDisposable));
                if (interfaceType != null)
                {
                    LServices.Register(interfaceType, world);
                }

                LServices.Register(providerType, world);
                world.Initialized();
                _worldBase = world;
                return world;
            }

            return null;
        }

        public void TryUnRegisterWorld()
        {
            if (_worldBase == null)
            {
                return;
            }

            // 先获取类型信息并解绑 DI，再释放回对象池
            var interfaceType = _worldBase.GetType()
                .GetDerivedInterfaces(typeof(IWorld), typeof(IReference), typeof(IDisposable));
            UnRegisterWorldService(_worldBase);
            if (interfaceType != null)
            {
                LServices.Unregister(interfaceType);
            }

            Injection.ClearReflectionCache(_worldBase.GetType());
            ReferencePool.Release(_worldBase);
            _worldBase = null;
        }

        private static void UnRegisterWorldService(WorldBase world)
        {
            if (world == null)
            {
                return;
            }

            var worldType = world.GetType();
            LServices.Unregister(worldType);
            var interfaceType =
                worldType.GetDerivedInterfaces(typeof(IWorld), typeof(IReference), typeof(IDisposable));
            if (interfaceType != null)
            {
                LServices.Unregister(interfaceType);
            }
        }
    }
}
