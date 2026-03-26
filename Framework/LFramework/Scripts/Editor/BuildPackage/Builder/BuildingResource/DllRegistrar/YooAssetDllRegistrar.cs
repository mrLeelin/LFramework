using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
#if YOOASSET_SUPPORT
using YooAsset.Editor;
#endif

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// Registers HybridCLR DLL folders into the active YooAsset package.
    /// </summary>
    public class YooAssetDllRegistrar : IDllResourceRegistrar
    {
        private const string DefaultPackageName = "DefaultPackage";

        public bool RegisterAotDlls(List<string> dllPaths, HybridCLRSetting setting)
        {
#if YOOASSET_SUPPORT
            return RegisterDllCollectors(
                dllPaths,
                setting,
                setting.aotAddressableGroupName,
                setting.defaultInitLabel,
                setting.defaultAotDllLabel);
#else
            Debug.LogError("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
            return false;
#endif
        }

        public bool RegisterHotfixDlls(List<string> dllPaths, HybridCLRSetting setting)
        {
#if YOOASSET_SUPPORT
            return RegisterDllCollectors(
                dllPaths,
                setting,
                setting.codeAddressableGroupName,
                setting.defaultInitLabel,
                setting.defaultCodeDllLabel);
#else
            Debug.LogError("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
            return false;
#endif
        }

        public bool EnsureGroupExists(string groupName)
        {
#if YOOASSET_SUPPORT
            return RecreateGroup(groupName);
#else
            Debug.LogError("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings -> Scripting Define Symbols.");
            return false;
#endif
        }

#if YOOASSET_SUPPORT
        private static bool RegisterDllCollectors(
            List<string> dllPaths,
            HybridCLRSetting setting,
            string groupName,
            params string[] labels)
        {
            if (setting == null)
            {
                Debug.LogError("HybridCLRSetting is null.");
                return false;
            }

            var package = GetOrCreatePackage();
            if (!RecreateGroup(groupName))
            {
                return false;
            }

            var group = package.Groups.FirstOrDefault(item => string.Equals(item.GroupName, groupName, StringComparison.Ordinal));
            if (group == null)
            {
                Debug.LogError($"Failed to recreate YooAsset group '{groupName}'.");
                return false;
            }

            group.ActiveRuleName = nameof(EnableGroup);
            group.AssetTags = string.Join(",",
                labels.Where(label => !string.IsNullOrWhiteSpace(label)).Distinct(StringComparer.Ordinal));

            if (dllPaths == null || dllPaths.Count == 0)
            {
                Debug.LogWarning($"No DLL paths found for YooAsset group '{groupName}'.");
                AssetBundleCollectorSettingData.ModifyGroup(package, group);
                AssetBundleCollectorSettingData.ModifyPackage(package);
                AssetBundleCollectorSettingData.SaveFile();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }

            foreach (var folderPath in GetDistinctUnityFolders(dllPaths))
            {
                var folderGuid = AssetDatabase.AssetPathToGUID(folderPath);
                if (string.IsNullOrWhiteSpace(folderGuid))
                {
                    Debug.LogWarning($"Skip YooAsset collector creation because folder GUID is missing: {folderPath}");
                    continue;
                }

                AssetBundleCollectorSettingData.CreateCollector(group, new AssetBundleCollector
                {
                    CollectPath = folderPath,
                    CollectorGUID = folderGuid,
                    CollectorType = ECollectorType.MainAssetCollector,
                    AddressRuleName = nameof(AddressByFileName),
                    PackRuleName = nameof(PackSeparately),
                    FilterRuleName = nameof(CollectAll),
                    AssetTags = string.Empty,
                    UserData = string.Empty
                });
            }

            AssetBundleCollectorSettingData.ModifyGroup(package, group);
            AssetBundleCollectorSettingData.ModifyPackage(package);
            AssetBundleCollectorSettingData.SaveFile();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        private static bool RecreateGroup(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
            {
                Debug.LogError("YooAsset group name is empty.");
                return false;
            }

            var package = GetOrCreatePackage();
            var group = package.Groups.FirstOrDefault(item => string.Equals(item.GroupName, groupName, StringComparison.Ordinal));
            if (group != null)
            {
                AssetBundleCollectorSettingData.RemoveGroup(package, group);
            }

            group = AssetBundleCollectorSettingData.CreateGroup(package, groupName);
            group.ActiveRuleName = nameof(EnableGroup);
            AssetBundleCollectorSettingData.ModifyGroup(package, group);
            AssetBundleCollectorSettingData.ModifyPackage(package);
            AssetBundleCollectorSettingData.SaveFile();
            return true;
        }

        private static AssetBundleCollectorPackage GetOrCreatePackage()
        {
            var collectorSetting = AssetBundleCollectorSettingData.Setting;
            if (collectorSetting == null)
            {
                throw new InvalidOperationException("YooAsset AssetBundleCollectorSettingData.Setting is null.");
            }

            var packageName = AssetUtilities.GetAllAssetsOfType<ResourceComponentSetting>().FirstOrDefault()?.YooAssetPackageName;
            if (string.IsNullOrWhiteSpace(packageName))
            {
                packageName = DefaultPackageName;
            }

            var package = collectorSetting.Packages.FirstOrDefault(item =>
                string.Equals(item.PackageName, packageName, StringComparison.Ordinal));
            if (package == null)
            {
                package = AssetBundleCollectorSettingData.CreatePackage(packageName);
            }

            package.PackageName = packageName;
            package.EnableAddressable = true;
            package.IgnoreRuleName = string.IsNullOrWhiteSpace(package.IgnoreRuleName)
                ? nameof(NormalIgnoreRule)
                : package.IgnoreRuleName;

            return package;
        }

        private static IEnumerable<string> GetDistinctUnityFolders(IEnumerable<string> dllPaths)
        {
            return dllPaths
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(FullPathToUnityFolderPath)
                .Where(path => !string.IsNullOrWhiteSpace(path) && AssetDatabase.IsValidFolder(path))
                .Distinct(StringComparer.Ordinal);
        }

        private static string FullPathToUnityFolderPath(string fullFilePath)
        {
            var normalized = fullFilePath.Replace('\\', '/');
            var directory = Path.GetDirectoryName(normalized)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(directory))
            {
                return string.Empty;
            }

            var dataPath = Application.dataPath.Replace('\\', '/');
            if (!directory.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return "Assets" + directory.Substring(dataPath.Length);
        }
#endif
    }
}
