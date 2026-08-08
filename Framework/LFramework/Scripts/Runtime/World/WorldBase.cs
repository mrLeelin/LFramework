using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Event;
using GameFramework.Procedure;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class WorldBase : IWorld
    {
        protected EventComponent EventComponent { get; private set; }
        protected HotfixComponent HotfixComponent { get; private set; }

        private readonly Dictionary<Type, IWorldHelper> _worldHelpers = new();
        private readonly List<IWorldUpdate> _worldUpdates = new();
        private readonly List<IWorldLateUpdate> _worldLateUpdates = new();
        private readonly List<Type> _autoRegisterWorldHelpers = new();
        private ProcedureBase _linkProcedure;
        private readonly Dictionary<Type, Type> _linkWorldHelperAndInterface = new();

        public void Initialized()
        {
            ResolveFrameworkDependencies();
            Subscribe(EventComponent);
            RegisterWorldHelper();
            InitializeAllHelpers();
            OnInitialized();
        }

        /// <summary>
        /// Resolves base-world dependencies directly because concrete worlds can provide their own
        /// generated injector implementation.
        /// </summary>
        private void ResolveFrameworkDependencies()
        {
            EventComponent = LServices.Get<EventComponent>();
            HotfixComponent = LServices.Get<HotfixComponent>();
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

            // 注意：同一个 WorldHelper 可能以多个 Type 作为 key 存在于 _worldHelpers 中
            // （例如：接口类型 + 具体类型），因此需要去重避免重复释放
            var uniqueHelpers = new HashSet<IWorldHelper>(_worldHelpers.Values);
            foreach (var worldHelper in uniqueHelpers)
            {
                ReferencePool.Release(worldHelper);
            }

            _linkWorldHelperAndInterface.Clear();
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
        public virtual TProcedure GetLinkProcedure<TProcedure>() where TProcedure : ProcedureBase
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

        #region Public Method

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

        /// <summary>
        /// 游戏开始
        /// </summary>
        protected void StartHelperGame()
        {
            foreach (var helper in _worldHelpers.Values)
            {
                helper.StartGame();
            }
        }

        /// <summary>
        /// 实例化
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 停止游戏
        /// </summary>
        protected void StopHelperGame()
        {
            foreach (var helper in _worldHelpers.Values)
            {
                helper.StopGame();
            }
        }

        /// <summary>
        /// 注册WorldHelper
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T BuildWorldHelpers<T>() where T : class, IWorldHelper, new()
        {
            var t = ReferencePool.Acquire<T>();
            var interfaceType = GetWorldHelperInterfaces(t.GetType());
            return BuildWorldHelpers(t, interfaceType);
        }

        #endregion


        private T BuildWorldHelpers<T>(T helper, Type interfaceType) where T : IWorldHelper
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
            // interface bind.
            if (interfaceType != null)
            {
                _worldHelpers.Add(interfaceType, t);
            }

            _worldHelpers.Add(typeof(T), t);
            return t;
        }

        private void BuildWorldHelpers(IWorldHelper worldHelperBase, Type interfaceType)
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
            // interface bind.
            if (interfaceType != null)
            {
                _worldHelpers.Add(interfaceType, worldHelperBase);
            }

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
                var interfaceType = GetWorldHelperInterfaces(worldHelper.GetType());
                BuildWorldHelpers(worldHelper, interfaceType);
                if (interfaceType == null)
                {
                    continue;
                }

                _autoRegisterWorldHelpers.Add(interfaceType);
                LServices.Register(interfaceType, worldHelper);
            }

            foreach (var worldHelper in _worldHelpers.Values)
            {
                LServices.Inject(worldHelper);
            }
        }

        private Type GetWorldHelperInterfaces(Type worldHelperType)
        {
            if (worldHelperType == null)
            {
                Log.Error(
                    "[WorldBase] [GetWorldHelperInterfaces] The world helper type is null. so get interface failure. ");
                return null;
            }

            if (_linkWorldHelperAndInterface.TryGetValue(worldHelperType, out var interfaceType))
            {
                return interfaceType;
            }

            interfaceType = worldHelperType.GetDerivedInterfaces(
                typeof(IWorld),
                typeof(IWorldHelper),
                typeof(IWorldUpdate),
                typeof(IWorldLateUpdate),
                typeof(IReference),
                typeof(IDisposable));
            if (interfaceType == null)
            {
                return null;
            }

            CheckAlreadyInterfaces(interfaceType, worldHelperType);
            _linkWorldHelperAndInterface.Add(worldHelperType, interfaceType);
            return interfaceType;
        }

        /// <summary>
        /// 检查重复注册的Interfaces
        /// </summary>
        /// <param name="interfaceType"></param>
        /// <param name="worldHelperType"></param>
        /// <returns></returns>
        private bool CheckAlreadyInterfaces(Type interfaceType, Type worldHelperType)
        {
            foreach (var kPair in _linkWorldHelperAndInterface)
            {
                if (kPair.Value == interfaceType)
                {
                    Log.Error(
                        $"[WorldBase] [CheckAlreadyInterfaces]  The Interface '{nameof(interfaceType)}' already exists in '{nameof(kPair.Key)}' but you use '{nameof(worldHelperType)}'.");
                    return false;
                }
            }

            return true;
        }

        private void UnRegisterWorldHelper()
        {
            if (_autoRegisterWorldHelpers.Count == 0)
            {
                return;
            }

            foreach (var @interfaceType in _autoRegisterWorldHelpers)
            {
                LServices.Unregister(@interfaceType);
            }
        }
    }
}