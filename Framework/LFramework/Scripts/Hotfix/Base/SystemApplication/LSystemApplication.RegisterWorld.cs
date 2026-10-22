using System;
using GameFramework;
using LFramework.Runtime;
using UnityGameFramework.Runtime;

namespace LFramework.Hotfix
{
    public partial class LSystemApplication
    {
        private WorldBase _worldBase;
        private bool _unregisteringWorld;

        /// <summary>
        /// 注册世界基类
        /// </summary>
        /// <param name="procedureState"></param>
        public IWorld TryRegisterWorld(int procedureState)
        {
            if (_worldBase != null)
            {
                throw new InvalidOperationException("Unregister the current World before entering another World.");
            }

            var ownsTransition = !_residentTransitionPrepared;
            PrepareWorldResidents(procedureState);
            var worldType = _residentWorldType;
            if (worldType == null)
            {
                if (ownsTransition)
                {
                    try
                    {
                        CompleteWorldResidentTransition();
                    }
                    catch (Exception completeException)
                    {
                        // 提交失败不会自动回滚，自身发起的事务需在此取消
                        try
                        {
                            CancelWorldResidentTransition();
                        }
                        catch (Exception rollbackException)
                        {
                            throw new AggregateException(
                                "World resident transition failed and rollback failed.",
                                completeException,
                                rollbackException);
                        }

                        throw;
                    }
                }

                return null;
            }

            WorldBase world = null;
            try
            {
                world = (WorldBase)ReferencePool.Acquire(worldType);
                Injection.Inject((object)world);
                var interfaceType =
                    worldType.GetDerivedInterfaces(typeof(IWorld), typeof(IReference), typeof(IDisposable), typeof(IInjectable));
                if (interfaceType != null)
                {
                    LServices.Register(interfaceType, world);
                }

                LServices.Register(worldType, world);
                world.Initialized();
                _worldBase = world;
                if (ownsTransition)
                {
                    CompleteWorldResidentTransition();
                }
            }
            catch (Exception registrationException)
            {
                var errors = new System.Collections.Generic.List<Exception> { registrationException };
                var worldCleanupSucceeded = true;
                try
                {
                    if (world != null)
                    {
                        _worldBase = world;
                        TryUnRegisterWorld();
                    }
                }
                catch (Exception rollbackException)
                {
                    worldCleanupSucceeded = false;
                    errors.Add(rollbackException);
                }

                if (ownsTransition && worldCleanupSucceeded)
                {
                    try
                    {
                        CancelWorldResidentTransition();
                    }
                    catch (Exception rollbackException)
                    {
                        errors.Add(rollbackException);
                    }
                }

                if (errors.Count > 1) throw new AggregateException("World entry and cleanup failed.", errors);
                throw;
            }

            return world;
        }

        public void TryUnRegisterWorld()
        {
            _residentHost?.EnsureLifecycleIdle();
            if (_worldBase == null || _unregisteringWorld)
            {
                return;
            }

            var world = _worldBase;
            _unregisteringWorld = true;
            try
            {
                ExitWorldResidents();
                UnRegisterWorldService(world);
                Injection.ClearReflectionCache(world.GetType());
                ReferencePool.Release(world);
                _worldBase = null;
            }
            finally
            {
                _unregisteringWorld = false;
                // 失败时保留尚需重试的实例及服务；驻留实例在下一目标提交时才决定去留。
                if (_worldBase == null) UnRegisterWorldService(world);
            }
        }

        private static void UnRegisterWorldService(WorldBase world)
        {
            if (world == null)
            {
                return;
            }

            var worldType = world.GetType();
            UnregisterWorldAlias(worldType, world);
            var interfaceType =
                worldType.GetDerivedInterfaces(typeof(IWorld), typeof(IReference), typeof(IDisposable), typeof(IInjectable));
            if (interfaceType != null)
            {
                UnregisterWorldAlias(interfaceType, world);
            }
        }

        private static void UnregisterWorldAlias(Type type, WorldBase world)
        {
            if (LServices.TryGet(type, null, out var current) && ReferenceEquals(current, world))
            {
                LServices.Unregister(type);
            }
        }
    }
}
