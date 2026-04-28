#if YOOASSET_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace LFramework.Editor
{
    /// <summary>
    /// Result payload for route-index generation.
    /// </summary>
    public readonly struct RouteIndexGenerationResult
    {
        /// <summary>
        /// Creates a successful result.
        /// </summary>
        public static RouteIndexGenerationResult CreateSuccess(string assetPath, int entryCount)
        {
            return new RouteIndexGenerationResult(true, assetPath, entryCount, null);
        }

        /// <summary>
        /// Creates a failed result.
        /// </summary>
        public static RouteIndexGenerationResult CreateFailure(string errorMessage)
        {
            return new RouteIndexGenerationResult(false, null, 0, errorMessage);
        }

        /// <summary>
        /// Whether generation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// The generated asset path.
        /// </summary>
        public string AssetPath { get; }

        /// <summary>
        /// The number of generated route entries.
        /// </summary>
        public int EntryCount { get; }

        /// <summary>
        /// The failure message when generation does not succeed.
        /// </summary>
        public string ErrorMessage { get; }

        private RouteIndexGenerationResult(bool succeeded, string assetPath, int entryCount, string errorMessage)
        {
            Succeeded = succeeded;
            AssetPath = assetPath;
            EntryCount = entryCount;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// Generates RouteIndexAsset content from active YooAsset package collectors.
    /// </summary>
    public static class RouteIndexGenerator
    {
        private const string GeneratedGroupName = "__GeneratedRouteIndex";

        /// <summary>
        /// Generates the route index asset and ensures the bootstrap package collects it.
        /// </summary>
        public static RouteIndexGenerationResult Generate(ResourceComponentSetting setting)
        {
            if (setting == null)
            {
                return RouteIndexGenerationResult.CreateFailure("ResourceComponentSetting is null.");
            }

            RoutingSettings routing = setting.YooAssetRouting;
            if (!setting.ValidateMultiPackageConfiguration(out List<string> errors, out _))
            {
                return RouteIndexGenerationResult.CreateFailure(string.Join(Environment.NewLine, errors));
            }
            HybridCLRSetting hybridClrSetting = SettingManager.GetSetting<HybridCLRSetting>();
            string assetTags = ResolveRouteIndexAssetTags(hybridClrSetting);

            if (string.IsNullOrWhiteSpace(routing.routeIndexAssetPath))
            {
                return RouteIndexGenerationResult.CreateFailure("Route index asset path is empty.");
            }

            RuntimePlatform platform = GetPreviewRuntimePlatform();
            string channel = GetPreviewChannel();
            var registry = new PackageRegistry();
            registry.Configure(setting.GetEffectivePackageDefinitions(), platform, channel);

            List<RouteIndexSource> sources = CollectSources(registry, routing.routeIndexAssetPath);
            List<RouteIndexEntry> entries = RouteIndexBuilder.BuildEntries(sources, routing.detectDuplicateAddress);

            RouteIndexAsset routeIndexAsset = LoadOrCreateRouteIndexAsset(routing.routeIndexAssetPath);
            routeIndexAsset.entries = entries;
            EditorUtility.SetDirty(routeIndexAsset);
            AssetDatabase.SaveAssets();

            if (!EnsureBootstrapCollector(registry, routing, setting.GetResolvedRouteIndexPackageId(), assetTags))
            {
                return RouteIndexGenerationResult.CreateFailure("Failed to configure bootstrap collector for the route index asset.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return RouteIndexGenerationResult.CreateSuccess(routing.routeIndexAssetPath, entries.Count);
        }

        private static List<RouteIndexSource> CollectSources(PackageRegistry registry, string routeIndexAssetPath)
        {
            var collectorSetting = AssetBundleCollectorSettingData.Setting;
            if (collectorSetting == null)
            {
                throw new InvalidOperationException("YooAsset AssetBundleCollectorSettingData.Setting is null.");
            }

            var sources = new List<RouteIndexSource>(256);
            foreach (KeyValuePair<string, PackageDefinition> pair in registry.ActivePackages)
            {
                PackageDefinition definition = pair.Value;
                if (definition == null || string.IsNullOrWhiteSpace(definition.yooPackageName))
                {
                    continue;
                }

                CollectResult collectResult = collectorSetting.BeginCollect(definition.yooPackageName, false, true);
                foreach (var collectedAsset in collectResult.CollectAssets)
                {
                    if (collectedAsset?.AssetInfo == null ||
                        string.Equals(collectedAsset.AssetInfo.AssetPath, routeIndexAssetPath, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    sources.Add(new RouteIndexSource
                    {
                        PackageId = pair.Key,
                        AssetPath = collectedAsset.AssetInfo.AssetPath,
                        Address = collectedAsset.Address,
                        IncludeInRouteIndex = collectedAsset.CollectorType == ECollectorType.MainAssetCollector
                    });
                }
            }

            return sources;
        }

        private static RouteIndexAsset LoadOrCreateRouteIndexAsset(string assetPath)
        {
            if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Route index asset path must be under Assets/. Path='{assetPath}'");
            }

            EnsureAssetFolder(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            RouteIndexAsset routeIndexAsset = AssetDatabase.LoadAssetAtPath<RouteIndexAsset>(assetPath);
            if (routeIndexAsset != null)
            {
                return routeIndexAsset;
            }

            routeIndexAsset = ScriptableObject.CreateInstance<RouteIndexAsset>();
            AssetDatabase.CreateAsset(routeIndexAsset, assetPath);
            return routeIndexAsset;
        }

        private static bool EnsureBootstrapCollector(PackageRegistry registry, RoutingSettings routing, string routeIndexPackageId, string assetTags)
        {
            string bootstrapPackageId = routeIndexPackageId;
            if (string.IsNullOrWhiteSpace(bootstrapPackageId))
            {
                return false;
            }

            PackageDefinition bootstrapDefinition = registry.GetPackage(bootstrapPackageId);
            if (bootstrapDefinition == null || string.IsNullOrWhiteSpace(bootstrapDefinition.yooPackageName))
            {
                return false;
            }

            var collectorSetting = AssetBundleCollectorSettingData.Setting;
            if (collectorSetting == null)
            {
                return false;
            }

            AssetBundleCollectorPackage package = collectorSetting.Packages.FirstOrDefault(item =>
                string.Equals(item.PackageName, bootstrapDefinition.yooPackageName, StringComparison.Ordinal));
            if (package == null)
            {
                package = AssetBundleCollectorSettingData.CreatePackage(bootstrapDefinition.yooPackageName);
            }

            AssetBundleCollectorGroup existingGroup = package.Groups.FirstOrDefault(item =>
                string.Equals(item.GroupName, GeneratedGroupName, StringComparison.Ordinal));
            if (existingGroup != null)
            {
                AssetBundleCollectorSettingData.RemoveGroup(package, existingGroup);
            }

            AssetBundleCollectorGroup group = AssetBundleCollectorSettingData.CreateGroup(package, GeneratedGroupName);
            group.ActiveRuleName = nameof(EnableGroup);
            AssetBundleCollectorSettingData.CreateCollector(group, new AssetBundleCollector
            {
                CollectPath = routing.routeIndexAssetPath,
                CollectorGUID = AssetDatabase.AssetPathToGUID(routing.routeIndexAssetPath),
                CollectorType = ECollectorType.MainAssetCollector,
                AddressRuleName = nameof(AddressByExactAddressUserData),
                PackRuleName = nameof(PackSeparately),
                FilterRuleName = nameof(CollectAll),
                AssetTags = assetTags,
                UserData = ExactAddressUserDataUtility.Serialize(routing.routeIndexAddress)
            });

            AssetBundleCollectorSettingData.ModifyGroup(package, group);
            AssetBundleCollectorSettingData.ModifyPackage(package);
            AssetBundleCollectorRouteIndexSaveSync.RunWithoutQueue(AssetBundleCollectorSettingData.SaveFile);
            return true;
        }

        private static string ResolveRouteIndexAssetTags(HybridCLRSetting hybridClrSetting)
        {
            if (hybridClrSetting != null && !string.IsNullOrWhiteSpace(hybridClrSetting.defaultInitLabel))
            {
                return hybridClrSetting.defaultInitLabel;
            }

            return "init_assets";
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) ||
                string.Equals(folderPath, "Assets", StringComparison.Ordinal) ||
                AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folderPath));
        }

        private static RuntimePlatform GetPreviewRuntimePlatform()
        {
            return EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneWindows => RuntimePlatform.WindowsPlayer,
                BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                _ => RuntimePlatform.WindowsEditor
            };
        }

        private static string GetPreviewChannel()
        {
            try
            {
                GameSetting gameSetting = SettingManager.GetSetting<GameSetting>();
                if (gameSetting != null && !string.IsNullOrWhiteSpace(gameSetting.channel))
                {
                    return gameSetting.channel;
                }
            }
            catch
            {
                // Keep generation resilient when settings are not ready in the editor.
            }

            return "Unknown";
        }
    }
}
#endif
