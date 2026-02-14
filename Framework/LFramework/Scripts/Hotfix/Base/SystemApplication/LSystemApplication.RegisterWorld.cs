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

                LFrameworkAspect.Instance.DiContainer.Inject(world);
                var interfaceType =
                    providerType.GetDerivedInterfaces(typeof(IWorld), typeof(IReference), typeof(IDisposable));
                if (interfaceType != null)
                {
                    LFrameworkAspect.Instance.DiContainer.Bind(interfaceType).FromInstance(world);
                }

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
            if (interfaceType != null)
            {
                if (!LFrameworkAspect.Instance.DiContainer.Unbind(interfaceType))
                {
                    Log.Fatal($"Un bind '{interfaceType}' 'World' error.");
                }
            }

            ReferencePool.Release(_worldBase);
            _worldBase = null;
        }
    }
}