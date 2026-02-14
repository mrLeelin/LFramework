using System;
using GameFramework.UI;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract class UIBehaviour : ViewBehaviour, IUIBehaviour, IUILifecycle
    {
        private int _originalLayer;
        private bool _visible;

        public IUIForm UIForm { get; private set; }

        public Transform CacheTransform { get; private set; }

        public RectTransform CacheRectTransform { get; private set; }

        public bool Available { get; private set; }

        public bool Visible
        {
            get => Available && _visible;
            set
            {
                if (!Available)
                {
                    Log.Warning($"UI form '{UIForm.UIFormAssetName}' is not available.");
                    return;
                }

                if (_visible == value)
                {
                    return;
                }

                _visible = value;
                InternalSetVisible(value);
            }
        }

        public abstract void CloseSelf();
        public abstract void CloseSelf(object userData);

        public virtual void OnInit(object userData)
        {
            if (CacheTransform == null)
            {
                CacheTransform = transform;
            }

            if (CacheRectTransform == null)
            {
                CacheRectTransform = this.GetComponent<RectTransform>();
            }

            UIForm = this.GetComponent<UIForm>();
            _originalLayer = gameObject.layer;
        }

        public virtual void OnRecycle()
        {
        }

        public virtual void OnRelease()
        {
            
        }

        public virtual void OnOpen(object userData)
        {
            Available = true;
            Visible = true;
        }

        public virtual void OnClose(bool isShutDown, object userData)
        {
            //gameObject.SetLayer(_originalLayer, true);
            Visible = false;
            Available = false;
        }

        public virtual void OnPause()
        {
            //Visible = false;
        }

        public virtual void OnResume()
        {
            //Visible = true;
        }

        public virtual void OnCover()
        {
        }

        public virtual void OnReveal()
        {
        }

        public virtual void OnRefocus(object userData)
        {
        }

        public virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        public virtual void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
        }
        
        protected virtual void InternalSetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}