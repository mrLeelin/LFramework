using System.IO;
using System;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Luban.Editor
{
    public partial class LubanExportConfig
    {
        private static string _cachedAssetGuid;
        private static Func<string[]> _assetLookupForTests;

        static LubanExportConfig()
        {
            EditorApplication.projectChanged += ResetCachedInstance;
        }

        public static LubanExportConfig Instance
        {
            get
            {
                if(_instance == null)
                {
                    _instance = GetOrCreate();
                }

                return _instance;
            }
        }

        private static LubanExportConfig _instance;

        public static LubanExportConfig GetOrCreate()
        {
            if (_instance != null)
            {
                return _instance;
            }

            if (TryLoadCachedAsset(out LubanExportConfig cachedAsset))
            {
                _instance = cachedAsset;
                return _instance;
            }

            var guids = FindConfigGuids();

            if(guids.Length > 1)
            {
                Debug.LogWarning("Found multiple Luban assets, using the first one");
            }

            switch(guids.Length)
            {
                case 0:
                    var setting = CreateInstance<LubanExportConfig>();

                    if(!Directory.Exists("Assets/Editor"))
                    {
                        Directory.CreateDirectory("Assets/Editor");
                    }

                    AssetDatabase.CreateAsset(setting, "Assets/Editor/LubanExportConfig.asset");
                    CacheAsset(setting, "Assets/Editor/LubanExportConfig.asset");
                    return _instance;

                default:
                    var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    _instance = AssetDatabase.LoadAssetAtPath<LubanExportConfig>(path);
                    CacheAsset(_instance, path);
                    return _instance;
            }
        }

        internal static void SetAssetLookupForTests(Func<string[]> assetLookup)
        {
            _assetLookupForTests = assetLookup;
        }

        internal static void ResetCacheForTests()
        {
            ResetCachedInstance();
        }

        private static string[] FindConfigGuids()
        {
            return _assetLookupForTests?.Invoke() ?? AssetDatabase.FindAssets("t:LubanExportConfig");
        }

        private static bool TryLoadCachedAsset(out LubanExportConfig config)
        {
            config = null;

            if (string.IsNullOrEmpty(_cachedAssetGuid))
            {
                return false;
            }

            string path = AssetDatabase.GUIDToAssetPath(_cachedAssetGuid);
            if (string.IsNullOrEmpty(path))
            {
                _cachedAssetGuid = null;
                return false;
            }

            config = AssetDatabase.LoadAssetAtPath<LubanExportConfig>(path);
            if (config == null)
            {
                _cachedAssetGuid = null;
                return false;
            }

            return true;
        }

        private static void CacheAsset(LubanExportConfig config, string assetPath)
        {
            _instance = config;
            _cachedAssetGuid = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.AssetPathToGUID(assetPath);
        }

        private static void ResetCachedInstance()
        {
            _instance = null;
            _cachedAssetGuid = null;
        }
    }

    static class LubanExportConfig_SettingsRegister
    {
        private static PropertyTree _PROPERTY;

        [SettingsProvider]
        public static SettingsProvider Create()
        {
            var provider = new SettingsProvider("Project/Luban", SettingsScope.Project)
            {
                label = "Luban",
                guiHandler = _ =>
                {
                    if(_PROPERTY is null)
                    {
                        var setting = LubanExportConfig.GetOrCreate();
                        var so      = new SerializedObject(setting);
                        _PROPERTY = PropertyTree.Create(so);
                    }

                    _PROPERTY.Draw();
                }
            };

            return provider;
        }
    }
}
