using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#if HybridCLR_SUPPORT
using HybridCLR.Editor.Settings;
#endif

using LFramework.Editor.Builder.Pipeline;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

#if ADDRESSABLE_SUPPORT
using UnityEditor.AddressableAssets;
using LFramework.Editor.Builder.BuildingResource;
#endif

namespace LFramework.Editor.Builder
{
    internal static class BuildEnvironmentConfigurator
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            EditorApplication.delayCall += EnsureProjectSettingsSynchronized;
        }

        public static void Prepare(BuildPipelineContext context)
        {
            EnsureProjectSettingsSynchronized();

            if (context?.BuildTarget == BuildTarget.Android)
            {
                EnsurePreferredAndroidToolRoots();
                NormalizeAndroidTargetSdk();
            }
        }

        private static void EnsureProjectSettingsSynchronized()
        {
#if HybridCLR_SUPPORT
            var runtimeSetting = SettingManager.GetSetting<HybridCLRSetting>();
            if (runtimeSetting == null)
            {
                return;
            }

            var hotUpdateAssemblyNames = ResolveHotUpdateAssemblyNames(runtimeSetting);
            SyncRuntimeSetting(runtimeSetting, hotUpdateAssemblyNames);
            SyncHybridClrSettingsAsset(hotUpdateAssemblyNames);
#endif

#if ADDRESSABLE_SUPPORT
            var addressableSettings = AddressableAssetSettingsDefaultObject.Settings;
            if (addressableSettings != null)
            {
                AddressableBuildHelper.EnsurePlayerDataBuilder(addressableSettings);
            }
#endif

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {
                EnsurePreferredAndroidToolRoots();
                EnsurePreferredAndroidSdkRoot();
                if (!IsUnity6000OrNewer())
                {
                    NormalizeAndroidTargetSdk();
                }
            }
        }

#if HybridCLR_SUPPORT
        private static string[] ResolveHotUpdateAssemblyNames(HybridCLRSetting runtimeSetting)
        {
            var sourceAssemblies = runtimeSetting.hotfixAssembliesSort ?? new List<string>();
            var results = new List<string>();

            if (!string.IsNullOrWhiteSpace(runtimeSetting.logicMainDllName))
            {
                results.Add(runtimeSetting.logicMainDllName);
            }

            results.AddRange(sourceAssemblies);

            return results
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .Where(AssemblyDefinitionExists)
                .ToArray();
        }

        private static void SyncRuntimeSetting(HybridCLRSetting runtimeSetting, string[] hotUpdateAssemblyNames)
        {
            var current = runtimeSetting.hotfixAssembliesSort ?? new List<string>();
            if (current.SequenceEqual(hotUpdateAssemblyNames, StringComparer.Ordinal))
            {
                return;
            }

            runtimeSetting.hotfixAssembliesSort = hotUpdateAssemblyNames.ToList();
            EditorUtility.SetDirty(runtimeSetting);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[BuildEnvironmentConfigurator] Updated runtime HybridCLR setting assemblies: {string.Join(", ", hotUpdateAssemblyNames)}");
        }

        private static void SyncHybridClrSettingsAsset(string[] hotUpdateAssemblyNames)
        {
            var asmdefAssets = hotUpdateAssemblyNames
                .Select(FindAssemblyDefinitionAsset)
                .Where(asset => asset != null)
                .ToArray();

            var settings = HybridCLRSettings.Instance;
            bool changed = false;

            changed |= UpdateStringArray(ref settings.hotUpdateAssemblies, Array.Empty<string>());
            changed |= UpdateAsmdefArray(ref settings.hotUpdateAssemblyDefinitions, asmdefAssets);

            if (!changed)
            {
                return;
            }

            HybridCLRSettings.Save();
            Debug.Log(
                $"[BuildEnvironmentConfigurator] Synchronized ProjectSettings/HybridCLRSettings.asset with assemblies: {string.Join(", ", hotUpdateAssemblyNames)}");
        }

        private static bool AssemblyDefinitionExists(string assemblyName)
        {
            return FindAssemblyDefinitionAsset(assemblyName) != null;
        }

        private static AssemblyDefinitionAsset FindAssemblyDefinitionAsset(string assemblyName)
        {
            string[] guids = AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.Equals(Path.GetFileNameWithoutExtension(path), assemblyName, StringComparison.Ordinal))
                {
                    return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(path);
                }
            }

            return null;
        }

        private static bool UpdateStringArray(ref string[] currentValue, string[] newValue)
        {
            currentValue ??= Array.Empty<string>();
            newValue ??= Array.Empty<string>();

            if (currentValue.SequenceEqual(newValue, StringComparer.Ordinal))
            {
                return false;
            }

            currentValue = newValue;
            return true;
        }

        private static bool UpdateAsmdefArray(ref AssemblyDefinitionAsset[] currentValue,
            AssemblyDefinitionAsset[] newValue)
        {
            currentValue ??= Array.Empty<AssemblyDefinitionAsset>();
            newValue ??= Array.Empty<AssemblyDefinitionAsset>();

            if (currentValue.SequenceEqual(newValue))
            {
                return false;
            }

            currentValue = newValue;
            return true;
        }
