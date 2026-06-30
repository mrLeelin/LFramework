using System;
using System.Collections.Generic;

namespace LFramework.Runtime
{
    /// <summary>
    /// Hierarchical runtime service registry used by generated injection.
    /// Child scopes can override local services while falling back to their parent scope.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scope stores concrete service instances only. It does not scan attributes, build expression
    /// trees, or create objects by reflection, so resolving dependencies stays allocation-light.
    /// </para>
    /// <para>
    /// Normal <c>Register</c> calls do not transfer ownership. Use <c>RegisterOwned</c> when a temporary
    /// view, procedure, or hotfix lifetime should dispose the service with the scope.
    /// </para>
    /// </remarks>
    public sealed class LServiceScope : IServiceResolver, IDisposable
    {
        private readonly LServiceScope _parent;
        private readonly Dictionary<ServiceKey, ServiceEntry> _services = new Dictionary<ServiceKey, ServiceEntry>();
        private readonly List<LServiceScope> _children = new List<LServiceScope>();
        private bool _disposed;

        internal LServiceScope(LServiceScope parent)
        {
            _parent = parent;
            _parent?.AttachChild(this);
        }

        /// <summary>
        /// Gets the parent scope used as the fallback resolver.
        /// </summary>
        public LServiceScope Parent => _parent;

        /// <summary>
        /// Gets whether this scope has been disposed and can no longer resolve local services.
        /// </summary>
        public bool IsDisposed => _disposed;

        /// <summary>
        /// Creates a child scope that resolves locally first, then falls back to this scope.
        /// Disposing this scope also disposes all child scopes created from it.
        /// </summary>
        public LServiceScope CreateScope()
        {
            EnsureNotDisposed();
            return new LServiceScope(this);
        }

        /// <summary>
        /// Registers a non-owned service instance under its compile-time type.
        /// </summary>
        public void Register<T>(T service) where T : class
        {
            Register(typeof(T), null, service, false);
        }

        /// <summary>
        /// Registers a non-owned service instance under an explicit service type.
        /// </summary>
        public void Register(Type serviceType, object service)
        {
            Register(serviceType, null, service, false);
        }

        /// <summary>
        /// Registers a non-owned service instance under an explicit service type and identifier.
        /// </summary>
        public void Register(Type serviceType, object identifier, object service)
        {
            Register(serviceType, identifier, service, false);
        }

        /// <summary>
        /// Registers a service instance owned by this scope under its compile-time type.
        /// The instance is disposed when the scope is disposed or the registration is replaced.
        /// </summary>
        public void RegisterOwned<T>(T service) where T : class
        {
            Register(typeof(T), null, service, true);
        }

        /// <summary>
        /// Registers a service instance owned by this scope under an explicit service type.
        /// </summary>
        public void RegisterOwned(Type serviceType, object service)
        {
            Register(serviceType, null, service, true);
        }

        /// <summary>
        /// Registers a service instance owned by this scope under an explicit service type and identifier.
        /// </summary>
        public void RegisterOwned(Type serviceType, object identifier, object service)
        {
            Register(serviceType, identifier, service, true);
        }

        private void Register(Type serviceType, object identifier, object service, bool disposeWithScope)
        {
            EnsureNotDisposed();
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (!serviceType.IsInstanceOfType(service))
            {
                throw new ArgumentException(
                    "Service instance '" + service.GetType().FullName + "' is not assignable to '" +
                    serviceType.FullName + "'.",
                    nameof(service));
            }

            var key = new ServiceKey(serviceType, identifier);
            if (_services.TryGetValue(key, out var oldEntry))
            {
                oldEntry.DisposeIfOwned(service);
            }

            _services[key] = new ServiceEntry(service, disposeWithScope);
        }

        /// <summary>
        /// Removes a local registration under its compile-time type.
        /// Owned services are disposed when removed.
        /// </summary>
        public bool Unregister<T>() where T : class
        {
            return Unregister(typeof(T), null);
        }

        /// <summary>
        /// Removes a local registration under an explicit service type and optional identifier.
        /// Parent registrations are not affected.
        /// </summary>
        public bool Unregister(Type serviceType, object identifier = null)
        {
            EnsureNotDisposed();
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            var key = new ServiceKey(serviceType, identifier);
            if (!_services.TryGetValue(key, out var entry))
            {
                return false;
            }

            _services.Remove(key);
            entry.DisposeIfOwned();
            return true;
        }

        /// <summary>
        /// Resolves a service by compile-time type from this scope or its parents.
        /// </summary>
        public T Get<T>() where T : class
        {
            return (T)Get(typeof(T), null);
        }

        /// <summary>
        /// Resolves a keyed service by compile-time type from this scope or its parents.
        /// </summary>
        public T Get<T>(object identifier) where T : class
        {
            return (T)Get(typeof(T), identifier);
        }

