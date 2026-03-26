using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public abstract class WorldBase : IWorld
    {
        [Inject] protected EventComponent EventComponent { get; }
        [Inject] protected HotfixComponent HotfixComponent { get; }

        private readonly Dictionary<Type, IWorldHelper> _worldHelpers = new();
        private readonly List<IWorldUpdate> _worldUpdates = new();
        private readonly List<IWorldLateUpdate> _worldLateUpdates = new();
        private readonly List<Type> _autoRegisterWorldHelpers = new();
        private ProcedureBase _linkProcedure;

        public void Initialized()
        {
            Subscribe(EventComponent);
            RegisterWorldHelper();
            InitializeAllHelpers();
            OnInitialized();
        }

        /*
        public void Dispose()
        {

        }
        */
        public void Clear()
        {
            UnSubscribe(EventComponent);
            // 先解绑 DI，再释放回对象池
            UnRegisterWorldHelper();
            foreach (var worldHelper in _worldHelpers.Values)
            {
                ReferencePool.Release(worldHelper);
            }

            _worldHelpers.Clear();
            _worldUpdates.Clear();
            _worldLateUpdates.Clear();
            _autoRegisterWorldHelpers.Clear();
            OnDeInitialized();
        }

        public void LinkProcedure(ProcedureBase procedure)
        {
            _linkProcedure = procedure;
            if (_linkProcedure == null)
            {
                Log.Error($"The link procedure is null. '{this.GetType().FullName}'");
            }
        }

        protected virtual void OnInitialized()
        {
        }

        protected virtual void OnDeInitialized()
        {
        }

        /// <summary>
        /// 获取链接的流程
        /// </summary>
        /// <typeparam name="TProcedure"></typeparam>
        /// <returns></returns>
        protected virtual TProcedure GetLinkProcedure<TProcedure>() where TProcedure : ProcedureBase
        {
            if (_linkProcedure == null)
            {
                return null;
            }

            if (_linkProcedure is not TProcedure result)
            {
                Log.Error($"The link procedure is not '{typeof(TProcedure).FullName}'");
                return null;
            }

            return result;
        }

        /// <summary>
        /// 轮询
        /// </summary>
        /// <param name="elapseSeconds"></param>
        /// <param name="realElapseSeconds"></param>
        public virtual void Update(float elapseSeconds, float realElapseSeconds)
        {
            foreach (var worldUpdate in _worldUpdates)
            {
                worldUpdate.OnUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        public virtual void LateUpdate()
        {
            foreach (var worldLateUpdate in _worldLateUpdates)
            {
                worldLateUpdate.LateUpdate();
            }
        }

        protected virtual void Subscribe(EventComponent eventComponent)

        {
        }

        protected virtual void UnSubscribe(EventComponent eventComponent)
        {
        }

        /// <summary>
        /// 下一帧抛出事件
        /// </summary>
        /// <param name="gameEventArgs"></param>
        protected void Fire(GameEventArgs gameEventArgs)
        {
            EventComponent.Fire(this, gameEventArgs);
        }

        /// <summary>
        /// 直接抛出事件
        /// </summary>
        /// <param name="gameEventArgs"></param>
        protected void FireNow(GameEventArgs gameEventArgs)
        {
            EventComponent.FireNow(this, gameEventArgs);
        }

        public T GetWorldHelper<T>() where T : class, IWorldHelper
        {
            if (_worldHelpers.TryGetValue(typeof(T), out var result))
            {
                return (T)result;
            }

            Log.Fatal($"The Get world helper is null '{typeof(T).FullName}'");
            return null;
        }

        protected void StartHelperGame()
        {
            foreach (var helper in _worldHelpers.Values)
            {
                helper.StartGame();
            }
        }

        protected UniTask InstantiateHelperGame()
        {
            var tasks = new List<UniTask>(_worldHelpers.Count);
            foreach (var helper in _worldHelpers.Values)
            {
                var t = helper.InstantiateWorld();
                tasks.Add(t);
            }

            return UniTask.WhenAll(tasks);
        }

        protected void StopHelperGame()
        {
            foreach (var helper in _worldHelpers.Values)
            {
                helper.StopGame();
            }
        }

        protected T BuildWorldHelpers<T>() where T : class, IWorldHelper, new()
        {
            var t = ReferencePool.Acquire<T>();
            return BuildWorldHelpers(t);
        }

        private T BuildWorldHelpers<T>(T helper) where T : IWorldHelper
        {
            var t = helper;
            if (t is not IWorldHelper worldHelper)
            {
                return t;
            }

            if (t is IWorldUpdate worldUpdate)
            {
                _worldUpdates.Add(worldUpdate);
            }

            if (t is IWorldLateUpdate worldLateUpdate)
            {
                _worldLateUpdates.Add(worldLateUpdate);
            }

            worldHelper.SetWorld(this);
            _worldHelpers.Add(typeof(T), t);

            return t;
        }

        private void BuildWorldHelpers(IWorldHelper worldHelperBase)
        {
            if (worldHelperBase is IWorldUpdate worldUpdate)
            {
                _worldUpdates.Add(worldUpdate);
            }

            if (worldHelperBase is IWorldLateUpdate worldLateUpdate)
            {
                _worldLateUpdates.Add(worldLateUpdate);
            }

            worldHelperBase.SetWorld(this);
            _worldHelpers.Add(worldHelperBase.GetType(), worldHelperBase);
        }

        private void InitializeAllHelpers()
        {
            foreach (var worldHelper in _worldHelpers.Values)
            {
                worldHelper.Initialize();
            }
        }

        private void RegisterWorldHelper()
        {
            _autoRegisterWorldHelpers.Clear();
            var worldType = this.GetType();
            var autoWorldHelper = AutoRegisterWorldHelper.GetRegisterWorldHelper(worldType);
            foreach (var worldHelper in autoWorldHelper)
            {
                BuildWorldHelpers(worldHelper);
                var interfaceType = worldHelper.GetType().GetDerivedInterfaces(
                    typeof(IWorldHelper), 
                    typeof(IWorldUpdate),
                    typeof(IWorldLateUpdate),
                    typeof(IReference),
                    typeof(IDisposable));
                if (interfaceType == null)
                {
                    continue;
                }

                _autoRegisterWorldHelpers.Add(interfaceType);
                LFrameworkAspect.Instance.DiContainer.Bind(interfaceType).FromInstance(worldHelper);
            }

            foreach (var worldHelper in _worldHelpers.Values)
            {
                LFrameworkAspect.Instance.DiContainer.Inject(worldHelper);
            }
        }

        private void UnRegisterWorldHelper()
        {
            if (_autoRegisterWorldHelpers.Count == 0)
            {
                return;
            }

            foreach (var @interfaceType in _autoRegisterWorldHelpers)
            {
                LFrameworkAspect.Instance.DiContainer.Unbind(@interfaceType);
            }
        }
    }
}