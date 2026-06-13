using System;
using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Runtime
{
    public readonly struct YooAssetPackageDownloadPlan
    {
        public YooAssetPackageDownloadPlan(string packageId, string packageName, IReadOnlyList<string> labels)
        {
            PackageId = packageId;
            PackageName = packageName;
            Labels = labels ?? Array.Empty<string>();
        }

        public string PackageId { get; }
        public string PackageName { get; }
        public IReadOnlyList<string> Labels { get; }
    }

    public static class YooAssetMultiPackageUtility
    {
        public static PackageRegistry CreateRegistry(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            var registry = new PackageRegistry();
            if (setting == null)
            {
                return registry;
            }

            registry.Configure(setting.GetEffectivePackageDefinitions(), platform, channel);
            return registry;
        }

        public static string ResolveDefaultPackageId(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            if (setting == null)
            {
                return null;
            }

            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            return ResolvePackageId(setting, registry, null);
        }

        public static string ResolveDefaultPackageName(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            if (setting == null)
            {
                return null;
            }

            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            string packageId = ResolvePackageId(setting, registry, null);
            return ResolvePackageName(registry, packageId);
        }

        public static string ResolveRouteIndexPackageName(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            if (setting == null || !setting.YooAssetRouting.enableRouteIndex)
            {
                return null;
            }

            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            string packageId = ResolvePackageId(setting, registry, setting.GetResolvedRouteIndexPackageId());
            return ResolvePackageName(registry, packageId);
        }

        public static List<string> ResolveDownloadAssetsPackageNames(
            ResourceComponentSetting setting,
            RuntimePlatform platform,
            string channel,
            string explicitPackageId = null)
        {
            var packageNames = new List<string>();
            if (setting == null)
            {
                return packageNames;
            }

            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            string packageId = ResolvePackageId(setting, registry, explicitPackageId);
            string packageName = ResolvePackageName(registry, packageId);
            if (!string.IsNullOrWhiteSpace(packageName))
            {
                packageNames.Add(packageName);
            }

            return packageNames;
        }

        public static List<PackageDefinition> CollectBuildPackages(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            string defaultPackageId = ResolvePackageId(setting, registry, null);
            string routeIndexPackageId = ResolveRouteIndexPackageId(setting, registry);
            return OrderPackages(registry.ActivePackages.Values, defaultPackageId, routeIndexPackageId);
        }

        public static List<PackageDefinition> CollectManifestUpdatePackages(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            string defaultPackageId = ResolvePackageId(setting, registry, null);
            string routeIndexPackageId = ResolveRouteIndexPackageId(setting, registry);

            return OrderPackages(
                registry.ActivePackages.Values.Where(definition =>
                    definition != null &&
                    (definition.updateManifestOnLaunch || IsSamePackage(definition.packageId, routeIndexPackageId))),
                defaultPackageId,
                routeIndexPackageId);
        }

        public static List<YooAssetPackageDownloadPlan> CollectDownloadPlans(
            ResourceComponentSetting setting,
            RuntimePlatform platform,
            string channel,
            IEnumerable<string> globalLabels)
        {
            PackageRegistry registry = CreateRegistry(setting, platform, channel);
            string defaultPackageId = ResolvePackageId(setting, registry, null);
            string routeIndexPackageId = ResolveRouteIndexPackageId(setting, registry);
            List<PackageDefinition> packages = OrderPackages(
                registry.ActivePackages.Values.Where(definition =>
                    definition != null &&
                    (definition.downloadOnLaunch || IsSamePackage(definition.packageId, routeIndexPackageId))),
                defaultPackageId,
                routeIndexPackageId);

            var plans = new List<YooAssetPackageDownloadPlan>(packages.Count);
            for (int i = 0; i < packages.Count; i++)
            {
                PackageDefinition package = packages[i];
                if (package == null || string.IsNullOrWhiteSpace(package.packageId) || string.IsNullOrWhiteSpace(package.yooPackageName))
                {
                    continue;
                }

                var labels = new HashSet<string>(StringComparer.Ordinal);
                AddLabels(labels, globalLabels);
                if (labels.Count == 0)
                {
                    continue;
                }

                plans.Add(new YooAssetPackageDownloadPlan(
                    package.packageId,
                    package.yooPackageName,
                    labels.ToList()));
            }

            return plans;
        }

        private static void AddLabels(HashSet<string> labels, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (string value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    labels.Add(value);
                }
            }
        }

        private static string ResolveRouteIndexPackageId(ResourceComponentSetting setting, PackageRegistry registry)
        {
            if (setting == null || registry == null || !setting.YooAssetRouting.enableRouteIndex)
            {
                return null;
            }

            return ResolvePackageId(setting, registry, setting.GetResolvedRouteIndexPackageId());
        }

        private static string ResolvePackageId(ResourceComponentSetting setting, PackageRegistry registry, string explicitPackageId)
        {
            if (setting == null || registry == null)
            {
                return null;
            }

            var resolver = new PackageResolver(setting.YooAssetRouting, registry);
            return resolver.ResolvePackageId(null, explicitPackageId, setting.GetResolvedDefaultPackageId());
        }

        private static string ResolvePackageName(PackageRegistry registry, string packageId)
        {
            PackageDefinition definition = registry?.GetPackage(packageId);
            return definition?.yooPackageName;
        }

        private static List<PackageDefinition> OrderPackages(
            IEnumerable<PackageDefinition> definitions,
            string defaultPackageId,
            string routeIndexPackageId)
        {
            var packageMap = new Dictionary<string, PackageDefinition>(StringComparer.Ordinal);
            if (definitions != null)
            {
                foreach (PackageDefinition definition in definitions)
                {
                    if (definition == null || string.IsNullOrWhiteSpace(definition.packageId) || string.IsNullOrWhiteSpace(definition.yooPackageName))
                    {
                        continue;
                    }

                    if (!packageMap.ContainsKey(definition.packageId))
                    {
                        packageMap.Add(definition.packageId, definition.Clone());
                    }
                }
            }

            var ordered = new List<PackageDefinition>(packageMap.Count);
            AddOrderedPackage(ordered, packageMap, defaultPackageId);
            AddOrderedPackage(ordered, packageMap, routeIndexPackageId);

            foreach (KeyValuePair<string, PackageDefinition> pair in packageMap.OrderBy(item => item.Key, StringComparer.Ordinal))
            {
                ordered.Add(pair.Value.Clone());
            }

            return ordered;
        }

        private static void AddOrderedPackage(
            List<PackageDefinition> ordered,
            Dictionary<string, PackageDefinition> packageMap,
            string packageId)
        {
            if (string.IsNullOrWhiteSpace(packageId))
            {
                return;
            }

            if (!packageMap.TryGetValue(packageId, out PackageDefinition definition))
            {
                return;
            }

            ordered.Add(definition.Clone());
            packageMap.Remove(packageId);
        }

        private static bool IsSamePackage(string left, string right)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
