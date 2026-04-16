using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Profiling;

namespace LFramework.Runtime
{
    /// <summary>
    /// Provides validated GameObject injection using FrameworkInjector.
    /// Replaces the former DiContainerExtensions.InjectGameObjectNotCheck method.
    /// Only MonoBehaviours decorated with [MonoBehaviourValidation] are injected.
    /// </summary>
    public static class FrameworkGameObjectInjector
    {
        private static readonly Dictionary<Type, bool> ValidationCache = new Dictionary<Type, bool>();

        /// <summary>
        /// Injects all MonoBehaviour components on the GameObject and its children
        /// that are decorated with the [MonoBehaviourValidation] attribute.
        /// </summary>
        /// <param name="injector">The framework injector to use for injection.</param>
        /// <param name="gameObject">The target GameObject to inject.</param>
        public static void InjectGameObjectValidated(FrameworkInjector injector, GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            var monoBehaviours = gameObject.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true);
            if (monoBehaviours.Length == 0)
            {
                return;
            }

            Profiler.BeginSample("InjectGameObjectValidated");
            try
            {
                for (int i = 0; i < monoBehaviours.Length; i++)
                {
                    var mb = monoBehaviours[i];
                    if (mb != null && HasValidationAttribute(mb))
                    {
                        injector.Inject(mb);
                    }
                }
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static bool HasValidationAttribute(UnityEngine.MonoBehaviour mb)
        {
            var type = mb.GetType();
            if (ValidationCache.TryGetValue(type, out var cached))
            {
                return cached;
            }

            var result = type.GetCustomAttribute<MonoBehaviourValidationAttribute>(true) != null;
            ValidationCache[type] = result;
            return result;
        }
    }
}
