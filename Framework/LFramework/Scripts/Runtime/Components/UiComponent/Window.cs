using System;
using System.Collections.Generic;
using GameFramework.Event;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using Zenject;
using Object = System.Object;

namespace LFramework.Runtime
{
    [PreLoadZenject]
    public abstract partial class Window : UIBehaviour
    {
        private const int DepthFactor = 10;
        private UIComponent _uiComponent;
        private Canvas _canvas;
        private bool _isClosing;
        private readonly List<Canvas> _canvasContainer = new List<Canvas>();
        private IAnimation _windowAnimation;
        private WindowAnimationState _windowAnimationState = WindowAnimationState.None;
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

        public override void OnInit(object userData)
        {
            base.OnInit(userData);
            _subModuleKeyMap.Clear();
            _windowAnimation = gameObject.GetComponent<IAnimation>();
            _canvas = gameObject.GetOrAddComponent<Canvas>();
            _canvas.overrideSorting = true;
            //_canvas.vertexColorAlwaysGammaSpace = true;
            OriginalDepth = _canvas.sortingOrder;
            gameObject.GetOrAddComponent<CanvasGroup>();
            CacheRectTransform.anchorMax = Vector2.one;
            CacheRectTransform.anchorMin = Vector2.zero;
            CacheRectTransform.anchoredPosition = Vector2.zero;
            CacheRectTransform.sizeDelta = Vector2.zero;

            gameObject.GetOrAddComponent<GraphicRaycaster>();
            
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnInit(this,userData);
                _subModuleKeyMap.Add(subWindow.GetType(),subWindow);
            }
        }

        public override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            Subscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            _windowAnimationState = WindowAnimationState.None;
            _isClosing = false;
            if (AutoPlayEnterAnimation)
            {
                PlayEnterAnimation(userData);
            }

            foreach (var subWindow in subModuleList)
            {
                subWindow.OnOpen(userData);
            }
        }

        public override void OnClose(bool isShutDown, object userData)
        {
            base.OnClose(isShutDown, userData);
            UnSubscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnClose(isShutDown,userData);
            }
        }

        public override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
        {
            var oldDepth = Depth;
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);
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
            foreach (var subWindow in subModuleList)
            {
                subWindow.OnDepthChanged();
            }
        }

        protected virtual void Subscribe(EventComponent eventComponent)
        {
            foreach (var subWindow in subModuleList)
            {
                subWindow.Subscribe(eventComponent);
            }
        }

        protected virtual void UnSubscribe(EventComponent eventComponent)
        {
            foreach (var subWindow in subModuleList)
            {
                subWindow.UnSubscribe(eventComponent);
            }
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            for (var index = 0; index < subModuleList.Count; index++)
            {
                var subWindow = subModuleList[index];
                subWindow.OUpdate(elapseSeconds, realElapseSeconds);
            }
        }

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