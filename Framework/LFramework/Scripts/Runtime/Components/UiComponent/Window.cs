using System;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Event;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Object = System.Object;

namespace LFramework.Runtime
{
    public abstract partial class Window : UIBehaviour
    {
        private const int DepthFactor = 10;
        private UIComponent _uiComponent;
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private bool _isClosing;
        private readonly List<Canvas> _canvasContainer = new List<Canvas>();
        private IAnimation _windowAnimation;
        private WindowAnimationState _windowAnimationState = WindowAnimationState.None;
        private NativeReference _customUserData;
        public int OriginalDepth { get; private set; }

        public int Depth => _canvas.sortingOrder;

        /// <summary>
        ///    Canvas
        /// </summary>
        public Canvas Canvas => _canvas;

        protected virtual bool AutoPlayEnterAnimation { get; set; } = true;

        /// <summary>
        ///    动画状态
        /// </summary>
        public WindowAnimationState WindowAnimationState => _windowAnimationState;

        protected IAnimation WindowAnimation => _windowAnimation;

        private UIComponent UIComponent
        {
            get { return _uiComponent ??= LFrameworkAspect.Instance.Get<UIComponent>(); }
        }

        protected virtual string EnterAnimationName { get; } = "Enter";
        protected virtual string ExitAnimationName { get; } = "Exit";

        /// <summary>
        /// 获取自定义信息
        /// </summary>
        /// <typeparam name="TUserData"></typeparam>
        /// <returns></returns>
        protected NativeReference<TUserData> GetUserData<TUserData>()
            where TUserData : NativeReference<TUserData>, new()
        {
            if (_customUserData is NativeReference<TUserData> userData)
            {
                return userData;
            }

            Log.Error($"[Window] The get userData is not {nameof(TUserData)} so failure.");
            return null;
        }


        #region Lifecycle

        protected virtual void OnInit(object userData)
        {
        }

        protected virtual void OnOpen(object userData)
        {
        }

        protected virtual void OnClose(bool isShutDown, object userData)
        {
        }

        protected virtual void OnRecycle()
        {
        }

        protected virtual void OnRelease()
        {
        }

        protected virtual void OnPause()
        {
        }

        protected virtual void OnResume()
        {
        }

        protected virtual void OnCover()
        {
        }

        protected virtual void OnReveal()
        {
        }

        protected virtual void OnRefocus(object userData)
        {
        }

        protected virtual void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
        }

        protected virtual void Subscribe(EventComponent eventComponent)
        {
        }


        protected virtual void UnSubscribe(EventComponent eventComponent)
        {
        }

