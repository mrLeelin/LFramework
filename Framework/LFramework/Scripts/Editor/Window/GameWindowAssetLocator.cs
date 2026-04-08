using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Window
{
    /// <summary>
    /// 为 GameWindow 提供稳定的资源筛选与显示名，避免工程 Setting 与模板 Setting 同名冲突。
    /// </summary>
    public static class GameWindowAssetLocator
    {
        public static List<T> GetPreferredAssetsAtType<T>() where T : ScriptableObject
        {
            List<T> allAssets = AssetUtilities.GetAllAssetsOfType<T>()
                .Where(asset => asset != null)
                .ToList();

            string preferredRoot = GetPreferredRootForType(typeof(T));
            if (string.IsNullOrEmpty(preferredRoot))
            {
                return allAssets;
            }

            List<T> projectOwnedAssets = allAssets
                .Where(asset => IsAssetUnderRoot(asset, preferredRoot))
                .ToList();

            return projectOwnedAssets.Count > 0 ? projectOwnedAssets : allAssets;
        }

        public static string GetMenuItemName<T>(T asset, IReadOnlyCollection<T> allAssets) where T : ScriptableObject
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            int duplicateCount = allAssets.Count(other =>
            {
                string otherPath = AssetDatabase.GetAssetPath(other);
                return Path.GetFileNameWithoutExtension(otherPath) == baseName;
            });

            if (duplicateCount <= 1)
            {
                return baseName;
            }

            string folderName = Path.GetFileName(Path.GetDirectoryName(assetPath)?.Replace('\\', '/'));
            return $"{baseName} ({folderName})";
        }

        private static string GetPreferredRootForType(System.Type type)
        {
            if (typeof(ComponentSetting).IsAssignableFrom(type))
            {
                return SettingProjectPaths.ComponentFolder;
            }

            if (typeof(BaseSetting).IsAssignableFrom(type))
            {
                return SettingProjectPaths.BaseFolder;
            }

            return null;
        }

        private static bool IsAssetUnderRoot(Object asset, string rootPath)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset).Replace('\\', '/');
            return assetPath.StartsWith(rootPath + "/", System.StringComparison.OrdinalIgnoreCase) ||
                   assetPath.Equals(rootPath, System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
