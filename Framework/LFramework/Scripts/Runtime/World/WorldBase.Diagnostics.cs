#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace LFramework.Runtime
{
    public abstract partial class WorldBase
    {
        public WorldDiagnosticsSnapshot GetDiagnosticsSnapshot()
        {
            var helpers = new List<WorldHelperDiagnostics>();
            var uniqueHelpers = new HashSet<IWorldHelper>(
                _worldHelpers.Values,
                WorldHelperReferenceComparer.Instance);

            foreach (IWorldHelper helper in uniqueHelpers.OrderBy(item => item.GetType().FullName, StringComparer.Ordinal))
            {
                Type helperType = helper.GetType();
                _linkWorldHelperAndInterface.TryGetValue(helperType, out Type serviceInterface);

                Type[] registrationTypes = _worldHelpers
                    .Where(pair => ReferenceEquals(pair.Value, helper))
                    .Select(pair => pair.Key)
                    .Distinct()
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .ToArray();

                Type[] implementedInterfaces = helperType
                    .GetInterfaces()
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .ToArray();

                helpers.Add(new WorldHelperDiagnostics(
                    helper,
                    serviceInterface,
                    registrationTypes,
                    implementedInterfaces,
                    helper is IWorldUpdate worldUpdate &&
                    _worldUpdates.Any(item => ReferenceEquals(item, worldUpdate)),
                    helper is IWorldLateUpdate worldLateUpdate &&
                    _worldLateUpdates.Any(item => ReferenceEquals(item, worldLateUpdate)),
                    _autoRegisteredWorldHelperInstances.Contains(helper)));
            }

            return new WorldDiagnosticsSnapshot(this, _linkProcedure?.GetType(), helpers);
        }
    }
}
#endif
