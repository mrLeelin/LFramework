using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using HybridCLR.Editor.Settings;


namespace HybridCLR.Editor
{
    public static class SettingsUtil
    {
        public static bool Enable
        {
            get => HybridCLRSettings.Instance.enable;
            set
            {
                HybridCLRSettings.Instance.enable = value;
                HybridCLRSettings.Save();
            }
        }
        
        public static string UnityIl2CppDllPath
        {
            get
            {
#if UNITY_2019
#if UNITY_EDITOR_WIN
                return $"{LocalIl2CppDir}/build/deploy/net471/Unity.IL2CPP.dll";
#else
            return $"{LocalIl2CppDir}/build/deploy/il2cppcore/Unity.IL2CPP.dll";
#endif
#elif UNITY_2020
                return $"{LocalIl2CppDir}/build/deploy/netcoreapp3.1/Unity.IL2CPP.dll";
#elif !UNITY_2023_1_OR_NEWER
                return $"{LocalIl2CppDir}/build/deploy/Unity.IL2CPP.dll";
#else
                return ResolveExistingPath(
                    $"{LocalIl2CppDir}/build/deploy/deploy-2022/Unity.IL2CPP.dll",
                    $"{LocalIl2CppDir}/build/deploy/Unity.IL2CPP.dll");
#endif
            }
        }

        private static string ResolveExistingPath(params string[] candidatePaths)
        {
            return candidatePaths.FirstOrDefault(File.Exists) ?? candidatePaths[0];
        }

        /// <summary>
        /// HybridCLR 的物理目录。
        /// 已通过 UPM 注册时使用 PackageInfo.resolvedPath；源码直接放入 Assets 时，
        /// 直接使用当前脚本的资产路径。两种布局都不依赖包名或包根目录名称。
        /// </summary>
        public static string HybridCLRPackagePath => GetHybridClrRootPath();

        public static string HybridCLRDataPathInPackage => $"{HybridCLRPackagePath}/Data~";

        public static string TemplatePathInPackage => $"{HybridCLRDataPathInPackage}/Templates";

        public static string ProjectDir { get; } = Directory.GetParent(Application.dataPath).ToString();

        private static string GetHybridClrRootPath()
        {
            DirectoryInfo editorDirectory = Directory.GetParent(GetSettingsUtilScriptFullPath());
            if (editorDirectory?.Parent != null)
            {
                return editorDirectory.Parent.FullName;
            }

            throw new DirectoryNotFoundException(
                "Unable to resolve the HybridCLR directory from SettingsUtil.cs.");
        }

        private static string GetSettingsUtilScriptFullPath()
        {
            string scriptAssetPath = AssetDatabase.FindAssets("SettingsUtil t:MonoScript")
                .Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path =>
                    string.Equals(Path.GetFileNameWithoutExtension(path), nameof(SettingsUtil),
                        StringComparison.Ordinal));
            if (string.IsNullOrWhiteSpace(scriptAssetPath))
            {
                throw new FileNotFoundException("Unable to locate SettingsUtil.cs in the AssetDatabase.");
            }

            UnityEditor.PackageManager.PackageInfo packageInfo =
                UnityEditor.PackageManager.PackageInfo.FindForAssetPath(scriptAssetPath);
            if (!string.IsNullOrWhiteSpace(packageInfo?.resolvedPath) &&
                TryGetPackageRelativePath(scriptAssetPath, out string relativePath))
            {
                return Path.Combine(packageInfo.resolvedPath, relativePath);
            }

            return Path.GetFullPath(Path.Combine(ProjectDir, scriptAssetPath));
        }

        private static bool TryGetPackageRelativePath(string assetPath, out string relativePath)
        {
            relativePath = null;
            string normalizedPath = assetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            int packageNameEndIndex = normalizedPath.IndexOf('/', "Packages/".Length);
            if (packageNameEndIndex < 0 || packageNameEndIndex == normalizedPath.Length - 1)
            {
                return false;
            }

            relativePath = normalizedPath.Substring(packageNameEndIndex + 1);
            return true;
        }

        public static string ScriptingAssembliesJsonFile { get; } = "ScriptingAssemblies.json";

        public static string HotUpdateDllsRootOutputDir => HybridCLRSettings.Instance.hotUpdateDllCompileOutputRootDir;

