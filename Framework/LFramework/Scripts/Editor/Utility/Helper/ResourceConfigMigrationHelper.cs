#if YOOASSET_SUPPORT
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using YooAsset.Editor;

namespace LFramework.Editor
{
    internal static class ResourceConfigMigrationHelper
    {
        private const string GeneratedAddressableGroupPrefix = "LFW_YOO__";
        private const string DefaultFallbackPackageName = "DefaultPackage";
        private const string AddressableHelperTypeName = "LFramework.Runtime.AddressableResourceHelper";
        private const string YooAssetHelperTypeName = "LFramework.Runtime.YooAssetResourceHelper";

        private static readonly string ConversionRootPath =
            Path.Combine(Directory.GetCurrentDirectory(), "Library", "LFramework", "ResourceConversion");

        public static ResourceConfigMigrationResult ConvertYooAssetsToAddressables(ResourceComponentSetting setting)
        {
            var report = new MigrationReport("YooAssetsToAddressables");
            try
            {
                EnsureDirectory(ConversionRootPath);
                ValidateResourceSetting(setting, report);

                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (addressableSettings == null)
                {
                    throw new InvalidOperationException("AddressableAssetSettings not found.");
                }

                var collectorSetting = AssetBundleCollectorSettingData.Setting;
                if (collectorSetting == null)
                {
                    throw new InvalidOperationException("AssetBundleCollectorSetting not found.");
                }

                var packageName = GetTargetPackageName(setting);
                var package = collectorSetting.GetPackage(packageName);
                package.CheckConfigError();

                var sourceAssets = CollectYooAssets(package, collectorSetting.UniqueBundleName, report);
                if (sourceAssets.Count == 0)
                {
                    report.AddWarning($"YooAssets package '{packageName}' has no collectible assets.");
                    return Finish(report, false);
                }

                var planEntries = BuildAddressablePlan(sourceAssets, packageName, report);
                ThrowIfHasErrors(report, "Addressable plan generation failed.");

                ApplyAddressablePlan(addressableSettings, planEntries, report);
                VerifyAddressablePlan(addressableSettings, planEntries, report);
                ThrowIfHasErrors(report, "Addressable verification failed.");

                SaveYooRoundTripMapping(package, sourceAssets, report);
                SwitchResourceMode(setting, ResourceMode.Addressable, AddressableHelperTypeName, package.PackageName, report);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                report.AddInfo(
                    $"Converted {planEntries.Count} assets from YooAssets package '{package.PackageName}' to Addressables.");
                return Finish(report, true);
            }
            catch (Exception ex)
            {
                report.AddError($"Unhandled exception: {ex}");
                return Finish(report, false);
            }
        }

        public static ResourceConfigMigrationResult ConvertAddressablesToYooAssets(ResourceComponentSetting setting)
        {
            var report = new MigrationReport("AddressablesToYooAssets");
            try
            {
                EnsureDirectory(ConversionRootPath);
                ValidateResourceSetting(setting, report);

                var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
                if (addressableSettings == null)
                {
                    throw new InvalidOperationException("AddressableAssetSettings not found.");
                }

                var collectorSetting = AssetBundleCollectorSettingData.Setting;
                if (collectorSetting == null)
                {
                    throw new InvalidOperationException("AssetBundleCollectorSetting not found.");
                }

                EnsureCustomRulesAvailable(report);

                var packageName = GetTargetPackageName(setting);
                var snapshots = CollectAddressableEntries(addressableSettings, report);
                if (snapshots.Count == 0)
                {
                    report.AddWarning("No convertible Addressable entries were found.");
                    return Finish(report, false);
                }

                ThrowIfHasErrors(report, "Addressable snapshot collection failed.");

                var mappingState = LoadYooRoundTripMapping(packageName, report);
                var plan = BuildYooPlan(snapshots, packageName, mappingState, report);
                ThrowIfHasErrors(report, "YooAssets plan generation failed.");

                ApplyYooPlan(collectorSetting, packageName, plan, report);
                VerifyYooPlan(packageName, snapshots, report);
                ThrowIfHasErrors(report, "YooAssets verification failed.");

                SwitchResourceMode(setting, ResourceMode.YooAsset, YooAssetHelperTypeName, packageName, report);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                report.AddInfo($"Converted {snapshots.Count} Addressable assets to YooAssets package '{packageName}'.");
                return Finish(report, true);
            }
            catch (Exception ex)
            {
                report.AddError($"Unhandled exception: {ex}");
                return Finish(report, false);
            }
        }

