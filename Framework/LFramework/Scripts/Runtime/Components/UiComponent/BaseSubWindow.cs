using Sirenix.OdinInspector;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract partial class BaseSubWindow:
        UnityEngine.MonoBehaviour
    {
        [BoxGroup("Sub Widnow Key")] [UnityEngine.SerializeField]
        private string key;

        public Window BaseWindow { get; private set; }


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
            OnInternalOpen(userData);
        }

        public void OnInit(Window window, object userData)
        {
            BaseWindow = window;
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
            OnInternalRelease();
        }

        public void OnPause()
        {
            OnInternalPause();
        }

        public void OnResume()
        {
            OnInternalResume();
        }

        public void OnCover()
        {
            OnInternalCover();
        }

        public void OnReveal()
        {
            OnInternalReveal();
        }

        public void OnRefocus(object userData)
        {
            OnInternalRefocus(userData);
        }

        public void OnDepthChanged()
        {
            OnInternalDepthChanged();
        }

        public void Subscribe(EventComponent eventComponent)
        {
            OnInternalSubscribe(eventComponent);
        }

        public void UnSubscribe(EventComponent eventComponent)
        {
            OnInternalUnSubscribe(eventComponent);
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            OnInternalUpdate(elapseSeconds, realElapseSeconds);
        }

        #endregion
        
        public void SetKey(string newKey) => key = newKey;
    }
}
