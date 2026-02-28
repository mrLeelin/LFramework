using UnityEngine;
using UnityEditor;
using System.IO;
using LFramework.Runtime;
using LFramework.Runtime.Settings;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// Setting 设置助手，用于快速创建示例配置
    /// </summary>
    public static class SettingSetupHelper
    {
        private const string SettingsPath = "Assets/Framework/Framework/LFramework/Assets/Settings";

        [MenuItem("LFramework/Setup/Create Example Settings")]
        public static void CreateExampleSettings()
        {
            if (!EditorUtility.DisplayDialog("创建示例配置",
                "这将创建以下示例配置：\n\n" +
                "• SettingSelector\n" +
                "• GameSetting (Development/Staging/Production)\n" +
                "• iOSSetting (Development)\n" +
                "• AndroidSetting (Development)\n\n" +
                "是否继续？",
                "创建", "取消"))
            {
                return;
            }

            // 确保目录存在
            EnsureDirectoryExists(SettingsPath);
            EnsureDirectoryExists($"{SettingsPath}/GameSettings");
            EnsureDirectoryExists($"{SettingsPath}/iOSSettings");
            EnsureDirectoryExists($"{SettingsPath}/AndroidSettings");

            // 创建 GameSetting 示例
            var gameSettingDev = CreateGameSetting("GameSetting_Development", "Development", "http://localhost:8080", "1.0.0.0");
            var gameSettingStaging = CreateGameSetting("GameSetting_Staging", "Staging", "http://staging.example.com", "1.0.0.0");
            var gameSettingProd = CreateGameSetting("GameSetting_Production", "Production", "http://api.example.com", "1.0.0.0");

            // 创建 iOSSetting 示例
            var iosSettingDev = CreateiOSSetting("iOSSetting_Development", "com.company.game.dev", "12.0");

            // 创建 AndroidSetting 示例
            var androidSettingDev = CreateAndroidSetting("AndroidSetting_Development", "com.company.game.dev", 21, 30);

            // 创建 SettingSelector
            var selector = CreateSettingSelector(gameSettingDev, iosSettingDev, androidSettingDev);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("创建完成",
                "示例配置已创建完成！\n\n" +
                $"配置位置：{SettingsPath}\n\n" +
                "请在 LFramework/GameSetting 窗口中查看和编辑配置。",
                "确定");

            // 选中 SettingSelector
            Selection.activeObject = selector;
            EditorGUIUtility.PingObject(selector);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static GameSetting CreateGameSetting(string fileName, string channel, string ip, string appVersion)
        {
            var path = $"{SettingsPath}/GameSettings/{fileName}.asset";

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<GameSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[SettingSetupHelper] {fileName} 已存在，跳过创建");
                return existing;
            }

            var setting = ScriptableObject.CreateInstance<GameSetting>();
            setting.name = fileName;

            // 设置默认值
            setting.isRelease = channel == "Production";
            setting.isResourcesBuildIn = false;
            setting.versionUrl = $"{ip}/version";
            setting.ip = ip;
            setting.webSocketIp = ip.Replace("http://", "ws://").Replace("https://", "wss://") + "/ws";
            setting.appVersion = appVersion;
            setting.resourceVersion = "1.0.0.0";
            setting.cdnType = CdnType.DaBaoji;
            setting.channel = channel;
            setting.cdnUrl = $"{ip}/cdn";

            AssetDatabase.CreateAsset(setting, path);
            Debug.Log($"[SettingSetupHelper] 创建 {fileName} 成功");

            return setting;
        }

        private static iOSSetting CreateiOSSetting(string fileName, string bundleId, string targetOS)
        {
            var path = $"{SettingsPath}/iOSSettings/{fileName}.asset";

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<iOSSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[SettingSetupHelper] {fileName} 已存在，跳过创建");
                return existing;
            }

            var setting = ScriptableObject.CreateInstance<iOSSetting>();
            setting.name = fileName;

            // 使用反射设置私有字段
            var type = typeof(iOSSetting);
            type.GetField("bundleIdentifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, bundleId);
            type.GetField("targetOSVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, targetOS);
            type.GetField("requiresFullScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, true);

            AssetDatabase.CreateAsset(setting, path);
            Debug.Log($"[SettingSetupHelper] 创建 {fileName} 成功");

            return setting;
        }

        private static AndroidSetting CreateAndroidSetting(string fileName, string bundleId, int minSdk, int targetSdk)
        {
            var path = $"{SettingsPath}/AndroidSettings/{fileName}.asset";

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<AndroidSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[SettingSetupHelper] {fileName} 已存在，跳过创建");
                return existing;
            }

            var setting = ScriptableObject.CreateInstance<AndroidSetting>();
            setting.name = fileName;

            // 使用反射设置私有字段
            var type = typeof(AndroidSetting);
            type.GetField("bundleIdentifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, bundleId);
            type.GetField("minSdkVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, minSdk);
            type.GetField("targetSdkVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, targetSdk);
            type.GetField("useIL2CPP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, true);

            AssetDatabase.CreateAsset(setting, path);
            Debug.Log($"[SettingSetupHelper] 创建 {fileName} 成功");

            return setting;
        }

        private static SettingSelector CreateSettingSelector(GameSetting gameSetting, iOSSetting iosSetting, AndroidSetting androidSetting)
        {
            var path = $"{SettingsPath}/SettingSelector.asset";

            // 检查是否已存在
            var existing = AssetDatabase.LoadAssetAtPath<SettingSelector>(path);
            if (existing != null)
            {
                Debug.Log("[SettingSetupHelper] SettingSelector 已存在，更新配置");
                existing.SetSetting(gameSetting);
                existing.SetSetting(iosSetting);
                existing.SetSetting(androidSetting);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var selector = ScriptableObject.CreateInstance<SettingSelector>();
            selector.name = "SettingSelector";

            // 设置默认选择
            selector.SetSetting(gameSetting);
            selector.SetSetting(iosSetting);
            selector.SetSetting(androidSetting);

            AssetDatabase.CreateAsset(selector, path);
            Debug.Log("[SettingSetupHelper] 创建 SettingSelector 成功");

            return selector;
        }

        [MenuItem("LFramework/Setup/Validate All Settings")]
        public static void ValidateAllSettings()
        {
            var selector = SettingManager.GetSelector();
            if (selector == null)
            {
                EditorUtility.DisplayDialog("验证失败", "未找到 SettingSelector！", "确定");
                return;
            }

            var allSettings = selector.GetAllSettings();
            bool allValid = true;
            int validCount = 0;
            int invalidCount = 0;

            foreach (var setting in allSettings)
            {
                if (setting == null)
                {
                    invalidCount++;
                    allValid = false;
                    continue;
                }

                if (setting.Validate(out string errorMessage))
                {
                    validCount++;
                }
                else
                {
                    Debug.LogError($"[SettingSetupHelper] {setting.GetType().Name}: {setting.name} - {errorMessage}");
                    invalidCount++;
                    allValid = false;
                }
            }

            string message = $"验证完成！\n\n✓ 通过: {validCount}\n✗ 失败: {invalidCount}";
            EditorUtility.DisplayDialog("验证结果", message, "确定");
        }
    }
}
