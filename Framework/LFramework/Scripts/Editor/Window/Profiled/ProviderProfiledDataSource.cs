using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;

namespace LFramework.Editor.Window
{
    public sealed class ProviderServiceRegistration
    {
        public ProviderServiceRegistration(
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

    public sealed class ProviderProfiledProvider
    {
        internal ProviderProfiledProvider(
            ProviderDiagnosticsSnapshot diagnostics,
            IEnumerable<ProviderServiceRegistration> serviceRegistrations)
        {
            Diagnostics = diagnostics;
            ServiceRegistrations = new List<ProviderServiceRegistration>(serviceRegistrations).AsReadOnly();
        }

        public ProviderDiagnosticsSnapshot Diagnostics { get; }
        public IReadOnlyList<ProviderServiceRegistration> ServiceRegistrations { get; }
    }

    public sealed class ProviderProfiledSnapshot
    {
        internal ProviderProfiledSnapshot(
            IEnumerable<ProviderProfiledProvider> providers,
            int scopeCount)
        {
            Providers = new List<ProviderProfiledProvider>(providers).AsReadOnly();
            ScopeCount = scopeCount;
        }

        public IReadOnlyList<ProviderProfiledProvider> Providers { get; }
        public int ScopeCount { get; }
    }

    public static class ProviderProfiledDataSource
    {
        public static ProviderProfiledSnapshot Capture()
        {
            LServicesDiagnosticsSnapshot services = LServices.GetDiagnosticsSnapshot();
            IEnumerable<ProviderServiceRegistration> registrations = services.Registrations.Select(
                registration => new ProviderServiceRegistration(
                    registration.ServiceType,
                    registration.Identifier,
                    registration.Instance,
                    registration.ScopeName,
                    registration.IsOwned));

            return CreateSnapshot(registrations, services.ScopeCount);
        }

        public static ProviderProfiledSnapshot CreateSnapshot(
            IEnumerable<ProviderServiceRegistration> registrations,
            int scopeCount = 0)
        {
            var providerRegistrations = new Dictionary<SystemProviderBase, List<ProviderServiceRegistration>>(
                ReferenceComparer<SystemProviderBase>.Instance);

            foreach (ProviderServiceRegistration registration in
                     registrations ?? Array.Empty<ProviderServiceRegistration>())
            {
                if (registration?.Instance is not SystemProviderBase provider)
                {
                    continue;
                }

                if (!providerRegistrations.TryGetValue(provider, out List<ProviderServiceRegistration> aliases))
                {
                    aliases = new List<ProviderServiceRegistration>();
                    providerRegistrations.Add(provider, aliases);
                }

                aliases.Add(registration);
            }

            ProviderProfiledProvider[] providers = providerRegistrations
                .OrderBy(item => item.Key.GetType().FullName, StringComparer.Ordinal)
                .Select(item => new ProviderProfiledProvider(
                    item.Key.GetDiagnosticsSnapshot(),
                    SortRegistrations(item.Value)))
                .ToArray();

            return new ProviderProfiledSnapshot(providers, scopeCount);
        }

        private static IEnumerable<ProviderServiceRegistration> SortRegistrations(
            IEnumerable<ProviderServiceRegistration> registrations)
        {
            return registrations
                .OrderBy(item => item.ScopeName, StringComparer.Ordinal)
                .ThenBy(item => item.ServiceType.FullName, StringComparer.Ordinal)
                .ThenBy(item => item.Identifier?.ToString(), StringComparer.Ordinal);
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