        public static string AssembliesPostIl2CppStripDir => HybridCLRSettings.Instance.strippedAOTDllOutputRootDir;

        public static string HybridCLRDataDir => $"{ProjectDir}/HybridCLRData";

        public static string LocalUnityDataDir => $"{HybridCLRDataDir}/LocalIl2CppData-{Application.platform}";

        public static string LocalIl2CppDir => $"{LocalUnityDataDir}/il2cpp";

        public static string GeneratedCppDir => $"{LocalIl2CppDir}/libil2cpp/hybridclr/generated";

        public static string Il2CppBuildCacheDir { get; } = $"{ProjectDir}/Library/Il2cppBuildCache";

        public static string GlobalgamemanagersBinFile { get; } = "globalgamemanagers";

        public static string Dataunity3dBinFile { get; } = "data.unity3d";

        public static string GetHotUpdateDllsOutputDirByTarget(BuildTarget target)
        {
            return $"{HotUpdateDllsRootOutputDir}/{target}";
        }

        public static string GetAssembliesPostIl2CppStripDir(BuildTarget target)
        {
            return $"{AssembliesPostIl2CppStripDir}/{target}";
        }

        class AssemblyDefinitionData
        {
            public string name;
        }

        public static List<string> HotUpdateAssemblyNamesExcludePreserved
        {
            get
            {
                var gs = HybridCLRSettings.Instance;
                var hotfixAssNames = (gs.hotUpdateAssemblyDefinitions ?? Array.Empty<AssemblyDefinitionAsset>()).Select(ad => JsonUtility.FromJson<AssemblyDefinitionData>(ad.text));

                var hotfixAssembles = new List<string>();
                foreach (var assName in hotfixAssNames)
                {
                    hotfixAssembles.Add(assName.name);
                }
                hotfixAssembles.AddRange(gs.hotUpdateAssemblies ?? Array.Empty<string>());
                return hotfixAssembles.ToList();
            }
        }

        public static List<string> HotUpdateAssemblyFilesExcludePreserved => HotUpdateAssemblyNamesExcludePreserved.Select(dll => dll + ".dll").ToList();


        public static List<string> HotUpdateAndDHEAssemblyNamesExcludePreserved => HotUpdateAssemblyNamesExcludePreserved.Concat(HybridCLRSettings.Instance.differentialHybridAssemblies ?? Array.Empty<string>()).ToList();

        public static List<string> HotUpdateAssemblyNamesIncludePreserved
        {
            get
            {
                List<string> allAsses = HotUpdateAssemblyNamesExcludePreserved;
                string[] preserveAssemblyNames = HybridCLRSettings.Instance.preserveHotUpdateAssemblies;
                if (preserveAssemblyNames != null && preserveAssemblyNames.Length > 0)
                {
                    foreach (var assemblyName in preserveAssemblyNames)
                    {
                        if (allAsses.Contains(assemblyName))
                        {
                            throw new Exception($"[HotUpdateAssemblyNamesIncludePreserved] assembly:'{assemblyName}' is duplicated");
                        }
                        allAsses.Add(assemblyName);
                    }
                }

                return allAsses;
            }
        }

        public static List<string> DifferentialHybridAssemblyNames => HybridCLRSettings.Instance.differentialHybridAssemblies?.ToList() ?? new List<string>();

        public static List<string> HotUpdateAssemblyFilesIncludePreserved => HotUpdateAssemblyNamesIncludePreserved.Select(ass => ass + ".dll").ToList();

        public static List<string> HotUpdateAndDHEAssemblyNamesIncludePreserved => HotUpdateAssemblyNamesIncludePreserved.Concat(HybridCLRSettings.Instance.differentialHybridAssemblies ?? Array.Empty<string>()).ToList();

        public static List<string> AOTAssemblyNames => HybridCLRSettings.Instance.patchAOTAssemblies.ToList();

        public static HybridCLRSettings HybridCLRSettings => HybridCLRSettings.Instance;

        public static EncryptionSettings EncryptionSettings => HybridCLRSettings.Instance.encryptionSettings;

        public static string[] InjectRulesFiles => HybridCLRSettings.Instance.injectRuleFiles ?? Array.Empty<string>();
    }
}
