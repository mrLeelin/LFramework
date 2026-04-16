using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;
using VContainer;

namespace LFramework.Runtime
{
    public sealed class LFrameworkAspect : Singleton<LFrameworkAspect>
    {
        private readonly FrameworkResolverContext _resolverContext;
        private readonly FrameworkInjector _frameworkInjector;
        private readonly HotfixScopeRegistry _hotfixScopeRegistry;
        private readonly RuntimeProcedureScopeRegistry _procedureScopeRegistry;
        private EventComponent _cacheEventComponent;

        public LFrameworkAspect()
        {
            throw new InvalidOperationException(
                "LFrameworkAspect must be created with a FrameworkResolverContext. Use LFrameworkAspect(FrameworkResolverContext) constructor.");
        }

        public LFrameworkAspect(FrameworkResolverContext resolverContext)
        {
            _resolverContext = resolverContext ?? throw new ArgumentNullException(nameof(resolverContext));
            _frameworkInjector = new FrameworkInjector(_resolverContext);
            _hotfixScopeRegistry = new HotfixScopeRegistry(_resolverContext);
            _procedureScopeRegistry = new RuntimeProcedureScopeRegistry(_resolverContext);
        }

        public override void Dispose()
        {
            base.Dispose();
            _cacheEventComponent = null;
        }

        public FrameworkResolverContext ResolverContext => _resolverContext;

        public IObjectResolver RootResolver => _resolverContext?.RootResolver;

        public FrameworkInjector FrameworkInjector => _frameworkInjector;

        public HotfixScopeRegistry HotfixScopeRegistry => _hotfixScopeRegistry;

        public RuntimeProcedureScopeRegistry ProcedureScopeRegistry => _procedureScopeRegistry;

        /// <summary>
        /// Get Anything
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>()
        {
            return _resolverContext.ActiveResolver.Resolve<T>();
        }

        public bool HasBinding<T>()
        {
            return _resolverContext.ActiveResolver?.TryResolve<T>(out _) ?? false;
        }

        /// <summary>
        /// 下一帧抛出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        public void Fire(object sender, GameEventArgs gameEventArgs)
        {
            _cacheEventComponent ??= Get<EventComponent>();
            _cacheEventComponent.Fire(sender, gameEventArgs);
        }

        /// <summary>
        /// 直接抛出事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="gameEventArgs"></param>
        public void FireNow(object sender, GameEventArgs gameEventArgs)
        {
            _cacheEventComponent ??= Get<EventComponent>();
            _cacheEventComponent?.FireNow(sender, gameEventArgs);
        }
    }
}
