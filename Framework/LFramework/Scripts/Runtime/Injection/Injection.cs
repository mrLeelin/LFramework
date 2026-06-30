using System;
using System.Collections.Generic;

namespace LFramework.Runtime
{
    /// <summary>
    /// Lightweight injection dispatcher.
    /// Runtime injection is intentionally zero-reflection: generated partial injectors implement
    /// <see cref="IInjectable"/>, while dynamic assemblies can register hand-written delegates.
    /// </summary>
    public static partial class Injection
    {
        private static readonly object SyncRoot = new object();
        private static readonly Dictionary<Type, Action<object, IServiceResolver>> GeneratedInjectors =
            new Dictionary<Type, Action<object, IServiceResolver>>();

        /// <summary>
        /// Registers a generated or hand-written injector for a target type.
        /// Hotfix assemblies should unregister their injectors before unload.
        /// </summary>
        /// <remarks>
        /// This is the dynamic extension point. Static project code should normally implement
        /// <see cref="IInjectable"/> through the Roslyn source generator instead.
        /// </remarks>
        public static void Register<T>(Action<T, IServiceResolver> injector)
        {
            if (injector == null)
            {
                throw new ArgumentNullException(nameof(injector));
            }

            lock (SyncRoot)
            {
                GeneratedInjectors[typeof(T)] = (target, resolver) => injector((T)target, resolver);
            }
        }

        /// <summary>
        /// Removes a dynamic injector registered for <typeparamref name="T"/>.
        /// Generated partial <see cref="IInjectable"/> implementations are not affected.
        /// </summary>
        public static bool UnregisterInjector<T>()
        {
            return UnregisterInjector(typeof(T));
        }

        /// <summary>
        /// Removes a dynamic injector registered for the specified target type.
        /// </summary>
        public static bool UnregisterInjector(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            lock (SyncRoot)
            {
                return GeneratedInjectors.Remove(targetType);
            }
        }

        /// <summary>
        /// Removes dynamic injectors that match a predicate, typically during hotfix assembly unload.
        /// </summary>
        public static int UnregisterInjectors(Predicate<Type> shouldUnregister)
        {
            if (shouldUnregister == null)
            {
                throw new ArgumentNullException(nameof(shouldUnregister));
            }

            lock (SyncRoot)
            {
                var targetTypes = new List<Type>();
                foreach (var pair in GeneratedInjectors)
                {
                    if (shouldUnregister(pair.Key))
                    {
                        targetTypes.Add(pair.Key);
                    }
                }

                for (var i = 0; i < targetTypes.Count; i++)
                {
                    GeneratedInjectors.Remove(targetTypes[i]);
                }

                return targetTypes.Count;
            }
        }

        /// <summary>
        /// Returns true when the target can be injected without reflection.
        /// </summary>
        public static bool CanInject(object target)
        {
            if (target == null)
            {
                return false;
            }

            if (target is IInjectable)
            {
                return true;
            }

            lock (SyncRoot)
            {
                return GeneratedInjectors.ContainsKey(target.GetType());
            }
        }

        /// <summary>
        /// Injects an object using the current root service resolver.
        /// </summary>
        public static void Inject(object target)
        {
            Inject(target, LServices.Resolver);
        }

        /// <summary>
        /// Injects an object using the provided resolver.
        /// The dispatcher first checks dynamic injectors, then generated <see cref="IInjectable"/> code.
        /// </summary>
        public static void Inject(object target, IServiceResolver resolver)
        {
            if (target == null)
            {
                return;
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            var targetType = target.GetType();
            Action<object, IServiceResolver> generatedInjector;
            lock (SyncRoot)
            {
                GeneratedInjectors.TryGetValue(targetType, out generatedInjector);
            }

            if (generatedInjector != null)
            {
                generatedInjector(target, resolver);
                return;
            }

            if (target is IInjectable injectable)
            {
                injectable.Inject(resolver);
            }
        }

        /// <summary>
        /// Injects a strongly-typed target using the current root service resolver.
        /// This generic overload avoids GetType() calls for zero-GC hot path injection.
        /// </summary>
        /// <typeparam name="T">The compile-time type of the target object.</typeparam>
        /// <param name="target">The object to inject dependencies into.</param>
        public static void Inject<T>(T target) where T : class
        {
            Inject(target, LServices.Resolver);
        }

        /// <summary>
        /// Injects a strongly-typed target using the provided resolver.
        /// This generic overload uses typeof(T) at compile-time, eliminating runtime GetType() calls
        /// and achieving zero-GC injection for performance-critical paths.
        /// </summary>
        /// <typeparam name="T">The compile-time type of the target object.</typeparam>
        /// <param name="target">The object to inject dependencies into.</param>
        /// <param name="resolver">The service resolver to pull dependencies from.</param>
        public static void Inject<T>(T target, IServiceResolver resolver) where T : class
        {
            if (target == null)
            {
                return;
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            // Use compile-time type instead of runtime GetType() - zero GC
            var targetType = typeof(T);
            Action<object, IServiceResolver> generatedInjector;
            lock (SyncRoot)
            {
                GeneratedInjectors.TryGetValue(targetType, out generatedInjector);
            }

            if (generatedInjector != null)
            {
                generatedInjector(target, resolver);
                return;
            }

            if (target is IInjectable injectable)
            {
                injectable.Inject(resolver);
            }
        }

        /// <summary>
        /// Compatibility no-op for older call sites that cleared reflection plans.
        /// Runtime injection no longer builds reflection plans.
        /// </summary>
        public static void ClearReflectionCache()
        {
        }

        /// <summary>
        /// Compatibility no-op for older call sites that cleared one reflection plan.
        /// Always returns false because no reflection plan is stored.
        /// </summary>
        public static bool ClearReflectionCache(Type targetType)
        {
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            return false;
        }

        /// <summary>
        /// Removes all dynamic injectors registered through <see cref="Register{T}"/>.
        /// Generated partial injectors remain part of their compiled assemblies.
        /// </summary>
        public static void ClearGeneratedInjectors()
        {
            lock (SyncRoot)
            {
                GeneratedInjectors.Clear();
            }
        }

        /// <summary>
        /// Clears all runtime-maintained injection state.
        /// </summary>
        public static void ClearAll()
        {
            lock (SyncRoot)
            {
                GeneratedInjectors.Clear();
            }
        }

        /// <summary>
        /// Deprecated cache clear name retained for source compatibility.
        /// </summary>
        [Obsolete("Use ClearReflectionCache, ClearGeneratedInjectors, or ClearAll.")]
        public static void ClearCache()
        {
            ClearAll();
        }
    }
}
