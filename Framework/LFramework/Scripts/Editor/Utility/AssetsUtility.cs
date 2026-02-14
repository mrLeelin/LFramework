using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using UnityEditor;
using Object = UnityEngine.Object;

namespace LFramework.Editor
{
    public static class AssetsUtility
    {
        /// <summary>
        /// Get all assets for folder
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="includeSubDirectories"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static HashSet<T> GetAssetsForFolder<T>(string folderPath, bool includeSubDirectories) where T : Object
        {
            HashSet<T> assets = new HashSet<T>();
            folderPath = (folderPath ?? "").TrimEnd('/') + "/";
            if (!folderPath.ToLower().StartsWith("assets/"))
            {
                folderPath = "Assets/" + folderPath;
            }

            folderPath = folderPath.TrimEnd('/') + "/";
            IEnumerable<string> strings = ((IEnumerable<string>)AssetDatabase.GetAllAssetPaths()).Where<string>(
                (Func<string, bool>)(x =>
                {
                    if (includeSubDirectories)
                        return x.StartsWith(folderPath, StringComparison.InvariantCultureIgnoreCase);
                    return String.Compare(PathUtilities.GetDirectoryName(x).Trim('/'), folderPath.Trim('/'),
                        StringComparison.OrdinalIgnoreCase) == 0;
                }));

            foreach (string str1 in strings)
            {
                T @object = AssetDatabase.LoadAssetAtPath<T>(str1);
                if (@object == null)
                {
                    continue;
                }
                assets.Add(@object);
            }

            return assets;
        }
    }
}