        private static List<YooSourceAssetRecord> CollectYooAssets(
            AssetBundleCollectorPackage package,
            bool uniqueBundleName,
            MigrationReport report)
        {
            var results = new List<YooSourceAssetRecord>(256);
            var ignoreRule = AssetBundleCollectorSettingData.GetIgnoreRuleInstance(package.IgnoreRuleName);
            var command = new CollectCommand(package.PackageName, ignoreRule)
            {
                UniqueBundleName = uniqueBundleName,
                UseAssetDependencyDB = true,
                EnableAddressable = package.EnableAddressable,
                SupportExtensionless = package.SupportExtensionless,
                LocationToLower = package.LocationToLower,
                IncludeAssetGUID = package.IncludeAssetGUID,
                AutoCollectShaders = package.AutoCollectShaders
            };

            foreach (var group in package.Groups)
            {
                if (group == null)
                {
                    continue;
                }

                var activeRule = AssetBundleCollectorSettingData.GetActiveRuleInstance(group.ActiveRuleName);
                if (!activeRule.IsActiveGroup(new GroupData(group.GroupName)))
                {
                    report.AddInfo($"Skip inactive YooAssets group '{group.GroupName}'.");
                    continue;
                }

                for (var collectorIndex = 0; collectorIndex < group.Collectors.Count; collectorIndex++)
                {
                    var collector = group.Collectors[collectorIndex];
                    if (collector == null)
                    {
                        continue;
                    }

                    var collectedAssets = collector.GetAllCollectAssets(command, group);
                    foreach (var collectedAsset in collectedAssets)
                    {
                        var assetInfo = collectedAsset.AssetInfo;
                        if (assetInfo == null || string.IsNullOrEmpty(assetInfo.AssetGUID))
                        {
                            report.AddError($"Invalid YooAssets entry in collector '{collector.CollectPath}'.");
                            continue;
                        }

                        results.Add(new YooSourceAssetRecord
                        {
                            AssetGuid = assetInfo.AssetGUID,
                            AssetPath = assetInfo.AssetPath,
                            ExpectedAddress = string.IsNullOrWhiteSpace(collectedAsset.Address)
                                ? assetInfo.AssetPath
                                : collectedAsset.Address,
                            BundleName = collectedAsset.BundleName,
                            AssetTags = collectedAsset.AssetTags == null
                                ? Array.Empty<string>()
                                : collectedAsset.AssetTags
                                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                                    .Distinct(StringComparer.Ordinal)
                                    .OrderBy(tag => tag, StringComparer.Ordinal)
                                    .ToArray(),
                            PackageName = package.PackageName,
                            PackageDesc = package.PackageDesc,
                            GroupName = group.GroupName,
                            GroupDesc = group.GroupDesc,
                            GroupTags = group.AssetTags,
                            ActiveRuleName = group.ActiveRuleName,
                            CollectorIndex = collectorIndex,
                            CollectorPath = collector.CollectPath,
                            CollectorGuid = string.IsNullOrWhiteSpace(collector.CollectorGUID)
                                ? AssetDatabase.AssetPathToGUID(collector.CollectPath)
                                : collector.CollectorGUID,
                            CollectorType = collector.CollectorType,
                            AddressRuleName = collector.AddressRuleName,
                            PackRuleName = collector.PackRuleName,
                            FilterRuleName = collector.FilterRuleName,
                            CollectorTags = collector.AssetTags,
                            UserData = collector.UserData
                        });
                    }
                }
            }

            return results;
        }

        private static List<AddressablePlanEntry> BuildAddressablePlan(
            List<YooSourceAssetRecord> sourceAssets,
            string packageName,
            MigrationReport report)
        {
            var addressIndex = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var assetIndex = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var bag = new ConcurrentBag<AddressablePlanEntry>();
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            };

            Parallel.ForEach(sourceAssets, options, record =>
            {
                if (!assetIndex.TryAdd(record.AssetGuid, record.AssetPath))
                {
                    report.AddError(
                        $"Duplicate YooAssets asset guid detected: guid={record.AssetGuid}, path={record.AssetPath}.");
                    return;
                }

                if (!addressIndex.TryAdd(record.ExpectedAddress, record.AssetPath))
                {
                    report.AddError(
                        $"Duplicate target Addressable address '{record.ExpectedAddress}'. " +
                        $"Conflict between '{addressIndex[record.ExpectedAddress]}' and '{record.AssetPath}'.");
                    return;
                }

                bag.Add(new AddressablePlanEntry
                {
                    AssetGuid = record.AssetGuid,
                    AssetPath = record.AssetPath,
                    Address = record.ExpectedAddress,
                    BundleName = record.BundleName,
                    Labels = record.AssetTags,
                    GroupName = ComposeGeneratedAddressableGroupName(packageName, record.BundleName)
                });
            });

            return bag
                .OrderBy(entry => entry.GroupName, StringComparer.Ordinal)
                .ThenBy(entry => entry.AssetPath, StringComparer.Ordinal)
                .ToList();
        }

        private static void ApplyAddressablePlan(
            AddressableAssetSettings settings,
            List<AddressablePlanEntry> planEntries,
            MigrationReport report)
        {
            RemoveGeneratedAddressableGroups(settings, report);

            var groups = new Dictionary<string, AddressableAssetGroup>(StringComparer.Ordinal);
            foreach (var groupName in planEntries.Select(x => x.GroupName).Distinct(StringComparer.Ordinal))
            {
                groups[groupName] = CreateGeneratedAddressableGroup(settings, groupName);
                report.AddInfo($"Created Addressable group '{groupName}'.");
            }

            foreach (var entryPlan in planEntries)
            {
                var existingEntry = settings.FindAssetEntry(entryPlan.AssetGuid);
                if (existingEntry != null && existingEntry.parentGroup != null &&
                    !string.Equals(existingEntry.parentGroup.Name, entryPlan.GroupName, StringComparison.Ordinal))
                {
                    report.AddWarning(
                        $"Moving Addressable entry '{entryPlan.AssetPath}' from group '{existingEntry.parentGroup.Name}' " +
                        $"to '{entryPlan.GroupName}'.");
                }

                var entry = settings.CreateOrMoveEntry(entryPlan.AssetGuid, groups[entryPlan.GroupName], false, false);
                entry.SetAddress(entryPlan.Address, false);
                ResetEntryLabels(entry);
                foreach (var label in entryPlan.Labels)
                {
                    entry.SetLabel(label, true, true, false);
                }
            }
        }

