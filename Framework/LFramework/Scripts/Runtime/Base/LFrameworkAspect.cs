using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public sealed class LFrameworkAspect : Singleton<LFrameworkAspect>
    {
        private readonly DiContainer _diContainer;
        private EventComponent _cacheEventComponent;

        public LFrameworkAspect()
        {
        }

        public LFrameworkAspect(DiContainer diContainer)
        {
            _diContainer = diContainer;
        }

        public override void Dispose()
        {
            base.Dispose();
            DiContainer.UnbindAll();
        }

        /// <summary>
        /// DiContainer
        /// </summary>
        public DiContainer DiContainer => _diContainer;

        /// <summary>
        /// Get Anything
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            return DiContainer.Resolve<T>();
        }

        public bool HasBinding<T>()
        {
            return DiContainer.HasBinding<T>();
        }
        

        /// <summary>
        /// 下一帧抛出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        public void Fire(object sender, GameEventArgs gameEventArgs)
        {
            _cacheEventComponent ??= DiContainer.Resolve<EventComponent>();
            _cacheEventComponent.Fire(sender, gameEventArgs);
        }

        /// <summary>
        /// 直接抛出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        public void FireNow(object sender, GameEventArgs gameEventArgs)
        {
            _cacheEventComponent ??= DiContainer.Resolve<EventComponent>();
            _cacheEventComponent?.FireNow(sender, gameEventArgs);
        }
    }
}