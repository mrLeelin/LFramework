using System.Collections.Generic;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Runtime
{
    public class PackageRegistry
    {
        private readonly Dictionary<string, PackageDefinition> _activePackages = new Dictionary<string, PackageDefinition>();

        public IReadOnlyDictionary<string, PackageDefinition> ActivePackages => _activePackages;

        public void Configure(IEnumerable<PackageDefinition> definitions, RuntimePlatform platform, string channel)
        {
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

                if (!Matches(definition.platformFilter, platform.ToString()) ||
                    !Matches(definition.channelFilter, channel))
                {
                    continue;
                }

                if (_activePackages.TryGetValue(definition.packageId, out PackageDefinition existing))
                {
                    if (definition.routePriority < existing.routePriority)
                    {
                        _activePackages[definition.packageId] = definition.Clone();
                    }
                }
                else
                {
                    _activePackages.Add(definition.packageId, definition.Clone());
                }
            }
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
