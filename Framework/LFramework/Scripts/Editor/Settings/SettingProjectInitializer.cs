using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// 初始化工程侧 Setting 资产。
    /// </summary>
    public static class SettingProjectInitializer
    {
        private const string TemplateRegistrySearchFilter = "t:SettingTemplateRegistry";

        /// <summary>
        /// 初始化工程正式 Setting，重复执行应保持幂等。
        /// </summary>
        public static ProjectSettingSelector InitializeProjectSettings()
        {
            EnsureProjectFoldersExist();

            ProjectSettingSelector selector = LoadOrCreateAsset<ProjectSettingSelector>(
                SettingProjectPaths.SelectorAssetPath,
                "ProjectSettingSelector");
            SettingSyncState syncState = LoadOrCreateAsset<SettingSyncState>(
                SettingProjectPaths.SyncStateAssetPath,
                "SettingSyncState");

            SettingTemplateRegistry registry = FindTemplateRegistry();

            foreach (BaseSetting template in LoadTemplateBaseSettings(registry))
            {
                BaseSetting localAsset = LoadOrCloneSetting(template, GetProjectAssetPath(template));
                selector.SetSetting(localAsset);
                syncState.UpsertRecord(BuildSyncRecord(template, localAsset));
                EditorUtility.SetDirty(localAsset);
            }

            foreach (ComponentSetting template in LoadTemplateComponentSettings(registry))
            {
                ComponentSetting localAsset = LoadOrCloneSetting(template, GetProjectAssetPath(template));
                selector.SetComponentSetting(localAsset);
                syncState.UpsertRecord(BuildSyncRecord(template, localAsset));
                EditorUtility.SetDirty(localAsset);
            }

            EditorUtility.SetDirty(selector);
            EditorUtility.SetDirty(syncState);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SettingManager.ClearCacheForTests();
            return AssetDatabase.LoadAssetAtPath<ProjectSettingSelector>(SettingProjectPaths.SelectorAssetPath);
        }

        public static List<BaseSetting> LoadTemplateBaseSettings(SettingTemplateRegistry registry = null)
        {
            return LoadTemplateAssets(registry)
                .OfType<BaseSetting>()
                .OrderBy(setting => setting.SettingId)
                .ToList();
        }

        public static List<ComponentSetting> LoadTemplateComponentSettings(SettingTemplateRegistry registry = null)
        {
            return LoadTemplateAssets(registry)
                .OfType<ComponentSetting>()
                .OrderBy(setting => setting.SettingId)
                .ToList();
        }

        public static List<ScriptableObject> LoadTemplateAssets(SettingTemplateRegistry registry = null)
        {
            registry ??= FindTemplateRegistry();
            if (registry == null)
            {
                throw new InvalidOperationException("[SettingProjectInitializer] SettingTemplateRegistry asset not found. Please create or include the registry asset.");
            }
            
            return registry.Entries
                .Where(entry => entry != null && entry.templateAsset != null)
                .Select(entry => entry.templateAsset)
                .Distinct()
                .ToList();
        }

        public static SettingTemplateRegistry FindTemplateRegistry()
        {
            string[] guids = AssetDatabase.FindAssets(TemplateRegistrySearchFilter);
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                SettingTemplateRegistry registry = AssetDatabase.LoadAssetAtPath<SettingTemplateRegistry>(assetPath);
                if (registry != null)
                {
                    return registry;
                }
            }

            return null;
        }

        private static T LoadOrCloneSetting<T>(T template, string targetPath) where T : ScriptableObject
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(targetPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), targetPath))
            {
                throw new InvalidOperationException($"[SettingProjectInitializer] Failed to clone template to '{targetPath}'.");
            }

            T cloned = AssetDatabase.LoadAssetAtPath<T>(targetPath);
            if (cloned == null)
            {
                throw new InvalidOperationException($"[SettingProjectInitializer] Failed to load cloned asset '{targetPath}'.");
            }

            cloned.name = template.name;
            EditorUtility.SetDirty(cloned);
            return cloned;
        }

        private static SettingSyncRecord BuildSyncRecord(ScriptableObject template, ScriptableObject localAsset)
        {
            string snapshotJson = EditorJsonUtility.ToJson(template, true);
            return new SettingSyncRecord
            {
                settingId = GetSettingId(template),
                settingTypeName = template.GetType().AssemblyQualifiedName,
                localAsset = localAsset,
                lastTemplateVersion = 1,
                lastTemplateHash = snapshotJson.GetHashCode().ToString(),
                lastSnapshotJson = snapshotJson,
                lastSyncTimeUtc = DateTime.UtcNow.ToString("O"),
                migrationNotes = string.Empty
            };
        }

        private static string GetSettingId(ScriptableObject setting)
        {
            return setting switch
            {
                BaseSetting baseSetting => baseSetting.SettingId,
                ComponentSetting componentSetting => componentSetting.SettingId,
                _ => setting.GetType().FullName
            };
        }

        public static string GetProjectAssetPath(ScriptableObject setting)
        {
            string fileName = GetAssetFileName(setting);
            return setting switch
            {
                BaseSetting => SettingProjectPaths.GetBaseSettingAssetPath(fileName),
                ComponentSetting => SettingProjectPaths.GetComponentSettingAssetPath(fileName),
                _ => throw new InvalidOperationException($"[SettingProjectInitializer] Unsupported setting type '{setting.GetType().FullName}'.")
            };
        }

        private static string GetAssetFileName(ScriptableObject setting)
        {
            string assetPath = AssetDatabase.GetAssetPath(setting);
            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            return string.IsNullOrWhiteSpace(fileName) ? setting.GetType().Name : fileName;
        }

        private static T LoadOrCreateAsset<T>(string assetPath, string assetName) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void EnsureProjectFoldersExist()
        {
            EnsureFolder("Assets", "Game");
            EnsureFolder("Assets/Game", "Settings");
            EnsureFolder("Assets/Game", "Resources");
            EnsureFolder(SettingProjectPaths.Root, "Base");
            EnsureFolder(SettingProjectPaths.Root, "Components");
            EnsureFolder(SettingProjectPaths.Root, "Sync");
            EnsureFolder(SettingProjectPaths.SyncFolder, "Snapshots");
        }

        private static void EnsureFolder(string parentFolder, string folderName)
        {
            string folderPath = $"{parentFolder}/{folderName}";
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }
}