        /// <summary>
        /// Resolves a service by explicit type and optional identifier from this scope or its parents.
        /// </summary>
        public object Get(Type serviceType, object identifier = null)
        {
            if (TryGet(serviceType, identifier, out var service))
            {
                return service;
            }

            throw new InvalidOperationException(
                "Service '" + (serviceType != null ? serviceType.FullName : "<null>") + "' with id '" +
                (identifier ?? "<null>") + "' is not registered.");
        }

        /// <summary>
        /// Tries to resolve a service by compile-time type from this scope or its parents.
        /// </summary>
        public bool TryGet<T>(out T service) where T : class
        {
            return TryGet(null, out service);
        }

        /// <summary>
        /// Tries to resolve a keyed service by compile-time type from this scope or its parents.
        /// </summary>
        public bool TryGet<T>(object identifier, out T service) where T : class
        {
            if (TryGet(typeof(T), identifier, out var value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Tries to resolve a service by explicit type and optional identifier from this scope or its parents.
        /// </summary>
        public bool TryGet(Type serviceType, object identifier, out object service)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (_disposed)
            {
                service = null;
                return false;
            }

            if (_services.TryGetValue(new ServiceKey(serviceType, identifier), out var entry))
            {
                service = entry.Instance;
                return true;
            }

            if (_parent != null)
            {
                return _parent.TryGet(serviceType, identifier, out service);
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Tries to resolve a service from this scope only, without checking parent scopes.
        /// </summary>
        public bool TryGetLocal<T>(out T service) where T : class
        {
            return TryGetLocal(null, out service);
        }

        /// <summary>
        /// Tries to resolve a keyed service from this scope only, without checking parent scopes.
        /// </summary>
        public bool TryGetLocal<T>(object identifier, out T service) where T : class
        {
            if (TryGetLocal(typeof(T), identifier, out var value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>
        /// Tries to resolve a service from this scope only, without checking parent scopes.
        /// </summary>
        public bool TryGetLocal(Type serviceType, object identifier, out object service)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (_disposed || !_services.TryGetValue(new ServiceKey(serviceType, identifier), out var entry))
            {
                service = null;
                return false;
            }

            service = entry.Instance;
            return true;
        }

        /// <summary>
        /// Returns true when this scope or a parent scope contains the requested service.
        /// </summary>
        public bool Contains(Type serviceType, object identifier = null)
        {
            return TryGet(serviceType, identifier, out _);
        }

        /// <summary>
        /// Returns true when this scope contains the requested service locally.
        /// </summary>
        public bool ContainsLocal(Type serviceType, object identifier = null)
        {
            return TryGetLocal(serviceType, identifier, out _);
        }

        /// <summary>
        /// Injects an object using this scope as the resolver.
        /// </summary>
        public void Inject(object target)
        {
            Injection.Inject(target, this);
        }

        /// <summary>
        /// Removes all local registrations. Owned services are disposed; parent scopes are untouched.
        /// </summary>
        public void Clear()
        {
            EnsureNotDisposed();
            ClearLocalServices();
        }

        /// <summary>
        /// Disposes this scope, all child scopes, and all owned local services.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            var children = _children.ToArray();
            for (var i = children.Length - 1; i >= 0; i--)
            {
                children[i].Dispose();
            }

            ClearLocalServices();
            _parent?.DetachChild(this);
            _disposed = true;
        }

        private void AttachChild(LServiceScope child)
        {
            EnsureNotDisposed();
            _children.Add(child);
        }

        private void DetachChild(LServiceScope child)
        {
            _children.Remove(child);
        }

        private void ClearLocalServices()
        {
            foreach (var pair in _services)
            {
                pair.Value.DisposeIfOwned();
            }

            _services.Clear();
        }

        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LServiceScope));
            }
        }

        private readonly struct ServiceEntry
        {
            private readonly bool _disposeWithScope;

            public ServiceEntry(object instance, bool disposeWithScope)
            {
                Instance = instance;
                _disposeWithScope = disposeWithScope;
            }

            public object Instance { get; }

            public void DisposeIfOwned()
            {
                if (_disposeWithScope && Instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            public void DisposeIfOwned(object replacement)
            {
                if (!ReferenceEquals(Instance, replacement))
                {
                    DisposeIfOwned();
                }
            }
        }

        private struct ServiceKey : IEquatable<ServiceKey>
        {
            private readonly Type _type;
            private readonly object _identifier;

            public ServiceKey(Type type, object identifier)
            {
                _type = type;
                _identifier = identifier;
            }

            public bool Equals(ServiceKey other)
            {
                return _type == other._type && Equals(_identifier, other._identifier);
            }

            public override bool Equals(object obj)
            {
                return obj is ServiceKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_type != null ? _type.GetHashCode() : 0) * 397) ^
                           (_identifier != null ? _identifier.GetHashCode() : 0);
                }
            }
        }
    }
}
