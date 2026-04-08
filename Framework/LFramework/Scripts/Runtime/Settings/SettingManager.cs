using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Setting 管理器，提供统一的配置访问接口
    /// </summary>
    public static class SettingManager
    {
        private const string ProjectSelectorResourceName = "ProjectSettingSelector";
        private const string LegacySelectorResourceName = "SettingSelector";

        private static ProjectSettingSelector _projectSelector;
        private static SettingSelector _selector;

        /// <summary>
        /// 获取工程侧 ProjectSettingSelector 实例
        /// </summary>
        public static ProjectSettingSelector GetProjectSelector()
        {
            if (_projectSelector == null)
            {
#if UNITY_EDITOR
                _projectSelector = LoadFirstAssetOfType<ProjectSettingSelector>(false);

                if (_projectSelector == null)
                {
                    Log.Warning("[SettingManager] ProjectSettingSelector not found in project.");
                }
#else
                _projectSelector = Resources.Load<ProjectSettingSelector>(ProjectSelectorResourceName);
                if (_projectSelector == null)
                {
                    Log.Warning("[SettingManager] ProjectSettingSelector not found in Resources!");
                }
#endif
            }

            return _projectSelector;
        }

        /// <summary>
        /// 获取 SettingSelector 实例
        /// </summary>
        public static SettingSelector GetSelector()
        {
            if (_selector == null)
            {
#if UNITY_EDITOR
                _selector = LoadFirstAssetOfType<SettingSelector>(false);

                if (_selector == null)
                {
                    Log.Error("[SettingManager] No SettingSelector found in project!");
                }
#else
                _selector = Resources.Load<SettingSelector>(LegacySelectorResourceName);
                if (_selector == null)
                {
                    Log.Error("[SettingManager] SettingSelector not found in Resources!");
                }
#endif
            }

            return _selector;
        }

        /// <summary>
        /// 获取指定类型的 Setting
        /// </summary>
        public static T GetSetting<T>() where T : BaseSetting
        {
            var projectSelector = GetProjectSelector();
            if (projectSelector != null)
            {
                return projectSelector.GetSetting<T>();
            }

            var legacySelector = GetSelector();
            return legacySelector?.GetSetting<T>();
        }

        /// <summary>
        /// 获取所有指定类型的 Setting 实例（用于编辑器）
        /// </summary>
        public static List<T> GetAllSettingsOfType<T>() where T : BaseSetting
        {
            var projectSelector = GetProjectSelector();
            if (projectSelector != null)
            {
                return projectSelector.GetAllSettings().OfType<T>().ToList();
            }

#if UNITY_EDITOR
            return LoadAllAssetsOfType<T>().ToList();
#else
            return Resources.LoadAll<T>("").ToList();
#endif
        }

#if UNITY_EDITOR
        private static T LoadFirstAssetOfType<T>(bool includeDerivedTypes = true) where T : UnityEngine.Object
        {
            return LoadAllAssetsOfType<T>(includeDerivedTypes).FirstOrDefault();
        }

        private static List<T> LoadAllAssetsOfType<T>(bool includeDerivedTypes = true) where T : UnityEngine.Object
        {
            string filter = $"t:{typeof(T).Name}";
            string[] guids = AssetDatabase.FindAssets(filter);
            var results = new List<T>(guids.Length);
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null && (includeDerivedTypes || asset.GetType() == typeof(T)))
                {
                    results.Add(asset);
                }
            }

            return results;
        }

        /// <summary>
        /// 清除缓存（编辑器脚本重新编译时调用）
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            _projectSelector = null;
            _selector = null;
        }

        /// <summary>
        /// 测试辅助：清除缓存
        /// </summary>
        public static void ClearCacheForTests()
        {
            _projectSelector = null;
            _selector = null;
        }
#else
        public static void ClearCacheForTests()
        {
            _projectSelector = null;
            _selector = null;
        }
#endif
    }
}
