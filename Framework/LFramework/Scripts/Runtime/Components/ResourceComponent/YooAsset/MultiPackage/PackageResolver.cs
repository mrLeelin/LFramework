using System.Collections.Generic;
using LFramework.Runtime.Settings;

namespace LFramework.Runtime
{
    public class PackageResolver
    {
        private readonly RoutingSettings _routingSettings;
        private readonly Dictionary<string, string> _addressToPackageId = new Dictionary<string, string>();

        public PackageResolver(RoutingSettings routingSettings)
        {
            _routingSettings = routingSettings ?? new RoutingSettings();
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
                return explicitPackageId;
            }

            if (_routingSettings.enableRouteIndex &&
                !string.IsNullOrWhiteSpace(address) &&
                _addressToPackageId.TryGetValue(address, out string packageId))
            {
                return packageId;
            }

            if (_routingSettings.allowDefaultPackageFallback)
            {
                return defaultPackageId;
            }

            return null;
        }
    }
}
