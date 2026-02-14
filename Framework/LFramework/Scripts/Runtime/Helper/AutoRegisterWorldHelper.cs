using System;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public static class AutoRegisterWorldHelper
    {
        private static Dictionary<Type, List<Type>> _cacheWorldHelperTypes =
            new Dictionary<Type, List<Type>>();


        private static readonly List<IWorldHelper> TempWorldHelpers = new();

        internal static List<IWorldHelper> GetRegisterWorldHelper(Type worldType)
        {
            TempWorldHelpers.Clear();

            var flag = _cacheWorldHelperTypes.TryGetValue(worldType, out var types);
            if (!flag)
            {
                types = GetWorldHelperTypes(worldType);
                _cacheWorldHelperTypes.SafeAdd(worldType, types);
            }

            if (types == null || types.Count == 0)
            {
                return TempWorldHelpers;
            }


            foreach (var t in types)
            {
                var instance = (IWorldHelper)ReferencePool.Acquire(t);
                TempWorldHelpers.Add(instance);
            }

            return TempWorldHelpers;
        }

        private static List<Type> GetWorldHelperTypes(Type worldType)
        {
            var hotfixComponent = LFrameworkAspect.Instance.Get<HotfixComponent>();
            if (hotfixComponent == null)
            {
                Log.Fatal("HotfixComponent is null");
                return null;
            }

            var types = hotfixComponent.GetTypesFromAttribute<AutoWorldHelperAttribute>();
            if (types == null)
            {
                return null;
            }

            var result = new List<Type>();
            foreach (var t in types)
            {
                var attribute = t.GetCustomAttribute<AutoWorldHelperAttribute>();
                if (!attribute.BindWorldType.Contains(worldType))
                {
                    continue;
                }

                if (!t.IsSubclassOf(typeof(WorldHelperBase)))
                {
                    continue;
                }

                result.Add(t);
            }

            return result;
        }
    }
}