using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityGameFramework.Runtime;
using Zenject;
using Zenject.Internal;

namespace LFramework.Runtime
{
    public static class DiContainerExtensions
    {
        private static readonly Dictionary<Type, bool> ExitMonoBehaviourValidationAttribute =
            new Dictionary<Type, bool>();

        /// <summary>
        /// 注入GameObject及其子物体的所有MonoBehaviour组件，但不检查是否已经注入过
        /// </summary>
        /// <param name="diContainer"></param>
        /// <param name="gameObject"></param>
        public static void InjectGameObjectNotCheck(this DiContainer diContainer, GameObject gameObject)
        {
            var monoBehaviours = ZenPools.SpawnList<UnityEngine.MonoBehaviour>();
            try
            {
                GetInjectableMonoBehavioursNotCheck(gameObject, monoBehaviours);

                Profiler.BeginSample("Inject GameObject Not Check");
                try
                {
                    for (int i = 0; i < monoBehaviours.Count; i++)
                    {
                        diContainer.Inject(monoBehaviours[i]);
                    }
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
            finally
            {
                ZenPools.DespawnList(monoBehaviours);
            }
        }

        /// <summary>
        /// 注入GameObject及其子物体的所有MonoBehaviour组件，但不检查是否已经注入过
        /// </summary>
        /// <param name="diContainer"></param>
        /// <param name="component"></param>
        public static void InjectComponentNotCheck(this DiContainer diContainer, UnityEngine.Component component)
        {
            try
            {
                Profiler.BeginSample("Inject Component Not Check");
#if ZEN_INTERNAL_PROFILING
                    using (ProfileTimers.CreateTimedBlock("Inject Component Not Check"))
#endif
                {
                    diContainer.Inject(component);
                }
                Profiler.EndSample();
            }
            finally
            {
                //ignore
            }
        }


        private static void GetInjectableMonoBehavioursNotCheck(GameObject gameObject,
            List<UnityEngine.MonoBehaviour> injectableComponents)
        {
#if ZEN_INTERNAL_PROFILING
            using (ProfileTimers.CreateTimedBlock("Searching Hierarchy"))
#endif
            {
                GetInjectableMonoBehavioursNotCheckInternal(gameObject, injectableComponents);
            }
        }


        private static void GetInjectableMonoBehavioursNotCheckInternal(
            GameObject gameObject, List<UnityEngine.MonoBehaviour> injectableComponents)
        {
            if (gameObject == null)
            {
                return;
            }


            var tempList = ZenPools.SpawnList<UnityEngine.MonoBehaviour>();
            try
            {
                gameObject.GetComponentsInChildren(true, tempList);
                if (tempList.Count == 0)
                {
                    Log.Error($"No valid MonoBehaviour found in '{gameObject.name}'.");
                    return;
                }

                for (int i = 0; i < tempList.Count; i++)
                {
                    var monoBehaviour = tempList[i];
                    if (monoBehaviour != null && GetMonoBehaviourValidationExit(monoBehaviour))
                    {
                        injectableComponents.Add(monoBehaviour);
                    }
                }
            }
            finally
            {
                ZenPools.DespawnList(tempList);
            }
        }

        private static bool GetMonoBehaviourValidationExit(UnityEngine.MonoBehaviour behaviour)
        {
            var type = behaviour.GetType();
            if (ExitMonoBehaviourValidationAttribute.TryGetValue(type, out var res))
            {
                return res;
            }

            res = type.GetCustomAttribute<MonoBehaviourValidationAttribute>(true) != null;
            ExitMonoBehaviourValidationAttribute.Add(type, res);
            return res;
        }
    }
}