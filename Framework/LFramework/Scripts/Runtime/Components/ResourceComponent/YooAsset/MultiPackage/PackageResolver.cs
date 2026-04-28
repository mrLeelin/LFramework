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
            return ResolveWithDiagnostics(address, explicitPackageId, defaultPackageId).FinalPackageId;
        }

        public PackageRouteResolutionResult ResolveWithDiagnostics(string address, string explicitPackageId, string defaultPackageId)
        {
            var result = new PackageRouteResolutionResult
            {
                RequestedAddress = address,
                ExplicitPackageId = explicitPackageId,
                DefaultPackageId = defaultPackageId
            };

            if (!string.IsNullOrWhiteSpace(explicitPackageId))
            {
                result.UsedExplicitPackageId = true;
                result.FinalPackageId = ResolvePackageIdWithFallback(explicitPackageId, result.MutableFallbackChain)
                                        ?? ResolveDefaultPackageId(defaultPackageId, result);
                result.UsedFallback = DidUseFallback(result);
                return result;
            }

            if (_routingSettings.enableRouteIndex &&
                !string.IsNullOrWhiteSpace(address) &&
                _addressToPackageId.TryGetValue(address, out string packageId))
            {
                result.UsedRouteIndex = true;
                result.RouteIndexPackageId = packageId;
                result.FinalPackageId = ResolvePackageIdWithFallback(packageId, result.MutableFallbackChain)
                                        ?? ResolveDefaultPackageId(defaultPackageId, result);
                result.UsedFallback = DidUseFallback(result);
                return result;
            }

            result.FinalPackageId = ResolveDefaultPackageId(defaultPackageId, result);
            result.UsedFallback = DidUseFallback(result);
            return result;
        }

        private string ResolveDefaultPackageId(string defaultPackageId, PackageRouteResolutionResult result)
        {
            if (!_routingSettings.allowDefaultPackageFallback)
            {
                return null;
            }

            if (result != null)
            {
                result.UsedDefaultPackage = true;
            }

            return ResolvePackageIdWithFallback(defaultPackageId, result?.MutableFallbackChain) ?? defaultPackageId;
        }

        private string ResolvePackageIdWithFallback(string packageId, List<string> fallbackChain)
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
                fallbackChain?.Add(currentPackageId);

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

        private static bool DidUseFallback(PackageRouteResolutionResult result)
        {
            if (result == null || result.FallbackChain.Count == 0 || string.IsNullOrWhiteSpace(result.FinalPackageId))
            {
                return false;
            }

            return !string.Equals(result.FallbackChain[0], result.FinalPackageId);
        }
    }
}
