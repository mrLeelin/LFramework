using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
#endif

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// GameSetting 统一访问提供者
    /// 通过 BaseComponentSetting 获取 GameSetting，确保配置统一
    /// </summary>
    public static class GameSettingProvider
    {
        private static GameSetting _cachedGameSetting;
        private static BaseComponentSetting _cachedBaseComponentSetting;

        /// <summary>
        /// 获取 GameSetting 实例
        /// 编辑器模式：从 BaseComponentSetting 中获取
        /// 运行时模式：从 DI 容器中获取
        /// </summary>
        public static GameSetting GetGameSetting()
        {
#if UNITY_EDITOR
            if (_cachedGameSetting == null)
            {
                // 先查找 BaseComponentSetting
                if (_cachedBaseComponentSetting == null)
                {
                    var allBaseSettings = AssetUtilities.GetAllAssetsOfType<BaseComponentSetting>();
                    _cachedBaseComponentSetting = allBaseSettings.FirstOrDefault();

                    if (_cachedBaseComponentSetting == null)
                    {
                        Debug.LogError("[GameSettingProvider] BaseComponentSetting not found in project!");
                        return null;
                    }
                }

                // 从 BaseComponentSetting 获取 GameSetting
                _cachedGameSetting = _cachedBaseComponentSetting.GameSetting;

                if (_cachedGameSetting == null)
                {
                    Debug.LogError("[GameSettingProvider] GameSetting is null in BaseComponentSetting! Please assign it in the inspector.");
                }
            }
            return _cachedGameSetting;
#else
            // 运行时模式：从 DI 容器获取
            if (LFrameworkAspect.Instance == null)
            {
                Debug.LogError("[GameSettingProvider] LFrameworkAspect not initialized!");
                return null;
            }

            return LFrameworkAspect.Instance.Get<GameSetting>();
#endif
        }

        /// <summary>
        /// 尝试获取 GameSetting（不输出错误日志）
        /// </summary>
        public static bool TryGetGameSetting(out GameSetting gameSetting)
        {
            gameSetting = null;

#if UNITY_EDITOR
            if (_cachedGameSetting == null)
            {
                // 先查找 BaseComponentSetting
                if (_cachedBaseComponentSetting == null)
                {
                    var allBaseSettings = AssetUtilities.GetAllAssetsOfType<BaseComponentSetting>();
                    _cachedBaseComponentSetting = allBaseSettings.FirstOrDefault();
                }

                if (_cachedBaseComponentSetting != null)
                {
                    _cachedGameSetting = _cachedBaseComponentSetting.GameSetting;
                }
            }
            gameSetting = _cachedGameSetting;
            return gameSetting != null;
#else
            // 运行时模式：从 DI 容器获取
            if (LFrameworkAspect.Instance == null)
            {
                return false;
            }

            gameSetting = LFrameworkAspect.Instance.Get<GameSetting>();
            return gameSetting != null;
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// 清除编辑器缓存（脚本重新编译时自动调用）
        /// </summary>
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            _cachedGameSetting = null;
            _cachedBaseComponentSetting = null;
        }
#endif
    }
}