        private static void VerifyAddressablePlan(
            AddressableAssetSettings settings,
            List<AddressablePlanEntry> planEntries,
            MigrationReport report)
        {
            var expected = planEntries.ToDictionary(x => x.AssetGuid, x => x, StringComparer.Ordinal);
            var actual = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var group in settings.groups)
            {
                if (group == null || !group.Name.StartsWith(GeneratedAddressableGroupPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var entry in group.entries)
                {
                    if (entry == null)
                    {
                        continue;
                    }

                    actual[entry.guid] = entry.address;
                }
            }

            foreach (var expectedEntry in expected.Values)
            {
                if (!actual.TryGetValue(expectedEntry.AssetGuid, out var actualAddress))
                {
                    report.AddError(
                        $"Addressable verification missing asset: guid={expectedEntry.AssetGuid}, path={expectedEntry.AssetPath}.");
                    continue;
                }

                if (!string.Equals(expectedEntry.Address, actualAddress, StringComparison.Ordinal))
                {
                    report.AddError(
                        $"Addressable verification address mismatch for '{expectedEntry.AssetPath}'. " +
                        $"Expected '{expectedEntry.Address}', actual '{actualAddress}'.");
                }
            }

            foreach (var actualGuid in actual.Keys)
            {
                if (!expected.ContainsKey(actualGuid))
                {
                    report.AddError(
                        $"Addressable verification found unexpected asset guid={actualGuid}, path={AssetDatabase.GUIDToAssetPath(actualGuid)}.");
                }
            }
        }

