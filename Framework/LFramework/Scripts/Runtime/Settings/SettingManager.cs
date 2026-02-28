using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityGameFramework.Runtime;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Setting 管理器，提供统一的配置访问接口
    /// </summary>
    public static class SettingManager
    {
        private static SettingSelector _selector;

        /// <summary>
        /// 获取 SettingSelector 实例
        /// </summary>
        public static SettingSelector GetSelector()
        {
            if (_selector == null)
            {
#if UNITY_EDITOR
                var selectors = AssetUtilities.GetAllAssetsOfType<SettingSelector>();
                _selector = selectors.FirstOrDefault();

                if (_selector == null)
                {
                    Log.Error("[SettingManager] No SettingSelector found in project!");
                }
#else
                _selector = Resources.Load<SettingSelector>("SettingSelector");
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
            var selector = GetSelector();
            return selector?.GetSetting<T>();
        }

        /// <summary>
        /// 获取所有指定类型的 Setting 实例（用于编辑器）
        /// </summary>
        public static List<T> GetAllSettingsOfType<T>() where T : BaseSetting
        {
#if UNITY_EDITOR
            return AssetUtilities.GetAllAssetsOfType<T>().ToList();
#else
            return Resources.LoadAll<T>("").ToList();
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 清除缓存（编辑器脚本重新编译时调用）
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            _selector = null;
        }
#endif
    }
}
