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

                LFrameworkAspect.Instance.FrameworkInjector.Inject(world);
                // World interface binding is managed by VContainer scope (registered in Procedure Scope)
                // Scope cleanup is handled by ExitProcedureScope() in HotfixProcedureBase.OnLeave

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

            // VContainer scope cleanup is handled by ExitProcedureScope() in HotfixProcedureBase.OnLeave
            ReferencePool.Release(_worldBase);
            _worldBase = null;
        }
    }
}
