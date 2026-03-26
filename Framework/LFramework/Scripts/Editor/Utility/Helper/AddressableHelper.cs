#if ADDRESSABLE_SUPPORT
using System.Collections;
using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;


namespace LFramework.Editor
{
    public static class AddressableHelper
    {
        /// <summary>
        /// Generate a new Addressable Asset Group with the specified name and settings.
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="settings"></param>
        /// <param name="entries"></param>
        /// <param name="group"></param>
        /// <param name="bundledAssetGroupSchema"></param>
        /// <returns></returns>
        public static void GenerateUpdateGroup(
            string groupName,
            AddressableAssetSettings settings,
            List<AddressableAssetEntry> entries,
            out AddressableAssetGroup group,
            out BundledAssetGroupSchema bundledAssetGroupSchema)
        {
            ContentUpdateScript.CreateContentUpdateGroup(settings, entries,
                groupName);
            group = settings.FindGroup(groupName);
            bundledAssetGroupSchema = group.GetSchema<BundledAssetGroupSchema>();
            bundledAssetGroupSchema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            bundledAssetGroupSchema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.GUID;
        }


        /// <summary>
        /// 构建一个本地的组
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="settings"></param>
        /// <param name="entries"></param>
        /// <param name="group"></param>
        /// <param name="schema"></param>
        public static void GenerateDefaultGroup( string groupName,
            AddressableAssetSettings settings,
            List<AddressableAssetEntry> entries,
            out AddressableAssetGroup group,
            out BundledAssetGroupSchema schema)
        {
            group = settings.CreateGroup(groupName, false, false, false, null, typeof(ContentUpdateGroupSchema), typeof(BundledAssetGroupSchema));
            schema = group.GetSchema<BundledAssetGroupSchema>();
            schema.BuildPath.SetVariableByName(settings, AddressableAssetSettings.kLocalBuildPath);
            schema.LoadPath.SetVariableByName(settings, AddressableAssetSettings.kLocalLoadPath);
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            schema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.GUID;
            var contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
            contentUpdateSchema.StaticContent = false;
        }
    }
}
#endif
