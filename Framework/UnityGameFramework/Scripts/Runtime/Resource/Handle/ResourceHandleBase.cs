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
        public void SetProgress(float progress) => _progress = progress;

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
            _error = errorMessage;
            _isDone = true;
            SetErrorInternal(errorMessage);
        }

        /// <summary>
        /// 子类实现的错误处理逻辑
        /// </summary>
        protected abstract void SetErrorInternal(string errorMessage);

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Release() => Dispose();

        /// <summary>
        /// 释放资源并归还对象池
        /// </summary>
        public virtual void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _onRelease?.Invoke();
            _onRelease = null;
            if (_isFromPool)
            {
                ReferencePool.Release(this);
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
        }
    }
}
