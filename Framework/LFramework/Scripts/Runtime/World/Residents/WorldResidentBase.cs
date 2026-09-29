using System;
using System.Collections.Generic;
using System.Threading;

namespace LFramework.Runtime
{
    /// <summary>由宿主创建和释放的跨 World 对象，业务代码无需手动管理实例。</summary>
    public abstract class WorldResidentBase
    {
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly CancellationToken _token;
        private bool _initialized;
        private bool _shutdown;

        protected WorldResidentBase()
        {
            _token = _lifetime.Token;
        }

        /// <summary>初始化成功且尚未关闭。</summary>
        public bool IsActive { get; private set; }

        /// <summary>关闭前取消；异步回调仍须检查取消状态并回收迟到的资源。</summary>
        protected CancellationToken LifetimeToken => _token;

        /// <summary>依赖全部注册并注入后调用；仅执行一次。</summary>
        protected virtual void OnInitialize() { }

        /// <summary>目标 World 初始化及流程注入完成后调用；每次进入匹配的 World 都会通知。</summary>
        protected virtual void OnEnterWorld(WorldBase world) { }

        /// <summary>在当前 World 和 Provider 清理前调用；进入失败也会补偿调用，退出失败可重试。</summary>
        protected virtual void OnExitWorld(WorldBase world) { }

        /// <summary>释放业务资源；初始化失败时也会调用，需允许部分初始化状态。</summary>
        protected virtual void OnShutdown() { }

        internal void EnterWorldResident(WorldBase world) => OnEnterWorld(world);

        internal void ExitWorldResident(WorldBase world) => OnExitWorld(world);

        internal void InitializeResident()
        {
            if (_shutdown || _initialized)
                throw new InvalidOperationException("Resident cannot be initialized twice.");
            _initialized = true;
            OnInitialize();
            IsActive = true;
        }

        internal void ShutdownResident()
        {
            if (_shutdown) return;
            _shutdown = true;
            IsActive = false;
            var errors = new List<Exception>();
            try { _lifetime.Cancel(); } catch (Exception exception) { errors.Add(exception); }
            try { OnShutdown(); } catch (Exception exception) { errors.Add(exception); }
            finally { _lifetime.Dispose(); }
            if (errors.Count > 0) throw new AggregateException("Resident shutdown failed.", errors);
        }
    }
}