        protected virtual void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
        }

        #endregion

        #region Inter Lifecycle

        public sealed override void OnInterInit(object userData)
        {
            base.OnInterInit(userData);
            if (userData is NativeReference nativeReference)
            {
                _customUserData = nativeReference;
            }

            _subModuleKeyMap.Clear();
            _windowAnimation = gameObject.GetComponent<IAnimation>();
            _canvas = gameObject.GetOrAddComponent<Canvas>();
            _canvas.overrideSorting = true;
            _canvas.vertexColorAlwaysGammaSpace = UIComponent.VertexColorAlwaysGammaSpace;
            OriginalDepth = _canvas.sortingOrder;
            _canvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            CacheRectTransform.anchorMax = Vector2.one;
            CacheRectTransform.anchorMin = Vector2.zero;
            CacheRectTransform.anchoredPosition = Vector2.zero;
            CacheRectTransform.sizeDelta = Vector2.zero;
            gameObject.GetOrAddComponent<GraphicRaycaster>();
            OnInit(userData);
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnInit(this, userData);
                _subModuleKeyMap.Add(subWindow.GetType(), subWindow);
            }
        }

        public sealed override void OnInterOpen(object userData)
        {
            if (userData is NativeReference nativeReference)
            {
                _customUserData = nativeReference;
            }

            PrepareForEnterAnimation();
            base.OnInterOpen(userData);
            InterSubscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            _windowAnimationState = WindowAnimationState.None;
            _isClosing = false;
            OnOpen(userData);
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnOpen(userData);
            }

            RestoreAfterEnterAnimation();
            if (AutoPlayEnterAnimation)
            {
                PlayEnterAnimation(userData);
            }
        }

        public sealed override void OnInterClose(bool isShutDown, object userData)
        {
            InterUnSubscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnClose(isShutDown, userData);
            }

            OnClose(isShutDown, userData);
            _customUserData?.Release();
            _customUserData = null;
            this.RemoveChild();
            base.OnInterClose(isShutDown, userData);
        }

        public sealed override void OnInterDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            var oldDepth = Depth;
            base.OnInterDepthChanged(uiGroupDepth, depthInUIGroup);
            var deltaDepth = DefaultUIGroupHelper.DepthFactor * uiGroupDepth + DepthFactor
                * depthInUIGroup - oldDepth + OriginalDepth;
            GetComponentsInChildren(true, _canvasContainer);
            for (var index = 0; index < _canvasContainer.Count; index++)
            {
                var canvas = _canvasContainer[index];
                canvas.sortingOrder += deltaDepth;
                canvas.sortingLayerName = "UI";
                if (index == 0)
                {
                    UIFormDepthUpdatedEventArg openUIFormSuccessEventArgs = UIFormDepthUpdatedEventArg.Create(
                        this.UIForm.SerialId
                        , this.UIForm.UIFormAssetName, canvas.sortingOrder, this.UIForm);
                    Fire(openUIFormSuccessEventArgs);
                }
            }

            _canvasContainer.Clear();
            OnDepthChanged(uiGroupDepth, depthInUIGroup);
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnDepthChanged();
            }
        }

        private void InterSubscribe(EventComponent eventComponent)
        {
            Subscribe(eventComponent);
            foreach (var subWindow in subModuleList)
            {
                subWindow.Subscribe(eventComponent);
            }
        }

        private void InterUnSubscribe(EventComponent eventComponent)
        {
            UnSubscribe(eventComponent);
            foreach (var subWindow in subModuleList)
            {
                subWindow.UnSubscribe(eventComponent);
            }
        }

        public sealed override void OnInterUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnInterUpdate(elapseSeconds, realElapseSeconds);
            OnUpdate(elapseSeconds, realElapseSeconds);
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        public sealed override void OnInterRecycle()
        {
            base.OnInterRecycle();
            OnRecycle();
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnRecycle();
            }
        }

        public sealed override void OnInterRelease()
        {
            base.OnInterRelease();
            OnRelease();
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnRelease();
            }
        }

        public sealed override void OnInterPause()
        {
            base.OnInterPause();
            OnPause();
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnPause();
            }
        }

        public sealed override void OnInterResume()
        {
            base.OnInterResume();
            OnResume();
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnResume();
            }
        }

        public sealed override void OnInterCover()
        {
            base.OnInterCover();
            OnCover();
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnCover();
            }
        }

        public sealed override void OnInterReveal()
        {
            base.OnInterReveal();
            OnReveal();
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnReveal();
            }
        }

        public sealed override void OnInterRefocus(object userData)
        {
            base.OnInterRefocus(userData);
            OnRefocus(userData);
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OnRefocus(userData);
            }
        }

        #endregion


        protected void Fire(GameEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return;
            }

            LFrameworkAspect.Instance.Get<EventComponent>().Fire(this, eventArgs);
        }

        protected void FireNow(GameEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                return;
            }

            LFrameworkAspect.Instance.Get<EventComponent>().FireNow(this, eventArgs);
        }

        public virtual void ImmediateClose()
        {
            Fire(WindowAnimationExitStartArg.Create(this.UIForm, null));
            _windowAnimationState = WindowAnimationState.None;
            _isClosing = true;
            UIComponent.CloseUIForm(this.UIForm.SerialId);
        }

        public void CloseSelfNotAnimation()
        {
            UIComponent.CloseUIForm(this.UIForm.SerialId);
        }

        public override void CloseSelf()
        {
            if (_isClosing)
            {
                return;
            }

            Fire(WindowAnimationExitStartArg.Create(this.UIForm, null));

            void Close()
            {
                _windowAnimationState = WindowAnimationState.None;
                _isClosing = false;
                UIComponent.CloseUIForm(this.UIForm.SerialId);
            }

            if (_windowAnimation != null)
            {
                _isClosing = true;
                _windowAnimationState = WindowAnimationState.Closing;
                OnAnimationExitStart();
                _windowAnimation.PlayAnimation(ExitAnimationName, Close);
            }
            else
            {
                _isClosing = true;
                Close();
            }
        }

        public override void CloseSelf(object userData)
        {
            if (_isClosing)
            {
                return;
            }

            Fire(WindowAnimationExitStartArg.Create(this.UIForm, null));

            void Close()
            {
                _windowAnimationState = WindowAnimationState.None;
                _isClosing = false;
                UIComponent.CloseUIForm(this.UIForm.SerialId, userData);
            }

            if (_windowAnimation != null)
            {
                _isClosing = true;
                _windowAnimationState = WindowAnimationState.Closing;
                OnAnimationExitStart();
                _windowAnimation.PlayAnimation(ExitAnimationName, Close);
            }
            else
            {
                Close();
            }
        }

        protected void PlayEnterAnimation(object userData)
        {
            if (_windowAnimation == null)
            {
                return;
            }

            _windowAnimationState = WindowAnimationState.Opening;

            void AnimationEnterCompleted()
            {
                _windowAnimationState = WindowAnimationState.None;
                OnAnimationEnterCompleted();
                LFrameworkAspect.Instance
                    .Fire(this,
                        WindowAnimationEnterCompletedArg.Create(this.UIForm, userData));
            }

            _windowAnimation.PlayAnimation(EnterAnimationName, AnimationEnterCompleted);
        }

        protected virtual void OnAnimationEnterCompleted()
        {
        }

        protected virtual void OnAnimationExitStart()
        {
        }


        /// <summary>
        ///   是否可以被外部关闭
        /// </summary>
        /// <returns></returns>
        public bool CanBeCloseExternally()
        {
            if (_windowAnimationState != WindowAnimationState.None)
            {
                return false;
            }

            return CanBeCloseExternallyInternal();
        }

        protected virtual bool CanBeCloseExternallyInternal() => true;

        private void PrepareForEnterAnimation()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        private void RestoreAfterEnterAnimation()
        {
            if (_canvasGroup == null)
            {
                return;
            }

            _canvasGroup.alpha = 1f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        protected T As<T>(object userData)
            where T : class
        {
            if (userData is T result)
            {
                return result;
            }

            Log.Fatal("UserData is not {0}", typeof(T).FullName);
            return null;
        }
    }

    public abstract class Window<T> : Window
        where T : class, IViewModel
    {
        public sealed override Type ViewModelType => typeof(T);

        public sealed override IViewModel ViewModelObject
        {
            get => base.ViewModelObject;
            set => base.ViewModelObject = value;
        }

        public T ViewModel => ViewModelObject as T;

        protected sealed override void Bind(IViewModel viewModel, CompositeDisposable compositeDisposable)
        {
            Bind(viewModel as T, compositeDisposable);
            base.Bind(viewModel, compositeDisposable);
        }

        protected virtual void Bind(T viewModel, CompositeDisposable compositeDisposable)
        {
        }
    }
}
