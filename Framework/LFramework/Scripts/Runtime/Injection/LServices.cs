using System;

namespace LFramework.Runtime
{
    /// <summary>
    /// Process-wide service facade used by framework bootstrap, runtime systems, and hotfix code.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is intentionally a small service registry instead of a full DI container. It only stores
    /// instances that the framework has already created, then generated injectors pull those instances
    /// through <see cref="IServiceResolver"/> without runtime reflection.
    /// </para>
    /// <para>
    /// The static facade owns a replaceable root scope. Cached <see cref="Resolver"/> references remain
    /// valid across <see cref="Reset"/> because they forward every lookup to the current root.
    /// </para>
    /// </remarks>
    public static class LServices
    {
        private static readonly IServiceResolver RootResolver = new CurrentRootResolver();
        private static LServiceScope _root = new LServiceScope(null);

        /// <summary>
        /// Stable resolver facade for generated injectors and systems that should not mutate services.
        /// </summary>
        public static IServiceResolver Resolver => RootResolver;

        /// <summary>
        /// Creates a child scope under the current root. Use this for temporary view/procedure lifetimes.
        /// </summary>
        public static LServiceScope CreateScope()
        {
            return _root.CreateScope();
        }

        /// <summary>
        /// Registers a non-owned root service under its compile-time type.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            _root.Register(service);
        }

        /// <summary>
        /// Registers a non-owned root service under an explicit service type.
        /// </summary>
        public static void Register(Type serviceType, object service)
        {
            _root.Register(serviceType, service);
        }

        /// <summary>
        /// Registers a non-owned root service under an explicit service type and identifier.
        /// </summary>
        public static void Register(Type serviceType, object identifier, object service)
        {
            _root.Register(serviceType, identifier, service);
        }

        /// <summary>
        /// Registers a root service owned by the root scope.
        /// The service is disposed on <see cref="Reset"/> or replacement.
        /// </summary>
        public static void RegisterOwned<T>(T service) where T : class
        {
            _root.RegisterOwned(service);
        }

        /// <summary>
        /// Registers a root service owned by the root scope under an explicit service type.
        /// </summary>
        public static void RegisterOwned(Type serviceType, object service)
        {
            _root.RegisterOwned(serviceType, service);
        }

        /// <summary>
        /// Registers a root service owned by the root scope under an explicit service type and identifier.
        /// </summary>
        public static void RegisterOwned(Type serviceType, object identifier, object service)
        {
            _root.RegisterOwned(serviceType, identifier, service);
        }

        /// <summary>
        /// Removes a non-keyed root registration. Owned services are disposed when removed.
        /// </summary>
        public static bool Unregister<T>() where T : class
        {
            return _root.Unregister<T>();
        }

        /// <summary>
        /// Removes a root registration by explicit type and optional identifier.
        /// </summary>
        public static bool Unregister(Type serviceType, object identifier = null)
        {
            return _root.Unregister(serviceType, identifier);
        }

        /// <summary>
        /// Resolves a root service by compile-time type.
        /// </summary>
        public static T Get<T>() where T : class
        {
            return _root.Get<T>();
        }

        /// <summary>
        /// Resolves a keyed root service by compile-time type.
        /// </summary>
        public static T Get<T>(object identifier) where T : class
        {
            return _root.Get<T>(identifier);
        }

        /// <summary>
        /// Resolves a root service by explicit type and optional identifier.
        /// </summary>
        public static object Get(Type serviceType, object identifier = null)
        {
            return _root.Get(serviceType, identifier);
        }

        /// <summary>
        /// Tries to resolve a root service by compile-time type.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            return _root.TryGet(out service);
        }

        /// <summary>
        /// Tries to resolve a keyed root service by compile-time type.
        /// </summary>
        public static bool TryGet<T>(object identifier, out T service) where T : class
        {
            return _root.TryGet(identifier, out service);
        }

        /// <summary>
        /// Tries to resolve a root service by explicit type and optional identifier.
        /// </summary>
        public static bool TryGet(Type serviceType, object identifier, out object service)
        {
            return _root.TryGet(serviceType, identifier, out service);
        }

        /// <summary>
        /// Injects an object using the current root resolver.
        /// </summary>
        public static void Inject(object target)
        {
            _root.Inject(target);
        }

        /// <summary>
        /// Replaces the root scope and disposes the old root, including its owned services and children.
        /// </summary>
        public static void Reset()
        {
            var oldRoot = _root;
            _root = new LServiceScope(null);
            oldRoot.Dispose();
        }

        private sealed class CurrentRootResolver : IServiceResolver
        {
            public T Get<T>() where T : class
            {
                return LServices.Get<T>();
            }

            public T Get<T>(object identifier) where T : class
            {
                return LServices.Get<T>(identifier);
            }

            public object Get(Type serviceType, object identifier = null)
            {
                return LServices.Get(serviceType, identifier);
            }

            public bool TryGet<T>(out T service) where T : class
            {
                return LServices.TryGet(out service);
            }

            public bool TryGet<T>(object identifier, out T service) where T : class
            {
                return LServices.TryGet(identifier, out service);
            }

            public bool TryGet(Type serviceType, object identifier, out object service)
            {
                return LServices.TryGet(serviceType, identifier, out service);
            }
        }
    }
}
