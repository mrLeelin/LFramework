using System;
using GameFramework;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 资源加载 Handle 基类，支持 ReferencePool 对象池化
    /// </summary>
    public abstract class ResourceHandleBase : IDisposable, IReference
    {
        protected bool _isDone;
        protected bool _isDisposed;
        protected string _error;
        protected float _progress;
        private Action _onRelease;
        private bool _isFromPool;
        private bool _releaseFinalized;
        private int _completionNotificationDepth;

        /// <summary>
        /// 是否加载完成
        /// </summary>
        public bool IsDone => _isDone;

        /// <summary>
        /// 是否加载成功
        /// </summary>
        public bool IsSucceed => _isDone && _error == null;

        /// <summary>
        /// 加载进度
        /// </summary>
        public float Progress => _progress;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string Error => _error;

        /// <summary>
        /// 设置加载进度
        /// </summary>
        public void SetProgress(float progress)
        {
            if (!_isDone)
            {
                _progress = progress;
            }
        }

        /// <summary>
        /// 标记此 Handle 来自对象池
        /// </summary>
        public void MarkFromPool() => _isFromPool = true;

        /// <summary>
        /// 注册底层资源释放回调（由 Helper 在加载完成时调用）
        /// </summary>
        public void RegisterReleaseAction(Action onRelease)
        {
            _onRelease = onRelease;
        }

        /// <summary>
        /// 设置错误信息并标记完成
        /// </summary>
        public void SetError(string errorMessage)
        {
            if (!TryBeginCompletion())
            {
                return;
            }

            _error = errorMessage;
            try
            {
                SetErrorInternal(errorMessage);
            }
            finally
            {
                EndCompletion();
            }
        }

        /// <summary>
        /// 子类实现的错误处理逻辑
        /// </summary>
        protected abstract void SetErrorInternal(string errorMessage);

        /// <summary>
        /// Cancels a waiter when the handle is released before its backend operation completes.
        /// The backend still owns the handle until it reports completion.
        /// </summary>
        protected virtual void CancelWaiterInternal()
        {
        }

        /// <summary>
        /// Clears a completed result when release is finalized.
        /// </summary>
        protected virtual void ReleaseResultInternal()
        {
        }

        /// <summary>
        /// Starts a terminal completion notification.
        /// </summary>
        protected bool TryBeginCompletion()
        {
            if (_isDone)
            {
                return false;
            }

            _isDone = true;
            _completionNotificationDepth++;
            return true;
        }

        /// <summary>
        /// Ends a terminal completion notification and performs any deferred release.
        /// </summary>
        protected void EndCompletion()
        {
            _completionNotificationDepth--;
            TryFinalizeRelease();
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release() => Dispose();

        /// <summary>
        /// 释放资源并归还对象池
        /// </summary>
        public virtual void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (!_isDone)
            {
                _completionNotificationDepth++;
                try
                {
                    CancelWaiterInternal();
                }
                finally
                {
                    _completionNotificationDepth--;
                }
            }

            TryFinalizeRelease();
        }

        private void TryFinalizeRelease()
        {
            if (!_isDisposed || !_isDone || _releaseFinalized || _completionNotificationDepth != 0)
            {
                return;
            }

            _releaseFinalized = true;
            Action onRelease = _onRelease;
            _onRelease = null;
            try
            {
                ReleaseResultInternal();
            }
            finally
            {
                try
                {
                    onRelease?.Invoke();
                }
                finally
                {
                    if (_isFromPool)
                    {
                        ReferencePool.Release(this);
                    }
                }
            }
        }

        /// <summary>
        /// 重置状态（IReference 接口，由 ReferencePool 归还时调用）
        /// </summary>
        public virtual void Clear()
        {
            _isDone = false;
            _isDisposed = false;
            _error = null;
            _progress = 0f;
            _onRelease = null;
            _isFromPool = false;
            _releaseFinalized = false;
            _completionNotificationDepth = 0;
        }
    }
}
