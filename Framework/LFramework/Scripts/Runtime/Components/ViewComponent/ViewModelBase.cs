using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using GameFramework;
using GameFramework.Event;
using UniRx;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class ViewModelBase : IViewModel
    {
        private CompositeDisposable _compositeDisposable;

        protected ViewModelBase()
        {
        }

        public void Clear()
        {
            UnSubscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            Identifier = null;
            References = 0;
            _compositeDisposable.Dispose();
            _compositeDisposable = null;
            OnDispose();
        }

        public string Identifier { get; set; }
        public int References { get; set; }


        protected CompositeDisposable CompositeDisposable => _compositeDisposable;

        void IViewModel.Initialize()
        {
            _compositeDisposable = new CompositeDisposable();
            Subscribe(LFrameworkAspect.Instance.Get<EventComponent>());
            OnInitialize(_compositeDisposable);
            Bind(_compositeDisposable);
        }

        protected virtual void Bind(CompositeDisposable compositeDisposable)
        {
        }


        protected virtual void OnInitialize(CompositeDisposable compositeDisposable)
        {
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual void Subscribe(EventComponent eventComponent)
        {
        }

        protected virtual void UnSubscribe(EventComponent eventComponent)
        {
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
    }
}