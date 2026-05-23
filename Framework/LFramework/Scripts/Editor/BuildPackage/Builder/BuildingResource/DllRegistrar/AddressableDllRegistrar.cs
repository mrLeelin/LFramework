
#if ADDRESSABLE_SUPPORT
using System.Collections.Generic;
using LFramework.Editor.Builder;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 基于 Addressable 的 DLL 资源注册器实现
    /// 从 BuildDllsHelper 中提取的 Addressable 注册逻辑
    /// </summary>
    public class AddressableDllRegistrar : IDllResourceRegistrar
    {
        private readonly AddressableAssetSettings _settings;

        public AddressableDllRegistrar()
        {
            _settings = AddressableAssetSettingsDefaultObject.Settings;
        }

        public bool RegisterAotDlls(List<string> dllPaths, HybridCLRSetting setting, BuildType buildType)
        {
            if (!EnsureGroupExists(setting.aotAddressableGroupName, buildType))
            {
                return false;
            }

            var group = _settings.FindGroup(setting.aotAddressableGroupName);
            if (dllPaths == null || dllPaths.Count == 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }

            foreach (var path in dllPaths)
            {
                var assetsGuid = AssetDatabase.AssetPathToGUID(FullPathToUnityPath(path));
                var entry = _settings.CreateOrMoveEntry(assetsGuid, group);
                entry.SetLabel(setting.defaultAotDllLabel, true, true);
                entry.SetLabel(setting.defaultInitLabel, true, true);
                _settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public bool RegisterHotfixDlls(List<string> dllPaths, HybridCLRSetting setting, BuildType buildType)
        {
            if (!EnsureGroupExists(setting.codeAddressableGroupName, buildType))
            {
                return false;
            }

            var group = _settings.FindGroup(setting.codeAddressableGroupName);
            if (dllPaths == null || dllPaths.Count == 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return true;
            }

            foreach (var path in dllPaths)
            {
                var assetsGuid = AssetDatabase.AssetPathToGUID(FullPathToUnityPath(path));
                var entry = _settings.CreateOrMoveEntry(assetsGuid, group);
                entry.SetLabel(setting.defaultInitLabel, true, true);
                entry.SetLabel(setting.defaultCodeDllLabel, true, true);
                _settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public bool EnsureGroupExists(string groupName)
        {
            return EnsureGroupExists(groupName, BuildType.App);
        }

        private bool EnsureGroupExists(string groupName, BuildType buildType)
        {
            var group = _settings.FindGroup(groupName);
            if (group != null)
            {
                _settings.RemoveGroup(group);
            }

            AddressableHelper.GenerateDefaultGroup(groupName, _settings, null, out group,
                out var groupSchema);
            if (buildType == BuildType.ResourcesUpdate)
            {
                groupSchema.BuildPath.SetVariableByName(_settings, AddressableAssetSettings.kRemoteBuildPath);
                groupSchema.LoadPath.SetVariableByName(_settings, AddressableAssetSettings.kRemoteLoadPath);
            }
            else
            {
                groupSchema.BuildPath.SetVariableByName(_settings, AddressableAssetSettings.kLocalBuildPath);
                groupSchema.LoadPath.SetVariableByName(_settings, AddressableAssetSettings.kLocalLoadPath);
            }

            groupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            var contentUpdateSchema = group.GetSchema<ContentUpdateGroupSchema>();
            if (contentUpdateSchema != null)
            {
                contentUpdateSchema.StaticContent = false;
            }

            return true;
        }

        private static string FullPathToUnityPath(string fullFilePath)
        {
            return "Assets/" + fullFilePath.Replace(Application.dataPath + "/", "");
        }
    }
}
#endif

