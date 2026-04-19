using System.Collections.Generic;
using LFramework.Runtime.Settings;

namespace LFramework.Runtime
{
    public class PackageResolver
    {
        private readonly RoutingSettings _routingSettings;
        private readonly PackageRegistry _packageRegistry;
        private readonly Dictionary<string, string> _addressToPackageId = new Dictionary<string, string>();

        public PackageResolver(RoutingSettings routingSettings)
            : this(routingSettings, null)
        {
        }

        public PackageResolver(RoutingSettings routingSettings, PackageRegistry packageRegistry)
        {
            _routingSettings = routingSettings ?? new RoutingSettings();
            _packageRegistry = packageRegistry;
        }

        public void LoadRouteIndex(RouteIndexAsset routeIndex)
        {
            _addressToPackageId.Clear();
            if (routeIndex == null || routeIndex.entries == null)
            {
                return;
            }

            foreach (RouteIndexEntry entry in routeIndex.entries)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.address) ||
                    string.IsNullOrWhiteSpace(entry.packageId))
                {
                    continue;
                }

                _addressToPackageId[entry.address] = entry.packageId;
            }
        }

        public string ResolvePackageId(string address, string explicitPackageId, string defaultPackageId)
        {
            if (!string.IsNullOrWhiteSpace(explicitPackageId))
            {
                return ResolvePackageIdWithFallback(explicitPackageId) ?? ResolveDefaultPackageId(defaultPackageId);
            }

            if (_routingSettings.enableRouteIndex &&
                !string.IsNullOrWhiteSpace(address) &&
                _addressToPackageId.TryGetValue(address, out string packageId))
            {
                return ResolvePackageIdWithFallback(packageId) ?? ResolveDefaultPackageId(defaultPackageId);
            }

            return ResolveDefaultPackageId(defaultPackageId);
        }

        private string ResolveDefaultPackageId(string defaultPackageId)
        {
            if (!_routingSettings.allowDefaultPackageFallback)
            {
                return null;
            }

            return ResolvePackageIdWithFallback(defaultPackageId) ?? defaultPackageId;
        }

        private string ResolvePackageIdWithFallback(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return null;
            }

            if (_packageRegistry == null)
            {
                return packageId;
            }

            var visitedPackageIds = new HashSet<string>();
            string currentPackageId = packageId;

            while (!string.IsNullOrWhiteSpace(currentPackageId) && visitedPackageIds.Add(currentPackageId))
            {
                if (_packageRegistry.GetPackage(currentPackageId) != null)
                {
                    return currentPackageId;
                }

                PackageDefinition configuredPackage = _packageRegistry.GetConfiguredPackage(currentPackageId);
                if (configuredPackage == null || string.IsNullOrWhiteSpace(configuredPackage.fallbackPackageId))
                {
                    return null;
                }

                currentPackageId = configuredPackage.fallbackPackageId;
            }

            return null;
        }
    }
}
