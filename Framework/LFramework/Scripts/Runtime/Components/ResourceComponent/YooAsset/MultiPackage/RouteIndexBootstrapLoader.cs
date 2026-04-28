using System;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Runtime
{
    public class RouteIndexBootstrapLoader
    {
        private readonly PackageRegistry _registry;
        private readonly RoutingSettings _routingSettings;
        private readonly string _defaultPackageId;

        public RouteIndexBootstrapLoader(PackageRegistry registry, RoutingSettings routingSettings, string defaultPackageId)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _routingSettings = routingSettings ?? throw new ArgumentNullException(nameof(routingSettings));
            _defaultPackageId = defaultPackageId;
        }

        public bool TryGetBootstrapRequest(out string packageId, out string address, out string errorMessage)
        {
            packageId = null;
            address = null;
            errorMessage = null;

            string resolvedPackageId = ResolveBootstrapPackageId();
            if (string.IsNullOrWhiteSpace(resolvedPackageId))
            {
                errorMessage = "Route index bootstrap package id is empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(_routingSettings.routeIndexAddress))
            {
                errorMessage = "Route index address is empty.";
                return false;
            }

            if (_registry.GetPackage(resolvedPackageId) == null)
            {
                errorMessage = $"Bootstrap package '{resolvedPackageId}' is not configured.";
                return false;
            }

            packageId = resolvedPackageId;
            address = _routingSettings.routeIndexAddress;
            return true;
        }

        private string ResolveBootstrapPackageId()
        {
            return !string.IsNullOrWhiteSpace(_routingSettings.routeIndexPackageId)
                ? _routingSettings.routeIndexPackageId
                : _defaultPackageId;
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
