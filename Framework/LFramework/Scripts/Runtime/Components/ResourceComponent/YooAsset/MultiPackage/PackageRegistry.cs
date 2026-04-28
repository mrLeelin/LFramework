using System.Collections.Generic;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Runtime
{
    public class PackageRegistry
    {
        private readonly Dictionary<string, PackageDefinition> _configuredPackages = new Dictionary<string, PackageDefinition>();
        private readonly Dictionary<string, PackageDefinition> _activePackages = new Dictionary<string, PackageDefinition>();

        public IReadOnlyDictionary<string, PackageDefinition> ActivePackages => _activePackages;

        public void Configure(IEnumerable<PackageDefinition> definitions, RuntimePlatform platform, string channel)
        {
            _configuredPackages.Clear();
            _activePackages.Clear();
            if (definitions == null)
            {
                return;
            }

            foreach (PackageDefinition definition in definitions)
            {
                if (definition == null || string.IsNullOrWhiteSpace(definition.packageId))
                {
                    continue;
                }

                AddOrReplace(_configuredPackages, definition);

                if (!Matches(definition.platformFilter, platform.ToString()) ||
                    !Matches(definition.channelFilter, channel))
                {
                    continue;
                }

                AddOrReplace(_activePackages, definition);
            }
        }

        public PackageDefinition GetConfiguredPackage(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return null;
            }

            _configuredPackages.TryGetValue(packageId, out PackageDefinition definition);
            return definition;
        }

        public PackageDefinition GetPackage(string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return null;
            }

            _activePackages.TryGetValue(packageId, out PackageDefinition definition);
            return definition;
        }

        private static void AddOrReplace(Dictionary<string, PackageDefinition> packageMap, PackageDefinition definition)
        {
            if (packageMap.TryGetValue(definition.packageId, out PackageDefinition existing))
            {
                if (definition.routePriority < existing.routePriority)
                {
                    packageMap[definition.packageId] = definition.Clone();
                }
            }
            else
            {
                packageMap.Add(definition.packageId, definition.Clone());
            }
        }

        private static bool Matches(List<string> filters, string value)
        {
            if (filters == null || filters.Count == 0)
            {
                return true;
            }

            foreach (string filter in filters)
            {
                if (string.Equals(filter, value, System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