#endif

        private static void NormalizeAndroidTargetSdk()
        {
            int? installedApiLevel = GetHighestInstalledStableAndroidApiLevel();
            if (!installedApiLevel.HasValue)
            {
                Debug.LogWarning(
                "[BuildEnvironmentConfigurator] No stable Android SDK platform was found. Keeping the current target SDK.");
                return;
            }

            Type androidSettingsType = typeof(PlayerSettings).GetNestedType("Android", BindingFlags.Public);
            PropertyInfo targetSdkProperty = androidSettingsType?.GetProperty("targetSdkVersion",
                BindingFlags.Public | BindingFlags.Static);

            if (targetSdkProperty == null)
            {
                Debug.LogWarning(
                    "[BuildEnvironmentConfigurator] Failed to locate PlayerSettings.Android.targetSdkVersion.");
                return;
            }

            string enumName = $"AndroidApiLevel{installedApiLevel.Value}";
            if (!Enum.GetNames(targetSdkProperty.PropertyType).Contains(enumName))
            {
                Debug.LogWarning(
                    $"[BuildEnvironmentConfigurator] Target SDK enum '{enumName}' is not available in this Unity version.");
                return;
            }

            object newValue = Enum.Parse(targetSdkProperty.PropertyType, enumName);
            object currentValue = targetSdkProperty.GetValue(null, null);
            if (Equals(currentValue, newValue))
            {
                return;
            }

            targetSdkProperty.SetValue(null, newValue, null);
            Debug.Log(
                $"[BuildEnvironmentConfigurator] Android target SDK set to API {installedApiLevel.Value} to match the installed SDK platforms.");
        }

        private static int? GetHighestInstalledStableAndroidApiLevel()
        {
            string sdkRoot = GetAndroidSdkRoot();
            if (string.IsNullOrWhiteSpace(sdkRoot))
            {
                return null;
            }

            string platformsDir = Path.Combine(sdkRoot, "platforms");
            if (!Directory.Exists(platformsDir))
            {
                return null;
            }

            int maxApiLevel = -1;
            foreach (string directory in Directory.GetDirectories(platformsDir, "android-*"))
            {
                string folderName = Path.GetFileName(directory);
                string suffix = folderName.Substring("android-".Length);
                if (!suffix.All(char.IsDigit))
                {
                    continue;
                }

                if (int.TryParse(suffix, out int apiLevel))
                {
                    maxApiLevel = Math.Max(maxApiLevel, apiLevel);
                }
            }

            return maxApiLevel > 0 ? maxApiLevel : null;
        }

        private static string GetAndroidSdkRoot()
        {
            string preferredSdk = GetPreferredAndroidSdkRoot();
            if (!string.IsNullOrWhiteSpace(preferredSdk))
            {
                return preferredSdk;
            }

            string sdkRoot = TryGetAndroidSdkRootFromUnity();
            if (!string.IsNullOrWhiteSpace(sdkRoot) && Directory.Exists(sdkRoot))
            {
                return sdkRoot;
            }

            string[] candidates =
            {
                Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
                Environment.GetEnvironmentVariable("ANDROID_HOME"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk")
            };

            return candidates.FirstOrDefault(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path));
        }

        private static string TryGetAndroidSdkRootFromUnity()
        {
            Type externalToolsType = GetAndroidExternalToolsType();
            PropertyInfo sdkRootProperty = externalToolsType?.GetProperty("sdkRootPath",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            return sdkRootProperty?.GetValue(null, null) as string;
        }

        private static void EnsurePreferredAndroidSdkRoot()
        {
            string preferredSdk = GetPreferredAndroidSdkRoot();
            if (string.IsNullOrWhiteSpace(preferredSdk))
            {
                return;
            }

            Type externalToolsType = GetAndroidExternalToolsType();
            PropertyInfo sdkRootProperty = externalToolsType?.GetProperty("sdkRootPath",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (sdkRootProperty == null)
            {
                return;
            }

            string currentSdk = sdkRootProperty.GetValue(null, null) as string;
            if (string.Equals(currentSdk, preferredSdk, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            sdkRootProperty.SetValue(null, preferredSdk, null);
            Debug.Log($"[BuildEnvironmentConfigurator] Android SDK root switched to: {preferredSdk}");
        }

        private static void EnsurePreferredAndroidToolRoots()
        {
            Type externalToolsType = GetAndroidExternalToolsType();
            if (externalToolsType == null)
            {
                return;
            }

            SetAndroidToolPath(externalToolsType, "sdkRootPath", GetPreferredAndroidSdkRoot(), "Android SDK");
            SetAndroidToolPath(externalToolsType, "ndkRootPath", GetPreferredAndroidNdkRoot(), "Android NDK");
            SetAndroidToolPath(externalToolsType, "jdkRootPath", GetPreferredAndroidJdkRoot(), "Android JDK");
        }

        private static Type GetAndroidExternalToolsType()
        {
            return Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, UnityEditor")
                   ?? Type.GetType("UnityEditor.Android.AndroidExternalToolsSettings, Unity.Android.Extensions");
        }

        private static void SetAndroidToolPath(Type externalToolsType, string propertyName, string preferredPath,
            string displayName)
        {
            if (string.IsNullOrWhiteSpace(preferredPath) || !Directory.Exists(preferredPath))
            {
                return;
            }

            PropertyInfo property = externalToolsType.GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (property == null)
            {
                return;
            }

            string currentPath = property.GetValue(null, null) as string;
            if (string.Equals(currentPath, preferredPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            property.SetValue(null, preferredPath, null);
            Debug.Log($"[BuildEnvironmentConfigurator] {displayName} root switched to: {preferredPath}");
        }

        private static string GetPreferredAndroidSdkRoot()
        {
            string[] candidates =
            {
                Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT"),
                Environment.GetEnvironmentVariable("ANDROID_HOME"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk"),
                Path.Combine(GetUnity2022AndroidPlayerRoot(), "SDK"),
                Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "SDK")
            };

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                .OrderByDescending(GetAndroidSdkScore)
                .FirstOrDefault();
        }

        private static string GetPreferredAndroidNdkRoot()
        {
            string localSdk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Android", "Sdk");
            string ndkParent = Path.Combine(localSdk, "ndk");
            IEnumerable<string> localNdks = Directory.Exists(ndkParent)
                ? Directory.GetDirectories(ndkParent)
                : Array.Empty<string>();

            string[] candidates =
            {
                Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT"),
                Environment.GetEnvironmentVariable("NDK_ROOT"),
                Environment.GetEnvironmentVariable("ANDROID_NDK_HOME"),
                Path.Combine(GetUnity2022AndroidPlayerRoot(), "NDK"),
                Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "NDK")
            };

            return localNdks
                .Concat(candidates)
                .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                .OrderByDescending(GetAndroidNdkScore)
                .FirstOrDefault();
        }

        private static string GetPreferredAndroidJdkRoot()
        {
            string[] candidates =
            {
                Environment.GetEnvironmentVariable("JAVA_HOME"),
                Path.Combine(GetUnity2022AndroidPlayerRoot(), "OpenJDK"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Android Studio", "jbr"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Android Studio", "jre"),
                Path.Combine(EditorApplication.applicationContentsPath, "PlaybackEngines", "AndroidPlayer", "OpenJDK")
            };

            return candidates
                .Where(path => !string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                .OrderByDescending(GetJavaHomeScore)
                .FirstOrDefault();
        }

        private static int GetAndroidNdkScore(string ndkRoot)
        {
            string sourceProperties = Path.Combine(ndkRoot, "source.properties");
            if (!File.Exists(sourceProperties))
            {
                return 0;
            }

            string versionLine = File.ReadLines(sourceProperties)
                .FirstOrDefault(line => line.StartsWith("Pkg.Revision", StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrWhiteSpace(versionLine))
            {
                return 1;
            }

            string versionText = versionLine.Split('=').LastOrDefault()?.Trim();
            Version.TryParse(versionText, out Version version);
            return version?.Major ?? 1;
        }

        private static int GetJavaHomeScore(string jdkRoot)
        {
            string javaExe = Path.Combine(jdkRoot, "bin", Application.platform == RuntimePlatform.WindowsEditor ? "java.exe" : "java");
            return File.Exists(javaExe) ? 1 : 0;
        }

        private static string GetUnity2022AndroidPlayerRoot()
        {
            string editorRoot = Directory.GetParent(EditorApplication.applicationContentsPath)?.FullName;
            if (string.IsNullOrWhiteSpace(editorRoot))
            {
                return string.Empty;
            }

            string currentVersion = new DirectoryInfo(editorRoot).Name;
            string legacyEditorRoot = editorRoot.Replace(currentVersion, "2022.3.62f1");
            return Path.Combine(legacyEditorRoot, "Data", "PlaybackEngines", "AndroidPlayer");
        }

        private static int GetAndroidSdkScore(string sdkRoot)
        {
            int score = 0;
            string platformsDir = Path.Combine(sdkRoot, "platforms");
            if (!Directory.Exists(platformsDir))
            {
                return score;
            }

            if (Directory.Exists(Path.Combine(platformsDir, "android-35-ext15")))
            {
                score += 1000;
            }

            foreach (string directory in Directory.GetDirectories(platformsDir, "android-*"))
            {
                string folderName = Path.GetFileName(directory);
                string suffix = folderName.Substring("android-".Length);
                string numericPart = new string(suffix.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(numericPart, out int apiLevel))
                {
                    score = Math.Max(score, apiLevel);
                }
            }

            return score;
        }

        private static bool IsUnity6000OrNewer()
        {
            string version = Application.unityVersion ?? string.Empty;
            string majorPart = new string(version.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(majorPart, out int major) && major >= 6000;
        }
    }
}
