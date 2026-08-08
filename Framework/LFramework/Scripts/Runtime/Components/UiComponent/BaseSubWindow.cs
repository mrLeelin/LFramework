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

        protected virtual void OnInternalUpdate(float elapseSeconds, float realElapseSeconds)
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
            if (!IsEnabled) return;
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
            if (!IsEnabled) return;
            OnInternalSubscribe(eventComponent);
        }

        public void UnSubscribe(EventComponent eventComponent)
        {
            if (!IsEnabled) return;
            OnInternalUnSubscribe(eventComponent);
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!IsEnabled) return;
            OnInternalUpdate(elapseSeconds, realElapseSeconds);
        }

        #endregion

        /// <summary>
        /// 设置子模块启用状态
        /// </summary>
        /// <param name="en">是否启用</param>
        /// <param name="isEnableGameObject"></param>
        public void SetEnabled(bool en,bool isEnableGameObject  = false)
        {
            if (IsEnabled == enabled) return;

            IsEnabled = enabled;
            if (isEnableGameObject)
            {
                gameObject.SetActive(enabled);
            }
            OnInternalEnabledChanged(enabled);
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
