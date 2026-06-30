using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Unity-facing adapter for lightweight injection.
    /// It keeps the old helper names but routes actual member assignment through Injection.
    /// </summary>
    public static class InjectUnity
    {
        /// <summary>
        /// Injects every injectable <see cref="MonoBehaviour"/> under a GameObject hierarchy.
        /// </summary>
        /// <remarks>
        /// The hierarchy walk uses Unity's component lookup only. It does not inspect fields or
        /// properties; <see cref="Injection.CanInject"/> filters to generated or manually registered injectors.
        /// </remarks>
        public static void InjectGameObjectNotCheck(GameObject gameObject)
        {
            var monoBehaviours = ListPool<UnityEngine.MonoBehaviour>.Spawn();
            try
            {
                GetInjectableMonoBehavioursNotCheck(gameObject, monoBehaviours);

                Profiler.BeginSample("Inject GameObject Not Check");
                try
                {
                    for (var i = 0; i < monoBehaviours.Count; i++)
                    {
                        Injection.Inject(monoBehaviours[i]);
                    }
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
            finally
            {
                ListPool<UnityEngine.MonoBehaviour>.Despawn(monoBehaviours);
            }
        }

        /// <summary>
        /// Injects a single Unity component through the generated injector dispatcher.
        /// </summary>
        public static void InjectComponentNotCheck(UnityEngine.Component component)
        {
            Profiler.BeginSample("Inject Component Not Check");
            try
            {
                Injection.Inject(component);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        private static void GetInjectableMonoBehavioursNotCheck(
            GameObject gameObject,
            List<UnityEngine.MonoBehaviour> injectableComponents)
        {
            GetInjectableMonoBehavioursNotCheckInternal(gameObject, injectableComponents);
        }

        private static void GetInjectableMonoBehavioursNotCheckInternal(
            GameObject gameObject,
            List<UnityEngine.MonoBehaviour> injectableComponents)
        {
            if (gameObject == null)
            {
                return;
            }

            var tempList = ListPool<UnityEngine.MonoBehaviour>.Spawn();
            try
            {
                gameObject.GetComponentsInChildren(true, tempList);
                if (tempList.Count == 0)
                {
                    Log.Error($"No valid MonoBehaviour found in '{gameObject.name}'.");
                    return;
                }

                for (var i = 0; i < tempList.Count; i++)
                {
                    var monoBehaviour = tempList[i];
                    if (monoBehaviour != null && Injection.CanInject(monoBehaviour))
                    {
                        injectableComponents.Add(monoBehaviour);
                    }
                }
            }
            finally
            {
                ListPool<UnityEngine.MonoBehaviour>.Despawn(tempList);
            }
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new Stack<List<T>>();

            public static List<T> Spawn()
            {
                lock (Pool)
                {
                    return Pool.Count > 0 ? Pool.Pop() : new List<T>();
                }
            }

            public static void Despawn(List<T> list)
            {
                if (list == null)
                {
                    return;
                }

                list.Clear();
                lock (Pool)
                {
                    Pool.Push(list);
                }
            }
        }
    }
}
