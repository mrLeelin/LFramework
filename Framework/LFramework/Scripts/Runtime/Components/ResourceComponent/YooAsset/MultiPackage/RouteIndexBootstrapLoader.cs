using System;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Runtime
{
    public class RouteIndexBootstrapLoader
    {
        private readonly PackageRegistry _registry;
        private readonly RoutingSettings _routingSettings;

        public RouteIndexBootstrapLoader(PackageRegistry registry, RoutingSettings routingSettings)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _routingSettings = routingSettings ?? throw new ArgumentNullException(nameof(routingSettings));
        }

        public bool TryGetBootstrapRequest(out string packageId, out string address, out string errorMessage)
        {
            packageId = null;
            address = null;
            errorMessage = null;

            if (string.IsNullOrWhiteSpace(_routingSettings.routeIndexPackageId))
            {
                errorMessage = "Route index bootstrap package id is empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_routingSettings.routeIndexAddress))
            {
                errorMessage = "Route index address is empty.";
                return false;
            }

            if (_registry.GetPackage(_routingSettings.routeIndexPackageId) == null)
            {
                errorMessage = $"Bootstrap package '{_routingSettings.routeIndexPackageId}' is not configured.";
                return false;
            }

            packageId = _routingSettings.routeIndexPackageId;
            address = _routingSettings.routeIndexAddress;
            return true;
        }

        public bool TryLoad(Func<string, string, ScriptableObject> loadRouteIndex, out RouteIndexAsset routeIndex,
            out string errorMessage)
        {
            routeIndex = null;

            if (loadRouteIndex == null)
            {
                errorMessage = "Route index loader delegate is null.";
                return false;
            }

            if (!TryGetBootstrapRequest(out string packageId, out string address, out errorMessage))
            {
                return false;
            }

            try
            {
                routeIndex = loadRouteIndex(packageId, address) as RouteIndexAsset;
            }
            catch (Exception ex)
            {
                errorMessage =
                    $"Route index load threw an exception for bootstrap package '{packageId}': {ex.Message}";
                return false;
            }

            if (routeIndex == null)
            {
                errorMessage = $"Route index '{address}' could not be loaded from bootstrap package '{packageId}'.";
                return false;
            }

            return true;
        }
    }
}
