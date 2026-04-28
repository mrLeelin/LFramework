using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;

namespace LFramework.Editor
{
    /// <summary>
    /// Source record used to build a route index.
    /// </summary>
    public sealed class RouteIndexSource
    {
        /// <summary>
        /// The logical package id owning the address.
        /// </summary>
        public string PackageId { get; set; }

        /// <summary>
        /// The project asset path.
        /// </summary>
        public string AssetPath { get; set; }

        /// <summary>
        /// The resolved address from YooAsset collection.
        /// </summary>
        public string Address { get; set; }

        /// <summary>
        /// Whether the source should be emitted into the route index.
        /// </summary>
        public bool IncludeInRouteIndex { get; set; }
    }

    /// <summary>
    /// Builds deterministic route-index entries from collected package sources.
    /// </summary>
    public static class RouteIndexBuilder
    {
        /// <summary>
        /// Builds route-index entries from collected package sources.
        /// </summary>
        public static List<RouteIndexEntry> BuildEntries(IEnumerable<RouteIndexSource> sources, bool detectDuplicateAddress)
        {
            var routes = new Dictionary<string, string>(StringComparer.Ordinal);
            if (sources != null)
            {
                foreach (RouteIndexSource source in sources)
                {
                    if (source == null ||
                        !source.IncludeInRouteIndex ||
                        string.IsNullOrWhiteSpace(source.PackageId))
                    {
                        continue;
                    }

                    string address = string.IsNullOrWhiteSpace(source.Address)
                        ? source.AssetPath
                        : source.Address;
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        continue;
                    }

                    if (detectDuplicateAddress &&
                        routes.TryGetValue(address, out string existingPackageId) &&
                        !string.Equals(existingPackageId, source.PackageId, StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Duplicate route index address '{address}' detected between packages '{existingPackageId}' and '{source.PackageId}'.");
                    }

                    routes[address] = source.PackageId;
                }
            }

            return routes
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new RouteIndexEntry
                {
                    address = pair.Key,
                    packageId = pair.Value
                })
                .ToList();
        }
    }
}
