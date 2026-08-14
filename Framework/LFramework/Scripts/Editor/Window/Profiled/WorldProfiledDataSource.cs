using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;

namespace LFramework.Editor.Window
{
    public sealed class WorldProfiledRefreshGate
    {
        private readonly double _interval;
        private double _lastAttemptTime = double.NegativeInfinity;

        public WorldProfiledRefreshGate(double interval)
        {
            _interval = interval;
        }

        public bool TryBeginRefresh(double currentTime)
        {
            if (currentTime - _lastAttemptTime < _interval)
            {
                return false;
            }

            _lastAttemptTime = currentTime;
            return true;
        }

        public void BeginImmediateRefresh(double currentTime)
        {
            _lastAttemptTime = currentTime;
        }
    }

    public sealed class WorldServiceRegistration
    {
        public WorldServiceRegistration(
            Type serviceType,
            object identifier,
            object instance,
            string scopeName,
            bool isOwned)
        {
            ServiceType = serviceType;
            Identifier = identifier;
            Instance = instance;
            ScopeName = scopeName;
            IsOwned = isOwned;
        }

        public Type ServiceType { get; }
        public object Identifier { get; }
        public object Instance { get; }
        public string ScopeName { get; }
        public bool IsOwned { get; }
    }

    public sealed class WorldProfiledHelper
    {
        internal WorldProfiledHelper(
            WorldHelperDiagnostics diagnostics,
            IEnumerable<WorldServiceRegistration> serviceRegistrations)
        {
            Diagnostics = diagnostics;
            ServiceRegistrations = new List<WorldServiceRegistration>(serviceRegistrations).AsReadOnly();
        }

        public WorldHelperDiagnostics Diagnostics { get; }
        public IReadOnlyList<WorldServiceRegistration> ServiceRegistrations { get; }
        public bool IsRegisteredAsService => ServiceRegistrations.Count > 0;
    }

    public sealed class WorldProfiledWorld
    {
        internal WorldProfiledWorld(
            WorldDiagnosticsSnapshot diagnostics,
            IEnumerable<WorldServiceRegistration> serviceRegistrations,
            IEnumerable<WorldProfiledHelper> helpers)
        {
            Diagnostics = diagnostics;
            ServiceRegistrations = new List<WorldServiceRegistration>(serviceRegistrations).AsReadOnly();
            Helpers = new List<WorldProfiledHelper>(helpers).AsReadOnly();
        }

        public WorldDiagnosticsSnapshot Diagnostics { get; }
        public IReadOnlyList<WorldServiceRegistration> ServiceRegistrations { get; }
        public IReadOnlyList<WorldProfiledHelper> Helpers { get; }
    }

    public sealed class WorldProfiledSnapshot
    {
        internal WorldProfiledSnapshot(IEnumerable<WorldProfiledWorld> worlds, int scopeCount)
        {
            Worlds = new List<WorldProfiledWorld>(worlds).AsReadOnly();
            ScopeCount = scopeCount;
        }

        public IReadOnlyList<WorldProfiledWorld> Worlds { get; }
        public int ScopeCount { get; }
    }

    public static class WorldProfiledDataSource
    {
        public static WorldProfiledSnapshot Capture()
        {
            LServicesDiagnosticsSnapshot services = LServices.GetDiagnosticsSnapshot();
            IEnumerable<WorldServiceRegistration> registrations = services.Registrations.Select(
                registration => new WorldServiceRegistration(
                    registration.ServiceType,
                    registration.Identifier,
                    registration.Instance,
                    registration.ScopeName,
                    registration.IsOwned));

            return CreateSnapshot(registrations, services.ScopeCount);
        }

        public static WorldProfiledSnapshot CreateSnapshot(IEnumerable<WorldServiceRegistration> registrations)
        {
            return CreateSnapshot(registrations, 0);
        }

        private static WorldProfiledSnapshot CreateSnapshot(
            IEnumerable<WorldServiceRegistration> registrations,
            int scopeCount)
        {
            var worldRegistrations = new Dictionary<WorldBase, List<WorldServiceRegistration>>(
                ReferenceComparer<WorldBase>.Instance);
            var helperRegistrations = new Dictionary<IWorldHelper, List<WorldServiceRegistration>>(
                ReferenceComparer<IWorldHelper>.Instance);

            foreach (WorldServiceRegistration registration in registrations ?? Array.Empty<WorldServiceRegistration>())
            {
                if (registration?.Instance is WorldBase world)
                {
                    AddRegistration(worldRegistrations, world, registration);
                }

                if (registration?.Instance is IWorldHelper helper)
                {
                    AddRegistration(helperRegistrations, helper, registration);
                }
            }

            var worlds = new List<WorldProfiledWorld>();
            foreach (KeyValuePair<WorldBase, List<WorldServiceRegistration>> pair in worldRegistrations
                         .OrderBy(item => item.Key.GetType().FullName, StringComparer.Ordinal))
            {
                WorldDiagnosticsSnapshot diagnostics = pair.Key.GetDiagnosticsSnapshot();
                WorldProfiledHelper[] helpers = diagnostics.Helpers
                    .Select(helper => new WorldProfiledHelper(
                        helper,
                        GetRegistrations(helperRegistrations, helper.Instance)))
                    .ToArray();

                worlds.Add(new WorldProfiledWorld(
                    diagnostics,
                    SortRegistrations(pair.Value),
                    helpers));
            }

            return new WorldProfiledSnapshot(worlds, scopeCount);
        }

        private static IEnumerable<WorldServiceRegistration> GetRegistrations(
            IReadOnlyDictionary<IWorldHelper, List<WorldServiceRegistration>> registrations,
            IWorldHelper helper)
        {
            return registrations.TryGetValue(helper, out List<WorldServiceRegistration> result)
                ? SortRegistrations(result)
                : Array.Empty<WorldServiceRegistration>();
        }

        private static IEnumerable<WorldServiceRegistration> SortRegistrations(
            IEnumerable<WorldServiceRegistration> registrations)
        {
            return registrations
                .OrderBy(item => item.ScopeName, StringComparer.Ordinal)
                .ThenBy(item => item.ServiceType.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.Identifier?.ToString(), StringComparer.Ordinal);
        }

        private static void AddRegistration<T>(
            IDictionary<T, List<WorldServiceRegistration>> registrations,
            T instance,
            WorldServiceRegistration registration)
        {
            if (!registrations.TryGetValue(instance, out List<WorldServiceRegistration> aliases))
            {
                aliases = new List<WorldServiceRegistration>();
                registrations.Add(instance, aliases);
            }

            aliases.Add(registration);
        }

        private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
        {
            internal static readonly ReferenceComparer<T> Instance = new();

            public bool Equals(T x, T y)
            {
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(T obj)
            {
                return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
