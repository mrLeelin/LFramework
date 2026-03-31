using Sirenix.OdinInspector;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    [PreLoadZenject]
    public abstract partial class BaseSubWindow:
        UnityEngine.MonoBehaviour
    {
        [BoxGroup("Sub Widnow Key")] [UnityEngine.SerializeField]
        private string key;

        public Window BaseWindow { get; private set; }


        internal string GetKey() => key;

        #region Children

        protected virtual void OnInternalInit(object userData)
        {
        }

        protected virtual void OnInternalOpen(object userData)
        {
        }

        protected virtual void OnInternalClose(bool isShutDown, object userData)
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

        public void OUpdate(float elapseSeconds, float realElapseSeconds)
        {
            OnInternalUpdate(elapseSeconds, realElapseSeconds);
        }
        
        public void SetKey(string newKey) => key = newKey;
    }
}