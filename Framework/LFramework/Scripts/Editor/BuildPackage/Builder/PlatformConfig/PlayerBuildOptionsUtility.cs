using System;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Builder.PlatformConfig
{
    /// <summary>
    /// 选择 Dev 或 Release 配置并应用到平台构建选项。
    /// </summary>
    internal static class PlayerBuildOptionsUtility
    {
        internal static BuildOptions Apply(BuildOptions requiredOptions, RuntimePlatform platform, bool isRelease)
        {
            var setting = SettingManager.GetSetting<PlayerBuildSetting>();
            if (setting == null)
            {
                throw new InvalidOperationException(
                    "PlayerBuildSetting is missing. Initialize project settings from the framework settings page, " +
                    "or create PlayerBuildSetting and register it in ProjectSettingSelector.");
            }

            var config = isRelease ? setting.GetReleaseConfig(platform) : setting.GetConfig(platform);
            var options = (BuildOptions)config.OptionsValue;
            if ((options & BuildOptions.CompressWithLz4) != 0 &&
                (options & BuildOptions.CompressWithLz4HC) != 0)
            {
                throw new InvalidOperationException("PlayerBuildSetting: CompressWithLz4 and CompressWithLz4HC are mutually exclusive.");
            }

            if (platform == RuntimePlatform.Android &&
                (options & BuildOptions.AcceptExternalModificationsToPlayer) != 0 &&
                (requiredOptions & BuildOptions.AcceptExternalModificationsToPlayer) == 0)
            {
                throw new InvalidOperationException("PlayerBuildSetting: Android export requires BuildAndroidAppType.ExportAndroidProject.");
            }

            options |= requiredOptions;

            Debug.Log($"[PlayerBuildSetting] Platform={platform}, Mode={(isRelease ? "Release" : "Dev")}, Options={options}");
            return options;
        }
    }
}
