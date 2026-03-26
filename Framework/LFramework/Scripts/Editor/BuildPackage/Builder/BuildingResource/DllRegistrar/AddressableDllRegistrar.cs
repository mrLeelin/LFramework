
#if ADDRESSABLE_SUPPORT
using System.Collections.Generic;
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

        public bool RegisterAotDlls(List<string> dllPaths, HybridCLRSetting setting)
        {
            if (!EnsureGroupExists(setting.aotAddressableGroupName))
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
                entry.SetLabel(setting.defaultAotDllLabel, true);
                entry.SetLabel(setting.defaultInitLabel, true);
                _settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public bool RegisterHotfixDlls(List<string> dllPaths, HybridCLRSetting setting)
        {
            if (!EnsureGroupExists(setting.codeAddressableGroupName))
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
                entry.SetLabel(setting.defaultInitLabel, true);
                entry.SetLabel(setting.defaultCodeDllLabel, true);
                _settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }

        public bool EnsureGroupExists(string groupName)
        {
            var group = _settings.FindGroup(groupName);
            if (group != null)
            {
                _settings.RemoveGroup(group);
            }

            AddressableHelper.GenerateDefaultGroup(groupName, _settings, null, out group,
                out var groupSchema);
            groupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            return true;
        }

        private static string FullPathToUnityPath(string fullFilePath)
        {
            return "Assets/" + fullFilePath.Replace(Application.dataPath + "/", "");
        }
    }
}
#endif

