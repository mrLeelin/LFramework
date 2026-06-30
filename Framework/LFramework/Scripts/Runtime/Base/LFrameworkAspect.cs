using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public sealed class LFrameworkAspect : Singleton<LFrameworkAspect>
    {
        private EventComponent _cacheEventComponent;

        public LFrameworkAspect()
        {
            LServices.Reset();
            Injection.ClearReflectionCache();
        }

        public override void Dispose()
        {
            base.Dispose();
            LServices.Reset();
            Injection.ClearReflectionCache();
            _cacheEventComponent = null;
        }

        /// <summary>
        /// Get Anything
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            if (LServices.TryGet(typeof(T), null, out var service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                "Service '" + typeof(T).FullName + "' is not registered in LServices.");
        }

        public bool HasBinding<T>()
        {
            return LServices.TryGet(typeof(T), null, out _);
        }
        

        /// <summary>
        /// 下一帧抛出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        public void Fire(object sender, GameEventArgs gameEventArgs)
        {
            _cacheEventComponent ??= LServices.TryGet<EventComponent>(out var eventComponent)
                ? eventComponent
                : Get<EventComponent>();
            _cacheEventComponent.Fire(sender, gameEventArgs);
        }

        /// <summary>
        /// 直接抛出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        public void FireNow(object sender, GameEventArgs gameEventArgs)
        {
            _cacheEventComponent ??= LServices.TryGet<EventComponent>(out var eventComponent)
                ? eventComponent
                : Get<EventComponent>();
            _cacheEventComponent?.FireNow(sender, gameEventArgs);
        }
    }
}
