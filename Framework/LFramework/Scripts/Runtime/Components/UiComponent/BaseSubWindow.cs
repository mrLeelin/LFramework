using System;
using System.Runtime.ExceptionServices;
using Sirenix.OdinInspector;
using UniRx;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class BaseSubWindow:
        UnityEngine.MonoBehaviour
    {
        [BoxGroup("Sub Widnow Key")] [UnityEngine.SerializeField]
        private string key;

        public Window BaseWindow { get; private set; }

        /// <summary>
        /// 自动管理的订阅容器，用于 UniRx 订阅
        /// 在 OnRelease 时会自动释放所有订阅，防止内存泄漏
        /// </summary>
        protected CompositeDisposable Disposables { get; private set; }

        /// <summary>
        /// 子模块启用状态
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// 是否已实际执行事件订阅，保证取消订阅与订阅成对出现
        /// </summary>
        private bool _isSubscribed;
        private bool _isSubscriptionTransition;
        private bool _unsubscribeRequested;

        internal bool RequiresUpdate => this is ISubModuleUpdate;

        internal string GetKey() => key;

        #region Internal Lifecycle

        protected virtual void OnInternalInit(object userData)
        {
        }

        protected virtual void OnInternalOpen(object userData)
        {
        }

        protected virtual void OnInternalClose(bool isShutDown, object userData)
        {
        }

        protected virtual void OnInternalRecycle()
        {
        }

        protected virtual void OnInternalRelease()
        {
        }

        protected virtual void OnInternalPause()
        {
        }

        protected virtual void OnInternalResume()
        {
        }

        protected virtual void OnInternalCover()
        {
        }

        protected virtual void OnInternalReveal()
        {
        }

        protected virtual void OnInternalRefocus(object userData)
        {
        }

        protected virtual void OnInternalDepthChanged()
        {
        }

        protected virtual void OnInternalSubscribe(EventComponent eventComponent)
        {
        }

        protected virtual void OnInternalUnSubscribe(EventComponent eventComponent)
        {
        }

        #endregion

        #region Lifecycle

        public void OnOpen(object userData)
        {
            if (!IsEnabled) return;
            OnInternalOpen(userData);
        }

        public void OnInit(Window window, object userData)
        {
            BaseWindow = window;

            // 防御性释放：如果被重复初始化，先释放旧的 Disposables
            Disposables?.Dispose();
            Disposables = new CompositeDisposable();

            OnInternalInit(userData);
        }

        public void OnClose(bool isShutDown, object userData)
        {
            OnInternalClose(isShutDown, userData);
        }

        public void OnRecycle()
        {
            OnInternalRecycle();
        }

        public void OnRelease()
        {
            Disposables?.Dispose();
            Disposables = null;
            OnInternalRelease();
        }

        public void OnPause()
        {
            if (!IsEnabled) return;
            OnInternalPause();
        }

        public void OnResume()
        {
            if (!IsEnabled) return;
            OnInternalResume();
        }

        public void OnCover()
        {
            if (!IsEnabled) return;
            OnInternalCover();
        }

        public void OnReveal()
        {
            if (!IsEnabled) return;
            OnInternalReveal();
        }

        public void OnRefocus(object userData)
        {
            if (!IsEnabled) return;
            OnInternalRefocus(userData);
        }

        public void OnDepthChanged()
        {
            if (!IsEnabled) return;
            OnInternalDepthChanged();
        }

        public void Subscribe(EventComponent eventComponent)
        {
            if (!IsEnabled || _isSubscribed || _isSubscriptionTransition) return;
            _isSubscribed = true;
            _isSubscriptionTransition = true;
            Exception subscribeError = null;
            try
            {
                OnInternalSubscribe(eventComponent);
            }
            catch (Exception exception)
            {
                subscribeError = exception;
            }
            finally
            {
                _isSubscriptionTransition = false;
            }

            if (subscribeError == null && !_unsubscribeRequested) return;

            _unsubscribeRequested = false;
            Exception cleanupError = null;
            try
            {
                UnSubscribe(eventComponent);
            }
            catch (Exception exception)
            {
                cleanupError = exception;
            }

            if (subscribeError != null && cleanupError != null)
                throw new AggregateException("Sub-window subscription and cleanup both failed.", subscribeError, cleanupError);
            if (subscribeError != null) ExceptionDispatchInfo.Capture(subscribeError).Throw();
            if (cleanupError != null) ExceptionDispatchInfo.Capture(cleanupError).Throw();
        }

        public void UnSubscribe(EventComponent eventComponent)
        {
            // Undo an attempted subscription, including a partially failed one.
            if (!_isSubscribed) return;
            if (_isSubscriptionTransition)
            {
                _unsubscribeRequested = true;
                return;
            }

            _isSubscriptionTransition = true;
            try
            {
                OnInternalUnSubscribe(eventComponent);
                _isSubscribed = false;
                _unsubscribeRequested = false;
            }
            finally
            {
                _isSubscriptionTransition = false;
            }
        }

        internal void DispatchUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!IsEnabled)
            {
                return;
            }

            ((ISubModuleUpdate)this).OnSubModuleUpdate(elapseSeconds, realElapseSeconds);
        }

        #endregion

        /// <summary>
        /// 设置子模块启用状态
        /// </summary>
        /// <param name="en">是否启用</param>
        /// <param name="isEnableGameObject"></param>
        public void SetEnabled(bool en,bool isEnableGameObject  = false)
        {
            if (IsEnabled == en) return;

            IsEnabled = en;
            if (isEnableGameObject)
            {
                gameObject.SetActive(en);
            }
            OnInternalEnabledChanged(en);
        }

        /// <summary>
        /// 启用状态改变时调用
        /// </summary>
        /// <param name="en">是否启用</param>
        protected virtual void OnInternalEnabledChanged(bool en)
        {
        }

        /// <summary>
        /// 便捷方法：获取父窗口的 ViewModel
        /// </summary>
        /// <typeparam name="TViewModel">ViewModel 类型</typeparam>
        /// <returns>ViewModel 实例，如果父窗口不是对应类型则返回 null</returns>
        protected TViewModel GetViewModel<TViewModel>()
            where TViewModel : class, IViewModel
        {
            if (BaseWindow is Window<TViewModel> window)
            {
                return window.ViewModel;
            }
            return null;
        }

        public void SetKey(string newKey) => key = newKey;
    }
}
