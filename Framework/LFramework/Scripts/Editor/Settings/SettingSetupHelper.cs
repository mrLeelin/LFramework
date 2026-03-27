using System;
using System.IO;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// Creates example settings assets for LFramework.
    /// </summary>
    public static class SettingSetupHelper
    {
        private const string SettingSetupHelperScriptGuid = "ba7c8ec35e9a0cc4fbcd7a7b13ddd441";

#if AUTO_CREATE_SETTING
        [MenuItem("LFramework/Setup/Create Example Settings")]
#endif
        public static void CreateExampleSettings()
        {
            string settingsPath = GetSettingsPath();

            if (!EditorUtility.DisplayDialog(
                    "Create Example Settings",
                    "This creates example GameSetting, iOSSetting, AndroidSetting, and SettingSelector assets.\n\nContinue?",
                    "Create",
                    "Cancel"))
            {
                return;
            }

            EnsureDirectoryExists(settingsPath);
            EnsureDirectoryExists($"{settingsPath}/GameSettings");
            EnsureDirectoryExists($"{settingsPath}/iOSSettings");
            EnsureDirectoryExists($"{settingsPath}/AndroidSettings");

            GameSetting gameSettingDev = CreateGameSetting("GameSetting_Development", "Development", "http://localhost:8080", "1.0.0.0");
            CreateGameSetting("GameSetting_Staging", "Staging", "http://staging.example.com", "1.0.0.0");
            CreateGameSetting("GameSetting_Production", "Production", "http://api.example.com", "1.0.0.0");

            iOSSetting iosSettingDev = CreateiOSSetting("iOSSetting_Development", "com.company.game.dev", "12.0");
            AndroidSetting androidSettingDev = CreateAndroidSetting("AndroidSetting_Development", "com.company.game.dev", 21, 30);
            SettingSelector selector = CreateSettingSelector(gameSettingDev, iosSettingDev, androidSettingDev);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Create Example Settings",
                $"Example settings created at:\n{settingsPath}",
                "OK");

            Selection.activeObject = selector;
            EditorGUIUtility.PingObject(selector);
        }

        internal static string GetSettingsPath()
        {
            string scriptAssetPath = AssetDatabase.GUIDToAssetPath(SettingSetupHelperScriptGuid);
            if (string.IsNullOrEmpty(scriptAssetPath))
            {
                throw new InvalidOperationException("[SettingSetupHelper] Failed to resolve SettingSetupHelper.cs asset path.");
            }

            string projectRoot = GetProjectRootPath();
            string scriptFullPath = Path.GetFullPath(Path.Combine(projectRoot, scriptAssetPath));
            string scriptDirectory = Path.GetDirectoryName(scriptFullPath);
            if (string.IsNullOrEmpty(scriptDirectory))
            {
                throw new InvalidOperationException("[SettingSetupHelper] Failed to resolve SettingSetupHelper.cs directory.");
            }

            string settingsFullPath = Path.GetFullPath(Path.Combine(scriptDirectory, "../../../Assets/Settings"));
            return NormalizeAssetPath(Path.GetRelativePath(projectRoot, settingsFullPath));
        }

        private static void EnsureDirectoryExists(string assetPath)
        {
            string fullPath = Path.GetFullPath(Path.Combine(GetProjectRootPath(), assetPath));
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }

        private static GameSetting CreateGameSetting(string fileName, string channel, string ip, string appVersion)
        {
            string path = $"{GetSettingsPath()}/GameSettings/{fileName}.asset";
            GameSetting existing = AssetDatabase.LoadAssetAtPath<GameSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[SettingSetupHelper] {fileName} already exists. Skipping.");
                return existing;
            }

            GameSetting setting = ScriptableObject.CreateInstance<GameSetting>();
            setting.name = fileName;
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
            Debug.Log($"[SettingSetupHelper] Created {fileName}.");
            return setting;
        }

        private static iOSSetting CreateiOSSetting(string fileName, string bundleId, string targetOS)
        {
            string path = $"{GetSettingsPath()}/iOSSettings/{fileName}.asset";
            iOSSetting existing = AssetDatabase.LoadAssetAtPath<iOSSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[SettingSetupHelper] {fileName} already exists. Skipping.");
                return existing;
            }

            iOSSetting setting = ScriptableObject.CreateInstance<iOSSetting>();
            setting.name = fileName;

            Type type = typeof(iOSSetting);
            type.GetField("bundleIdentifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, bundleId);
            type.GetField("targetOSVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, targetOS);
            type.GetField("requiresFullScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, true);

            AssetDatabase.CreateAsset(setting, path);
            Debug.Log($"[SettingSetupHelper] Created {fileName}.");
            return setting;
        }

        private static AndroidSetting CreateAndroidSetting(string fileName, string bundleId, int minSdk, int targetSdk)
        {
            string path = $"{GetSettingsPath()}/AndroidSettings/{fileName}.asset";
            AndroidSetting existing = AssetDatabase.LoadAssetAtPath<AndroidSetting>(path);
            if (existing != null)
            {
                Debug.Log($"[SettingSetupHelper] {fileName} already exists. Skipping.");
                return existing;
            }

            AndroidSetting setting = ScriptableObject.CreateInstance<AndroidSetting>();
            setting.name = fileName;

            Type type = typeof(AndroidSetting);
            type.GetField("bundleIdentifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, bundleId);
            type.GetField("minSdkVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, minSdk);
            type.GetField("targetSdkVersion", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, targetSdk);
            type.GetField("useIL2CPP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(setting, true);

            AssetDatabase.CreateAsset(setting, path);
            Debug.Log($"[SettingSetupHelper] Created {fileName}.");
            return setting;
        }

        private static SettingSelector CreateSettingSelector(GameSetting gameSetting, iOSSetting iosSetting, AndroidSetting androidSetting)
        {
            string path = $"{GetSettingsPath()}/SettingSelector.asset";
            SettingSelector existing = AssetDatabase.LoadAssetAtPath<SettingSelector>(path);
            if (existing != null)
            {
                Debug.Log("[SettingSetupHelper] SettingSelector already exists. Updating values.");
                existing.SetSetting(gameSetting);
                existing.SetSetting(iosSetting);
                existing.SetSetting(androidSetting);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            SettingSelector selector = ScriptableObject.CreateInstance<SettingSelector>();
            selector.name = "SettingSelector";
            selector.SetSetting(gameSetting);
            selector.SetSetting(iosSetting);
            selector.SetSetting(androidSetting);

            AssetDatabase.CreateAsset(selector, path);
            Debug.Log("[SettingSetupHelper] Created SettingSelector.");
            return selector;
        }

#if AUTO_CREATE_SETTING
        [MenuItem("LFramework/Setup/Validate All Settings")]
#endif
        public static void ValidateAllSettings()
        {
            SettingSelector selector = SettingManager.GetSelector();
            if (selector == null)
            {
                EditorUtility.DisplayDialog("Validation Failed", "SettingSelector was not found.", "OK");
                return;
            }

            var allSettings = selector.GetAllSettings();
            bool allValid = true;
            int validCount = 0;
            int invalidCount = 0;

            foreach (BaseSetting setting in allSettings)
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

            string message = $"Validation finished.\n\nPassed: {validCount}\nFailed: {invalidCount}";
            string title = allValid ? "Validation Passed" : "Validation Finished";
            EditorUtility.DisplayDialog(title, message, "OK");
        }

        private static string GetProjectRootPath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return assetPath.Replace('\\', '/');
        }
    }
}
