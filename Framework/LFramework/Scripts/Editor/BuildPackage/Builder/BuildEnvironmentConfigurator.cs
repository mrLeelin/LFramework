using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
using HybridCLR.Editor.Settings;
#endif

using LFramework.Editor.Builder.Pipeline;
using LFramework.Editor.Builder.HybridCLR;
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

#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
            var runtimeSetting = SettingManager.GetSetting<HybridCLRSetting>();
            ValidateRuntimeSettingForBuild(runtimeSetting);
            ValidateDheBuildConfiguration(context, runtimeSetting);
#endif

            if (context?.BuildTarget == BuildTarget.Android)
            {
                EnsurePreferredAndroidToolRoots();
                NormalizeAndroidTargetSdk();
            }
        }

        private static void EnsureProjectSettingsSynchronized()
        {
#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
            var runtimeSetting = SettingManager.GetSetting<HybridCLRSetting>();
            if (runtimeSetting == null)
            {
                return;
            }

            var assemblyNames = ResolveConfiguredAssemblyNames(runtimeSetting);
            SyncRuntimeSetting(runtimeSetting, assemblyNames);
            bool useDhe = HybridCLRBuildWorkflow.IsDheEdition;
            SyncHybridClrSettingsAsset(assemblyNames, useDhe);
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

#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
        internal static void ValidateRuntimeSettingForBuild(HybridCLRSetting runtimeSetting)
        {
            if (runtimeSetting == null)
            {
                throw new InvalidOperationException(
                    "HybridCLRSetting is missing from ProjectSettingSelector. " +
                    "Initialize the project settings before building.");
            }
        }

        private static void ValidateDheBuildConfiguration(
            BuildPipelineContext context,
            HybridCLRSetting runtimeSetting)
        {
            if (context?.BuildSetting == null || context.BuildSetting.buildType != BuildType.App)
            {
                return;
            }

            if (!HybridCLRBuildWorkflow.IsDheEdition)
            {
                return;
            }

            HybridCLRBuildWorkflow.EnsureDheAvailable();
            if (!context.BuildSetting.isbuildDll || !context.BuildSetting.isBuildResources)
            {
                throw new InvalidOperationException(
                    "A DHE App build must enable both DLL and resource builds so the Player contains " +
                    "fresh placeholder artifacts and produces a matching baseline snapshot.");
            }
        }

        private static string[] ResolveConfiguredAssemblyNames(HybridCLRSetting runtimeSetting)
        {
            var sourceAssemblies = runtimeSetting.hotfixAssembliesSort ?? new List<string>();
            var results = new List<string>(sourceAssemblies);
            if (!string.IsNullOrWhiteSpace(runtimeSetting.logicMainDllName) &&
                !results.Contains(runtimeSetting.logicMainDllName, StringComparer.Ordinal))
            {
                results.Add(runtimeSetting.logicMainDllName);
            }

            string[] assemblyNames = results
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            string[] missing = assemblyNames
                .Where(name => FindAssemblyDefinitionAsset(name) == null)
                .ToArray();
            if (missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"HybridCLR hot-update assembly definitions are missing: {string.Join(", ", missing)}");
            }

            return assemblyNames;
        }

        private static void SyncRuntimeSetting(HybridCLRSetting runtimeSetting, string[] assemblyNames)
        {
            var current = runtimeSetting.hotfixAssembliesSort ?? new List<string>();
            if (current.SequenceEqual(assemblyNames, StringComparer.Ordinal))
            {
                return;
            }

            runtimeSetting.hotfixAssembliesSort = assemblyNames.ToList();

            EditorUtility.SetDirty(runtimeSetting);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"[BuildEnvironmentConfigurator] Updated runtime HybridCLR setting assemblies: {string.Join(", ", assemblyNames)}");
        }

        private static void SyncHybridClrSettingsAsset(string[] assemblyNames, bool useDhe)
        {
            var asmdefAssets = assemblyNames
                .Select(FindAssemblyDefinitionAsset)
                .Where(asset => asset != null)
                .ToArray();

            var settings = HybridCLRSettings.Instance;
            bool changed = false;

            if (useDhe)
            {
                changed |= UpdateStringArray(ref settings.hotUpdateAssemblies, Array.Empty<string>());
                changed |= UpdateAsmdefArray(ref settings.hotUpdateAssemblyDefinitions,
                    Array.Empty<AssemblyDefinitionAsset>());
#if LFRAMEWORK_HYBRIDCLR_DHE
                if (!HybridCLRBuildWorkflow.TryUpdateDifferentialHybridAssemblies(
                        settings,
                        assemblyNames,
                        out bool differentialChanged))
                {
                    throw new InvalidOperationException(
                        "DHE mode requires HybridCLRSettings.differentialHybridAssemblies.");
                }

                changed |= differentialChanged;
#endif
            }
            else
            {
                // HybridCLR treats assembly names and asmdef references as alternative sources;
                // keeping both populated would make SettingsUtil enumerate every assembly twice.
                changed |= UpdateStringArray(ref settings.hotUpdateAssemblies, Array.Empty<string>());
                changed |= UpdateAsmdefArray(ref settings.hotUpdateAssemblyDefinitions, asmdefAssets);
                changed |= ClearDifferentialHybridAssemblies(settings);
            }

            if (!changed)
            {
                return;
            }

            HybridCLRSettings.Save();
            Debug.Log(
                $"[BuildEnvironmentConfigurator] Synchronized ProjectSettings/HybridCLRSettings.asset in " +
                $"{(useDhe ? "DHE" : "standard HybridCLR")} mode: {string.Join(", ", assemblyNames)}");
        }

        internal static bool ClearDifferentialHybridAssemblies(HybridCLRSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var serializedSettings = new SerializedObject(settings);
            SerializedProperty differentialAssemblies = serializedSettings.FindProperty(
                "differentialHybridAssemblies");
            if (differentialAssemblies == null || !differentialAssemblies.isArray ||
                differentialAssemblies.arraySize == 0)
            {
                return false;
            }

            differentialAssemblies.arraySize = 0;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            return true;
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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android",
                    "Sdk")
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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android",
                    "Sdk"),
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
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android",
                    "Android Studio", "jbr"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android",
                    "Android Studio", "jre"),
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
            string javaExe = Path.Combine(jdkRoot, "bin",
                Application.platform == RuntimePlatform.WindowsEditor ? "java.exe" : "java");
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
