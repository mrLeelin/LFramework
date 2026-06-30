using System;

namespace LFramework.Runtime
{
    /// <summary>
    /// Minimal service lookup contract used by Inject and generated injectors.
    /// It intentionally exposes only resolve operations so callers cannot mutate scopes.
    /// </summary>
    public interface IServiceResolver
    {
        /// <summary>
        /// Resolves a non-keyed service by compile-time type.
        /// </summary>
        T Get<T>() where T : class;

        /// <summary>
        /// Resolves a keyed service by compile-time type.
        /// </summary>
        T Get<T>(object identifier) where T : class;

        /// <summary>
        /// Resolves a service by explicit type and optional identifier.
        /// </summary>
        object Get(Type serviceType, object identifier = null);

        /// <summary>
        /// Tries to resolve a non-keyed service by compile-time type.
        /// </summary>
        bool TryGet<T>(out T service) where T : class;

        /// <summary>
        /// Tries to resolve a keyed service by compile-time type.
        /// </summary>
        bool TryGet<T>(object identifier, out T service) where T : class;

        /// <summary>
        /// Tries to resolve a service by explicit type and optional identifier.
        /// </summary>
        bool TryGet(Type serviceType, object identifier, out object service);
    }
}
