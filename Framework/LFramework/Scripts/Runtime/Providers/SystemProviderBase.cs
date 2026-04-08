using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using LFramework.Runtime;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public abstract class SystemProviderBase : ISystemProvider
    {
        [Inject] protected EventComponent EventComponent;
        private bool _isSubscribed;


        public virtual void AwakeComponent()
        {
        }

        public void SubscribeEvent()
        {
            Subscribe(EventComponent);
            _isSubscribed = true;
        }

        public virtual void SetUp()
        {
        }

        public void UnSubscribeEvent()
        {
            if (!_isSubscribed)
            {
                return;
            }

            UnSubscribe(EventComponent);
            _isSubscribed = false;
        }

        public virtual void OnStop()
        {
        }

        /*
        public void Dispose()
        {
            OnStop();
            GC.SuppressFinalize(this);
        }
        */
        public void Clear()
        {
            UnSubscribeEvent();
            OnStop();
        }

        public virtual void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
        }

        #region Event

        protected virtual void Subscribe(EventComponent eventComponent)
        {
        }

        protected virtual void UnSubscribe(EventComponent eventComponent)
        {
        }

        protected virtual void Fire<T>(T gameEventArgs) where T : GameEventArgs
        {
            if (EventComponent == null)
            {
                Log.Fatal("Event Component is null.");
                return;
            }

            EventComponent.Fire(this, gameEventArgs);
        }

        protected virtual void Fire<T>(object sender, T gameEventArgs) where T : GameEventArgs
        {
            if (EventComponent == null)
            {
                Log.Fatal("Event Component is null.");
                return;
            }

            EventComponent.Fire(sender, gameEventArgs);
        }

        protected virtual void FireNow<T>(T gameEventArgs) where T : GameEventArgs
        {
            if (EventComponent == null)
            {
                Log.Fatal("Event Component is null.");
                return;
            }

            EventComponent.FireNow(null, gameEventArgs);
        }

        protected virtual void FireNow<T>(object sender, T gameEventArgs) where T : GameEventArgs
        {
            if (EventComponent == null)
            {
                Log.Fatal("Event Component is null.");
                return;
            }

            EventComponent.FireNow(sender, gameEventArgs);
        }

        #endregion
    }
}