        private static List<AddressableSnapshot> CollectAddressableEntries(
            AddressableAssetSettings settings,
            MigrationReport report)
        {
            var snapshots = new List<AddressableSnapshot>(256);
            var seenGuids = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var group in settings.groups.OrderBy(group => group == null ? string.Empty : group.Name, StringComparer.Ordinal))
            {
                if (group == null)
                {
                    continue;
                }

                if (group.ReadOnly)
                {
                    report.AddInfo($"Skip read-only Addressable group '{group.Name}'.");
                    continue;
                }

                var schema = group.GetSchema<BundledAssetGroupSchema>();
                if (schema == null)
                {
                    if (group.entries.Count > 0)
                    {
                        report.AddWarning($"Skip Addressable group '{group.Name}' because it has no BundledAssetGroupSchema.");
                    }
                    continue;
                }

                var gatheredEntries = new List<AddressableAssetEntry>();
                group.GatherAllAssets(gatheredEntries, true, true, false, entry => entry != null && !entry.ReadOnly);

                foreach (var entry in gatheredEntries)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.guid))
                    {
                        continue;
                    }

                    var assetPath = entry.AssetPath;
                    if (string.IsNullOrWhiteSpace(assetPath))
                    {
                        report.AddWarning($"Skip Addressable entry with empty asset path. guid={entry.guid}");
                        continue;
                    }

                    if (AssetDatabase.IsValidFolder(assetPath))
                    {
                        continue;
                    }

                    if (!assetPath.StartsWith("Assets/", StringComparison.Ordinal))
                    {
                        report.AddWarning($"Skip non-project Addressable entry '{assetPath}'.");
                        continue;
                    }

                    if (entry.IsSubAsset)
                    {
                        report.AddError(
                            $"Explicit sub-asset Addressable entry is not supported by automatic YooAssets conversion: '{assetPath}'.");
                        continue;
                    }

                    if (seenGuids.TryGetValue(entry.guid, out var existingGroupName))
                    {
                        report.AddError(
                            $"Duplicate Addressable asset guid detected across groups. guid={entry.guid}, path={assetPath}, " +
                            $"groups='{existingGroupName}' and '{group.Name}'.");
                        continue;
                    }

                    seenGuids.Add(entry.guid, group.Name);
                    snapshots.Add(new AddressableSnapshot
                    {
                        AssetGuid = entry.guid,
                        AssetPath = assetPath,
                        Address = string.IsNullOrWhiteSpace(entry.address) ? assetPath : entry.address,
                        GroupName = group.Name,
                        Labels = entry.labels == null
                            ? Array.Empty<string>()
                            : entry.labels.OrderBy(label => label, StringComparer.Ordinal).ToArray(),
                        BundleMode = schema.BundleMode
                    });
                }
            }

            return snapshots
                .OrderBy(snapshot => snapshot.GroupName, StringComparer.Ordinal)
                .ThenBy(snapshot => snapshot.AssetPath, StringComparer.Ordinal)
                .ToList();
        }

        private static YooBuildPlan BuildYooPlan(
            List<AddressableSnapshot> snapshots,
            string packageName,
            YooRoundTripMappingState mappingState,
            MigrationReport report)
        {
            var plan = new YooBuildPlan();
            var handledGuids = new HashSet<string>(StringComparer.Ordinal);
            if (mappingState != null &&
                string.Equals(mappingState.PackageName, packageName, StringComparison.Ordinal))
            {
                plan.PackageMetadata = mappingState;
            }

            if (mappingState != null &&
                string.Equals(mappingState.PackageName, packageName, StringComparison.Ordinal))
            {
                var mappingByGuid = mappingState.Entries.ToDictionary(record => record.AssetGuid, record => record, StringComparer.Ordinal);
                var expectedCollectors = mappingState.Entries
                    .GroupBy(record => record.GetCollectorKey(), StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
                var candidateCollectors = snapshots
                    .Where(snapshot =>
                        mappingByGuid.TryGetValue(snapshot.AssetGuid, out var record) &&
                        string.Equals(record.AssetPath, snapshot.AssetPath, StringComparison.Ordinal) &&
                        string.Equals(record.ExpectedAddress, snapshot.Address, StringComparison.Ordinal))
                    .GroupBy(snapshot => mappingByGuid[snapshot.AssetGuid].GetCollectorKey(), StringComparer.Ordinal);

                foreach (var candidateCollector in candidateCollectors)
                {
                    if (!expectedCollectors.TryGetValue(candidateCollector.Key, out var expectedRecords))
                    {
                        continue;
                    }

                    var expectedSet = new HashSet<string>(expectedRecords.Select(record => record.AssetGuid), StringComparer.Ordinal);
                    var actualSet = new HashSet<string>(candidateCollector.Select(snapshot => snapshot.AssetGuid), StringComparer.Ordinal);
                    if (!expectedSet.SetEquals(actualSet))
                    {
                        continue;
                    }

                    var template = expectedRecords[0];
                    var groupPlan = plan.GetOrCreateGroup(
                        template.GroupName,
                        string.IsNullOrWhiteSpace(template.GroupDesc) ? $"Recovered from Addressables ({template.GroupName})" : template.GroupDesc,
                        template.GroupTags,
                        string.IsNullOrWhiteSpace(template.ActiveRuleName) ? nameof(EnableGroup) : template.ActiveRuleName);

                    groupPlan.Collectors.Add(new YooCollectorPlan
                    {
                        CollectPath = template.CollectorPath,
                        CollectorGuid = template.CollectorGuid,
                        CollectorType = (ECollectorType)template.CollectorType,
                        AddressRuleName = template.AddressRuleName,
                        PackRuleName = template.PackRuleName,
                        FilterRuleName = template.FilterRuleName,
                        AssetTags = template.CollectorTags,
                        UserData = template.UserData
                    });

                    foreach (var guid in actualSet)
                    {
                        handledGuids.Add(guid);
                    }
                }
            }

            var fallbackCollectors = new ConcurrentBag<FallbackCollectorPlan>();
            var addressIndex = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount)
            };

            var mappingLookup = mappingState != null &&
                                string.Equals(mappingState.PackageName, packageName, StringComparison.Ordinal)
                ? mappingState.Entries.ToDictionary(record => record.AssetGuid, record => record, StringComparer.Ordinal)
                : new Dictionary<string, YooRoundTripEntryRecord>(StringComparer.Ordinal);

            Parallel.ForEach(snapshots.Where(snapshot => !handledGuids.Contains(snapshot.AssetGuid)), options, snapshot =>
            {
                if (!addressIndex.TryAdd(snapshot.Address, snapshot.AssetPath))
                {
                    report.AddError(
                        $"Duplicate Addressable address detected during YooAssets conversion: '{snapshot.Address}'. " +
                        $"Conflict between '{addressIndex[snapshot.Address]}' and '{snapshot.AssetPath}'.");
                    return;
                }

                var mappingRecord = mappingLookup.TryGetValue(snapshot.AssetGuid, out var record) ? record : null;
                var targetGroupName = mappingRecord != null && !string.IsNullOrWhiteSpace(mappingRecord.GroupName)
                    ? mappingRecord.GroupName
                    : snapshot.GroupName;
                var targetGroupDesc = mappingRecord != null && !string.IsNullOrWhiteSpace(mappingRecord.GroupDesc)
                    ? mappingRecord.GroupDesc
                    : $"Generated from Addressables group '{snapshot.GroupName}'.";
                var targetGroupTags = mappingRecord == null ? string.Empty : mappingRecord.GroupTags;
                var bundleName = ComputeFallbackBundleName(snapshot, report);
                var userData = AddressableYooMigrationUserDataUtility.Serialize(snapshot.Address, bundleName);

                fallbackCollectors.Add(new FallbackCollectorPlan
                {
                    GroupName = targetGroupName,
                    GroupDesc = targetGroupDesc,
                    GroupTags = targetGroupTags,
                    ActiveRuleName = mappingRecord != null && !string.IsNullOrWhiteSpace(mappingRecord.ActiveRuleName)
                        ? mappingRecord.ActiveRuleName
                        : nameof(EnableGroup),
                    Collector = new YooCollectorPlan
                    {
                        CollectPath = snapshot.AssetPath,
                        CollectorGuid = snapshot.AssetGuid,
                        CollectorType = ECollectorType.MainAssetCollector,
                        AddressRuleName = nameof(AddressByMigrationUserData),
                        PackRuleName = nameof(PackByMigrationUserData),
                        FilterRuleName = nameof(CollectAll),
                        AssetTags = snapshot.Labels.Length == 0
                            ? string.Empty
                            : string.Join(";", snapshot.Labels),
                        UserData = userData
                    }
                });
            });

            foreach (var fallback in fallbackCollectors.OrderBy(item => item.GroupName, StringComparer.Ordinal)
                         .ThenBy(item => item.Collector.CollectPath, StringComparer.Ordinal))
            {
                var groupPlan = plan.GetOrCreateGroup(
                    fallback.GroupName,
                    fallback.GroupDesc,
                    fallback.GroupTags,
                    fallback.ActiveRuleName);
                groupPlan.Collectors.Add(fallback.Collector);
            }

            return plan;
        }

        private static void ApplyYooPlan(
            AssetBundleCollectorSetting collectorSetting,
            string packageName,
            YooBuildPlan plan,
            MigrationReport report)
        {
            var package = collectorSetting.Packages.FirstOrDefault(item =>
                string.Equals(item.PackageName, packageName, StringComparison.Ordinal));
            if (package == null)
            {
                package = AssetBundleCollectorSettingData.CreatePackage(packageName);
            }

            package.PackageName = packageName;
            package.PackageDesc = plan.PackageMetadata == null ? string.Empty : plan.PackageMetadata.PackageDesc;
            package.EnableAddressable = true;
            package.SupportExtensionless = plan.PackageMetadata == null || plan.PackageMetadata.SupportExtensionless;
            package.LocationToLower = plan.PackageMetadata != null && plan.PackageMetadata.LocationToLower;
            package.IncludeAssetGUID = plan.PackageMetadata == null || plan.PackageMetadata.IncludeAssetGuid;
            package.AutoCollectShaders = plan.PackageMetadata == null || plan.PackageMetadata.AutoCollectShaders;
            package.IgnoreRuleName = plan.PackageMetadata == null || string.IsNullOrWhiteSpace(plan.PackageMetadata.IgnoreRuleName)
                ? nameof(NormalIgnoreRule)
                : plan.PackageMetadata.IgnoreRuleName;
            package.Groups.Clear();

            foreach (var groupPlan in plan.Groups.OrderBy(group => group.GroupName, StringComparer.Ordinal))
            {
                var group = AssetBundleCollectorSettingData.CreateGroup(package, groupPlan.GroupName);
                group.GroupDesc = groupPlan.GroupDesc;
                group.AssetTags = groupPlan.GroupTags;
                group.ActiveRuleName = string.IsNullOrWhiteSpace(groupPlan.ActiveRuleName)
                    ? nameof(EnableGroup)
                    : groupPlan.ActiveRuleName;

                foreach (var collectorPlan in groupPlan.Collectors.OrderBy(collector => collector.CollectPath, StringComparer.Ordinal))
                {
                    if (string.IsNullOrWhiteSpace(collectorPlan.CollectorGuid))
                    {
                        collectorPlan.CollectorGuid = AssetDatabase.AssetPathToGUID(collectorPlan.CollectPath);
                    }

                    AssetBundleCollectorSettingData.CreateCollector(group, new AssetBundleCollector
                    {
                        CollectPath = collectorPlan.CollectPath,
                        CollectorGUID = collectorPlan.CollectorGuid,
                        CollectorType = collectorPlan.CollectorType,
                        AddressRuleName = collectorPlan.AddressRuleName,
                        PackRuleName = collectorPlan.PackRuleName,
                        FilterRuleName = collectorPlan.FilterRuleName,
                        AssetTags = collectorPlan.AssetTags,
                        UserData = collectorPlan.UserData
                    });
                }
            }

            AssetBundleCollectorSettingData.ModifyPackage(package);
            AssetBundleCollectorSettingData.SaveFile();
            report.AddInfo($"Rebuilt YooAssets package '{packageName}' with {plan.Groups.Count} groups.");
        }

        private static void VerifyYooPlan(
            string packageName,
            List<AddressableSnapshot> sourceSnapshots,
            MigrationReport report)
        {
            var collectorSetting = AssetBundleCollectorSettingData.Setting;
            var collectResult = collectorSetting.BeginCollect(packageName, false, true);
            var actual = collectResult.CollectAssets.ToDictionary(
                asset => asset.AssetInfo.AssetGUID,
                asset => asset,
                StringComparer.Ordinal);
            var expected = sourceSnapshots.ToDictionary(
                snapshot => snapshot.AssetGuid,
                snapshot => snapshot.Address,
                StringComparer.Ordinal);

            foreach (var sourceSnapshot in sourceSnapshots)
            {
                if (!actual.TryGetValue(sourceSnapshot.AssetGuid, out var actualAsset))
                {
                    report.AddError(
                        $"YooAssets verification missing asset: guid={sourceSnapshot.AssetGuid}, path={sourceSnapshot.AssetPath}.");
                    continue;
                }

                if (actualAsset.CollectorType == ECollectorType.MainAssetCollector)
                {
                    var actualAddress = string.IsNullOrWhiteSpace(actualAsset.Address)
                        ? actualAsset.AssetInfo.AssetPath
                        : actualAsset.Address;
                    if (!string.Equals(sourceSnapshot.Address, actualAddress, StringComparison.Ordinal))
                    {
                        report.AddError(
                            $"YooAssets verification address mismatch for '{sourceSnapshot.AssetPath}'. " +
                            $"Expected '{sourceSnapshot.Address}', actual '{actualAddress}'.");
                    }
                }
            }

            foreach (var actualGuid in actual.Keys)
            {
                if (!expected.ContainsKey(actualGuid))
                {
                    report.AddError(
                        $"YooAssets verification found unexpected asset guid={actualGuid}, path={AssetDatabase.GUIDToAssetPath(actualGuid)}.");
                }
            }
        }

        private static void SaveYooRoundTripMapping(
            AssetBundleCollectorPackage package,
            List<YooSourceAssetRecord> sourceAssets,
            MigrationReport report)
        {
            var state = new YooRoundTripMappingState
            {
                PackageName = package.PackageName,
                PackageDesc = package.PackageDesc,
                SupportExtensionless = package.SupportExtensionless,
                LocationToLower = package.LocationToLower,
                IncludeAssetGuid = package.IncludeAssetGUID,
                AutoCollectShaders = package.AutoCollectShaders,
                IgnoreRuleName = package.IgnoreRuleName,
                Entries = sourceAssets.Select(asset => new YooRoundTripEntryRecord
                {
                    AssetGuid = asset.AssetGuid,
                    AssetPath = asset.AssetPath,
                    ExpectedAddress = asset.ExpectedAddress,
                    ExpectedBundleName = asset.BundleName,
                    GroupName = asset.GroupName,
                    GroupDesc = asset.GroupDesc,
                    GroupTags = asset.GroupTags,
                    ActiveRuleName = asset.ActiveRuleName,
                    CollectorPath = asset.CollectorPath,
                    CollectorGuid = asset.CollectorGuid,
                    CollectorType = (int)asset.CollectorType,
                    AddressRuleName = asset.AddressRuleName,
                    PackRuleName = asset.PackRuleName,
                    FilterRuleName = asset.FilterRuleName,
                    CollectorTags = asset.CollectorTags,
                    UserData = asset.UserData,
                    CollectorIndex = asset.CollectorIndex
                }).ToList()
            };

            var mappingPath = GetMappingFilePath(package.PackageName);
            File.WriteAllText(mappingPath, JsonUtility.ToJson(state, true), Encoding.UTF8);
            report.AddInfo($"Saved round-trip mapping: {mappingPath}");
        }

        private static YooRoundTripMappingState LoadYooRoundTripMapping(string packageName, MigrationReport report)
        {
            var mappingPath = GetMappingFilePath(packageName);
            if (!File.Exists(mappingPath))
            {
                report.AddInfo($"Round-trip mapping not found: {mappingPath}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(mappingPath, Encoding.UTF8);
                var state = JsonUtility.FromJson<YooRoundTripMappingState>(json);
                if (state == null)
                {
                    report.AddWarning($"Failed to parse round-trip mapping file: {mappingPath}");
                    return null;
                }

                return state;
            }
            catch (Exception ex)
            {
                report.AddWarning($"Failed to load round-trip mapping '{mappingPath}': {ex.Message}");
                return null;
            }
        }

        private static AddressableAssetGroup CreateGeneratedAddressableGroup(AddressableAssetSettings settings, string groupName)
        {
            var existing = settings.FindGroup(groupName);
            if (existing != null)
            {
                settings.RemoveGroup(existing);
            }

            var group = settings.CreateGroup(
                groupName,
                false,
                false,
                false,
                null,
                typeof(ContentUpdateGroupSchema),
                typeof(BundledAssetGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            schema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.GUID;
            var contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
            contentUpdateSchema.StaticContent = false;
            return group;
        }

        private static void RemoveGeneratedAddressableGroups(AddressableAssetSettings settings, MigrationReport report)
        {
            var groupsToRemove = settings.groups
                .Where(group => group != null &&
                                group.Name.StartsWith(GeneratedAddressableGroupPrefix, StringComparison.Ordinal))
                .ToArray();
            foreach (var group in groupsToRemove)
            {
                settings.RemoveGroup(group);
                report.AddInfo($"Removed generated Addressable group '{group.Name}'.");
            }
        }

        private static void ResetEntryLabels(AddressableAssetEntry entry)
        {
            if (entry.labels == null || entry.labels.Count == 0)
            {
                return;
            }

            foreach (var label in entry.labels.ToArray())
            {
                entry.SetLabel(label, false, true, false);
            }
        }

        private static string ComputeFallbackBundleName(AddressableSnapshot snapshot, MigrationReport report)
        {
            switch (snapshot.BundleMode)
            {
                case BundledAssetGroupSchema.BundlePackingMode.PackSeparately:
                    return RemoveExtension(snapshot.AssetPath);

                case BundledAssetGroupSchema.BundlePackingMode.PackTogether:
                    return $"addressables/{SanitizeName(snapshot.GroupName)}";

                case BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel:
                    if (snapshot.Labels.Length == 0)
                    {
                        report.AddWarning(
                            $"Addressable group '{snapshot.GroupName}' uses PackTogetherByLabel but asset '{snapshot.AssetPath}' has no labels. Falling back to group bundle.");
                        return $"addressables/{SanitizeName(snapshot.GroupName)}";
                    }

                    var labelKey = string.Join("_", snapshot.Labels);
                    return $"addressables/{SanitizeName(snapshot.GroupName)}/label_{ComputeShortHash(labelKey)}";

                default:
                    report.AddWarning(
                        $"Unknown Addressable bundle mode '{snapshot.BundleMode}' for asset '{snapshot.AssetPath}'. Falling back to group bundle.");
                    return $"addressables/{SanitizeName(snapshot.GroupName)}";
            }
        }

        private static string ComposeGeneratedAddressableGroupName(string packageName, string bundleName)
        {
            var leafName = Path.GetFileName(bundleName);
            if (string.IsNullOrWhiteSpace(leafName))
            {
                leafName = "bundle";
            }

            return $"{GeneratedAddressableGroupPrefix}{SanitizeName(packageName)}__{SanitizeName(leafName)}__{ComputeShortHash(bundleName)}";
        }

        private static string GetTargetPackageName(ResourceComponentSetting setting)
        {
            return string.IsNullOrWhiteSpace(setting.YooAssetPackageName)
                ? DefaultFallbackPackageName
                : setting.YooAssetPackageName;
        }

        private static string GetMappingFilePath(string packageName)
        {
            return Path.Combine(ConversionRootPath, $"yoo_roundtrip_{SanitizeName(packageName)}.json");
        }

        private static void ValidateResourceSetting(ResourceComponentSetting setting, MigrationReport report)
        {
            if (setting == null)
            {
                report.AddError("ResourceComponentSetting is null.");
                ThrowIfHasErrors(report, "ResourceComponentSetting is required.");
            }
        }

        private static void EnsureCustomRulesAvailable(MigrationReport report)
        {
            if (!AssetBundleCollectorSettingData.HasAddressRuleName(nameof(AddressByMigrationUserData)))
            {
                report.AddError($"Missing YooAssets address rule: {nameof(AddressByMigrationUserData)}");
            }

            if (!AssetBundleCollectorSettingData.HasPackRuleName(nameof(PackByMigrationUserData)))
            {
                report.AddError($"Missing YooAssets pack rule: {nameof(PackByMigrationUserData)}");
            }
        }

        private static void SwitchResourceMode(
            ResourceComponentSetting setting,
            ResourceMode mode,
            string helperTypeName,
            string packageName,
            MigrationReport report)
        {
            var serializedObject = new SerializedObject(setting);
            serializedObject.Update();

            serializedObject.FindProperty("_resourceMode").enumValueIndex = (int)mode;
            serializedObject.FindProperty("m_ResourceHelperTypeName").stringValue = helperTypeName;

            var packageProperty = serializedObject.FindProperty("_yooAssetPackageName");
            if (packageProperty != null && !string.IsNullOrWhiteSpace(packageName))
            {
                packageProperty.stringValue = packageName;
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(setting);
            report.AddInfo($"Switched ResourceComponentSetting mode to '{mode}'.");
        }

        private static ResourceConfigMigrationResult Finish(MigrationReport report, bool success)
        {
            var reportPath = report.FlushToDisk(ConversionRootPath, success);
            if (success)
            {
                Debug.Log($"[ResourceMigration] Success. Report: {reportPath}");
            }
            else
            {
                Debug.LogError($"[ResourceMigration] Failed. Report: {reportPath}");
            }

            return new ResourceConfigMigrationResult(success, reportPath, report.BuildSummary());
        }

        private static void ThrowIfHasErrors(MigrationReport report, string message)
        {
            if (report.ErrorCount > 0)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "unnamed";
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                builder.Append(char.IsLetterOrDigit(ch) || ch == '_' ? ch : '_');
            }

            return builder.ToString();
        }

        private static string ComputeShortHash(string value)
        {
            using var md5 = MD5.Create();
            var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
            return BitConverter.ToString(bytes, 0, 6).Replace("-", string.Empty);
        }

        private static string RemoveExtension(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return assetPath;
            }

            var index = assetPath.LastIndexOf(".", StringComparison.Ordinal);
            return index < 0 ? assetPath : assetPath.Substring(0, index);
        }

        internal readonly struct ResourceConfigMigrationResult
        {
            public ResourceConfigMigrationResult(bool success, string reportPath, string summary)
            {
                Success = success;
                ReportPath = reportPath;
                Summary = summary;
            }

            public bool Success { get; }
            public string ReportPath { get; }
            public string Summary { get; }
        }

        [Serializable]
        private sealed class YooRoundTripMappingState
        {
            public string PackageName;
            public string PackageDesc;
            public bool SupportExtensionless;
            public bool LocationToLower;
            public bool IncludeAssetGuid;
            public bool AutoCollectShaders;
            public string IgnoreRuleName;
            public List<YooRoundTripEntryRecord> Entries = new List<YooRoundTripEntryRecord>();
        }

        [Serializable]
        private sealed class YooRoundTripEntryRecord
        {
            public string AssetGuid;
            public string AssetPath;
            public string ExpectedAddress;
            public string ExpectedBundleName;
            public string GroupName;
            public string GroupDesc;
            public string GroupTags;
            public string ActiveRuleName;
            public string CollectorPath;
            public string CollectorGuid;
            public int CollectorType;
            public string AddressRuleName;
            public string PackRuleName;
            public string FilterRuleName;
            public string CollectorTags;
            public string UserData;
            public int CollectorIndex;

            public string GetCollectorKey()
            {
                return string.Join(
                    "|",
                    GroupName ?? string.Empty,
                    CollectorIndex.ToString(),
                    CollectorPath ?? string.Empty,
                    CollectorType.ToString(),
                    AddressRuleName ?? string.Empty,
                    PackRuleName ?? string.Empty,
                    FilterRuleName ?? string.Empty,
                    CollectorTags ?? string.Empty,
                    UserData ?? string.Empty);
            }
        }

        private sealed class YooBuildPlan
        {
            private readonly Dictionary<string, YooGroupPlan> _groups = new Dictionary<string, YooGroupPlan>(StringComparer.Ordinal);

            public YooRoundTripMappingState PackageMetadata { get; set; }
            public IReadOnlyCollection<YooGroupPlan> Groups => _groups.Values;

            public YooGroupPlan GetOrCreateGroup(string groupName, string groupDesc, string groupTags, string activeRuleName)
            {
                if (!_groups.TryGetValue(groupName, out var group))
                {
                    group = new YooGroupPlan
                    {
                        GroupName = groupName,
                        GroupDesc = groupDesc,
                        GroupTags = groupTags,
                        ActiveRuleName = activeRuleName
                    };
                    _groups.Add(groupName, group);
                }

                return group;
            }
        }

        private sealed class YooGroupPlan
        {
            public string GroupName;
            public string GroupDesc;
            public string GroupTags;
            public string ActiveRuleName;
            public List<YooCollectorPlan> Collectors = new List<YooCollectorPlan>();
        }

        private sealed class YooCollectorPlan
        {
            public string CollectPath;
            public string CollectorGuid;
            public ECollectorType CollectorType;
            public string AddressRuleName;
            public string PackRuleName;
            public string FilterRuleName;
            public string AssetTags;
            public string UserData;
        }

        private sealed class FallbackCollectorPlan
        {
            public string GroupName;
            public string GroupDesc;
            public string GroupTags;
            public string ActiveRuleName;
            public YooCollectorPlan Collector;
        }

        private sealed class YooSourceAssetRecord
        {
            public string AssetGuid;
            public string AssetPath;
            public string ExpectedAddress;
            public string BundleName;
            public string[] AssetTags;
            public string PackageName;
            public string PackageDesc;
            public string GroupName;
            public string GroupDesc;
            public string GroupTags;
            public string ActiveRuleName;
            public int CollectorIndex;
            public string CollectorPath;
            public string CollectorGuid;
            public ECollectorType CollectorType;
            public string AddressRuleName;
            public string PackRuleName;
            public string FilterRuleName;
            public string CollectorTags;
            public string UserData;
        }

        private sealed class AddressablePlanEntry
        {
            public string AssetGuid;
            public string AssetPath;
            public string Address;
            public string BundleName;
            public string GroupName;
            public string[] Labels;
        }

        private sealed class AddressableSnapshot
        {
            public string AssetGuid;
            public string AssetPath;
            public string Address;
            public string GroupName;
            public string[] Labels;
            public BundledAssetGroupSchema.BundlePackingMode BundleMode;
        }

        private sealed class MigrationReport
        {
            private readonly ConcurrentQueue<string> _infos = new ConcurrentQueue<string>();
            private readonly ConcurrentQueue<string> _warnings = new ConcurrentQueue<string>();
            private readonly ConcurrentQueue<string> _errors = new ConcurrentQueue<string>();
            private readonly DateTime _startedAt = DateTime.Now;
            private readonly string _name;

            public MigrationReport(string name)
            {
                _name = name;
            }

            public int ErrorCount => _errors.Count;

            public void AddInfo(string message)
            {
                _infos.Enqueue(message);
            }

            public void AddWarning(string message)
            {
                _warnings.Enqueue(message);
            }

            public void AddError(string message)
            {
                _errors.Enqueue(message);
            }

            public string BuildSummary()
            {
                return $"info={_infos.Count}, warning={_warnings.Count}, error={_errors.Count}";
            }

            public string FlushToDisk(string rootPath, bool success)
            {
                EnsureDirectory(rootPath);
                var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{_name}_{(success ? "success" : "failed")}.log";
                var reportPath = Path.Combine(rootPath, fileName);
                var builder = new StringBuilder(4096);
                builder.AppendLine($"Name: {_name}");
                builder.AppendLine($"Success: {success}");
                builder.AppendLine($"Started: {_startedAt:yyyy-MM-dd HH:mm:ss}");
                builder.AppendLine($"Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                builder.AppendLine($"DurationMs: {(DateTime.Now - _startedAt).TotalMilliseconds:F0}");
                builder.AppendLine($"ThreadBudget: {Math.Max(1, Environment.ProcessorCount)}");
                builder.AppendLine($"Summary: {BuildSummary()}");
                builder.AppendLine();

                AppendSection(builder, "Errors", _errors);
                AppendSection(builder, "Warnings", _warnings);
                AppendSection(builder, "Infos", _infos);

                File.WriteAllText(reportPath, builder.ToString(), Encoding.UTF8);
                return reportPath;
            }

            private static void AppendSection(StringBuilder builder, string title, IEnumerable<string> messages)
            {
                builder.AppendLine($"[{title}]");
                foreach (var message in messages)
                {
                    builder.AppendLine(message);
                }

                builder.AppendLine();
            }
        }
    }

    [Serializable]
    internal sealed class AddressableYooMigrationUserData
    {
        public string ExactAddress;
        public string BundleName;
    }

    internal static class AddressableYooMigrationUserDataUtility
    {
        public static string Serialize(string exactAddress, string bundleName)
        {
            return JsonUtility.ToJson(new AddressableYooMigrationUserData
            {
                ExactAddress = exactAddress,
                BundleName = bundleName
            });
        }

        public static bool TryDeserialize(string userData, out AddressableYooMigrationUserData payload)
        {
            payload = null;
            if (string.IsNullOrWhiteSpace(userData))
            {
                return false;
            }

            try
            {
                payload = JsonUtility.FromJson<AddressableYooMigrationUserData>(userData);
                return payload != null &&
                       !string.IsNullOrWhiteSpace(payload.ExactAddress) &&
                       !string.IsNullOrWhiteSpace(payload.BundleName);
            }
            catch
            {
                payload = null;
                return false;
            }
        }
    }
}
#else
using LFramework.Runtime.Settings;

namespace LFramework.Editor
{
    internal static class ResourceConfigMigrationHelper
    {
        public static ResourceConfigMigrationResult ConvertYooAssetsToAddressables(ResourceComponentSetting setting)
        {
            return new ResourceConfigMigrationResult(false, string.Empty, "YOOASSET_SUPPORT is not enabled.");
        }

        public static ResourceConfigMigrationResult ConvertAddressablesToYooAssets(ResourceComponentSetting setting)
        {
            return new ResourceConfigMigrationResult(false, string.Empty, "YOOASSET_SUPPORT is not enabled.");
        }

        internal readonly struct ResourceConfigMigrationResult
        {
            public ResourceConfigMigrationResult(bool success, string reportPath, string summary)
            {
                Success = success;
                ReportPath = reportPath;
                Summary = summary;
            }

            public bool Success { get; }
            public string ReportPath { get; }
            public string Summary { get; }
        }
    }
}
#endif
