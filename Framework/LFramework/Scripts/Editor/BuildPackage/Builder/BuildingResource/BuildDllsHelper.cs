using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if HybridCLR_SUPPORT
using HybridCLR.Editor;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.HotUpdate;
using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.Settings;
#endif

using GameFramework.Resource;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Editor.Builder.HybridCLR;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("LFramework.Editor.Tests")]

namespace LFramework.Editor
{
    public static class BuildDllsHelper
    {
        public static bool CopyDll(BuildSetting buildSetting, HybridCLRSetting hybridClrSetting, string backAotFolder)
        {
#if HybridCLR_SUPPORT
            var registrar = CreateRegistrar(buildSetting.ResourceSystem);
            if (registrar == null)
            {
                return false;
            }

            bool useDhe = IsDheEnabled(hybridClrSetting);
            if (!useDhe && ShouldCopyAotDlls(buildSetting.buildType) &&
                !CopyAotDllToProject(buildSetting, registrar, hybridClrSetting, backAotFolder))
            {
                return false;
            }

            if (useDhe)
            {
                HybridCLRBuildWorkflow.EnsureDheAvailable();
                bool copied = buildSetting.buildType == BuildType.App
                    ? CopyDhePlaceholdersToProject(buildSetting, registrar, hybridClrSetting)
                    : CopyDheArtifactsToProject(
                        buildSetting,
                        registrar,
                        hybridClrSetting,
                        backAotFolder);
                if (!copied)
                {
                    return false;
                }
            }
            else if (!CopyHotfixDllToProject(buildSetting, registrar, hybridClrSetting))
            {
                return false;
            }
#endif

            return true;
        }

        private static bool ShouldCopyAotDlls(BuildType buildType)
        {
            return buildType == BuildType.App || buildType == BuildType.ResourcesUpdate;
        }

        public static bool BuildDll(bool isBuildApp, string backAotFolder)
        {
#if HybridCLR_SUPPORT
            CheckInstallHybridClr();
            var hybridClrSetting = SettingManager.GetSetting<HybridCLRSetting>();
            bool useDhe = IsDheEnabled(hybridClrSetting);
            if (useDhe)
            {
                HybridCLRBuildWorkflow.EnsureDheAvailable();
            }

            if (isBuildApp)
            {
                PrebuildCommand.GenerateAll();
                //构建整包
                //Copy 裁剪aot 到 备份目录
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                if (!useDhe)
                {
                    StripAOTAssembly(backAotFolder);
                    GenerateAOTGenericReference(target, backAotFolder);
                }
            }
            else
            {
                //编译Dll
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);
                if (useDhe)
                {
                    HybridCLRBuildWorkflow.GenerateHotUpdateMetaVersionFiles(
                        GetDheSnapshotDirectory(backAotFolder, target),
                        target);
                }
            }

#endif

            return true;
        }

#if HybridCLR_SUPPORT
        private static bool IsDheEnabled(HybridCLRSetting setting)
        {
            return setting != null &&
                   setting.loadMode == HybridCLRLoadMode.DifferentialHybridMetaVersion;
        }

        internal static string GetDheSnapshotDirectory(string backupPath, BuildTarget target)
        {
            return Path.GetFullPath(Path.Combine(
                backupPath,
                "DHE",
                target.ToString(),
                "AotSnapshot"));
        }

        /// <summary>
        /// 根据 ResourceMode 创建对应的 DLL 资源注册器
        /// </summary>
        internal static IDllResourceRegistrar CreateRegistrar(ResourceMode mode)
        {
            switch (mode)
            {
                case ResourceMode.Addressable:
#if ADDRESSABLE_SUPPORT
                     return new AddressableDllRegistrar();
#else
                    Log.Error("Addressable is not enabled. Define ADDRESSABLE_SUPPORT in Player Settings.");
                    return null;
#endif

                   
                case ResourceMode.YooAsset:
#if YOOASSET_SUPPORT
                    return new YooAssetDllRegistrar();
#else
                    Log.Error("YooAsset support is not enabled. Define YOOASSET_SUPPORT in Player Settings.");
                    return null;
#endif
                default:
                    Log.Error($"Unsupported resource system: {mode}");
                    return null;
            }
        }

        /// <summary>
        /// 调用AotGenericReferenceWriter，生成AOT泛型文件
        /// </summary>
        /// <param name="target"></param>
        private static void GenerateAOTGenericReference(BuildTarget target, string backAotFolder)
        {
            var gs = SettingsUtil.HybridCLRSettings;
            List<string> hotUpdateDllNames = SettingsUtil.HotUpdateAssemblyNamesExcludePreserved;

            AssemblyReferenceDeepCollector collector = new AssemblyReferenceDeepCollector(
                MetaUtil.CreateHotUpdateAndAOTAssemblyResolver(target, hotUpdateDllNames), hotUpdateDllNames);
            var analyzer = new Analyzer(new Analyzer.Options
            {
                MaxIterationCount = Math.Min(20, gs.maxGenericReferenceIteration),
                Collector = collector,
            });

            analyzer.Run();

            var modules = new HashSet<dnlib.DotNet.ModuleDef>(
                analyzer.AotGenericTypes.Select(t => t.Type.Module)
                    .Concat(analyzer.AotGenericMethods.Select(m => m.Method.Module))).ToList();
            modules.Sort((a, b) => a.Name.CompareTo(b.Name));
            var filePath = GetHybridClrAotTxtFilePath(backAotFolder);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var strBuilder = new System.Text.StringBuilder();
            foreach (dnlib.DotNet.ModuleDef module in modules)
            {
                strBuilder.Append(module.Name);
                strBuilder.Append(",");
            }

            File.WriteAllText(filePath, strBuilder.ToString());
            AssetDatabase.Refresh();
        }

        /// 进一步剔除AOT dll中非泛型函数元数据，输出到StrippedAOTAssembly2目录下
        private static void StripAOTAssembly(string backAotFolder)
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string srcDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
            string dstDir = GetHybridClrBackUpFolder(backAotFolder);
            if (!Directory.Exists(dstDir))
            {
                Directory.CreateDirectory(dstDir);
            }

            foreach (var src in Directory.GetFiles(srcDir, "*.dll"))
            {
                string dllName = Path.GetFileName(src);
                string dstFile = $"{dstDir}/{dllName}";
                AOTAssemblyMetadataStripper.Strip(src, dstFile);
            }
        }


        private static void CheckInstallHybridClr()
        {
            InstallerController hybridClrInstaller = new InstallerController();
            if (hybridClrInstaller.HasInstalledHybridCLR() &&
                hybridClrInstaller.PackageVersion == hybridClrInstaller.InstalledLibil2cppVersion)
            {
                Debug.Log("HybridClr 已安装!");
                return;
            }

            Debug.Log($"开始安装HybridClr, version={hybridClrInstaller.PackageVersion}");
            hybridClrInstaller.InstallDefaultHybridCLR();
        }

        private static bool CopyAotDllToProject(BuildSetting buildSetting, IDllResourceRegistrar registrar,
            HybridCLRSetting firstHybridClrSetting, string backUpFolder)
        {
            var aotInProjectFolder = GetAotPathInProject(firstHybridClrSetting);
            if (Directory.Exists(aotInProjectFolder))
            {
                Directory.Delete(aotInProjectFolder, true);
            }

            Directory.CreateDirectory(aotInProjectFolder);

            var folder = GetHybridClrBackUpFolder(backUpFolder);
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"The AOT dlls folder is not exist! '{folder}'");
                return false;
            }

            var patchedAotAssemblyPath = GetHybridClrAotTxtFilePath(backUpFolder);
            if (!File.Exists(patchedAotAssemblyPath))
            {
                Debug.LogError("The AOT dlls txt file is not exist! " + patchedAotAssemblyPath);
                return false;
            }

            var aotPathStr = File.ReadAllText(patchedAotAssemblyPath);
            Debug.Log("AOT dlls : " + aotPathStr);

            List<string> targetFilesPath = new List<string>();
            foreach (var aotName in aotPathStr.Split(','))
            {
                if (string.IsNullOrEmpty(aotName))
                {
                    continue;
                }

                Debug.Log("AOT dll name : " + aotName);
                var filePath = folder + "/" + aotName;
                if (!File.Exists(filePath))
                {
                    Debug.LogError("The AOT dll is not exist! " + filePath);
                    continue;
                }

                var targetFilPath = aotInProjectFolder + "/" + aotName + ".bytes";
                File.Copy(filePath, targetFilPath);
                var bytes = File.ReadAllBytes(targetFilPath);
                var encryptByte = AESUtility.Encrypt(bytes);
                File.WriteAllBytes(targetFilPath, encryptByte);
                if (!File.Exists(targetFilPath))
                {
                    Debug.LogError("The AoT dll copy failed! " + targetFilPath);
                }

                targetFilesPath.Add(targetFilPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!registrar.RegisterAotDlls(targetFilesPath, firstHybridClrSetting, buildSetting.buildType))
            {
                return false;
            }

            return true;
        }

        private static bool CopyHotfixDllToProject(BuildSetting buildSetting,
            IDllResourceRegistrar registrar, HybridCLRSetting firstHybridClrSetting)
        {
            var hotfixInProjectFolder = GetHotfixPathInProject(firstHybridClrSetting);
            if (Directory.Exists(hotfixInProjectFolder))
            {
                Directory.Delete(hotfixInProjectFolder, true);
            }

            Directory.CreateDirectory(hotfixInProjectFolder);
            var folder = GetHybridClrDataHOtUpdateDllsFolder(buildSetting);
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"The hotfix dlls folder is not exist! '{folder}'");
                return false;
            }

            List<string> targetFilesPath = new List<string>();
            foreach (var aotName in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                var filePath = folder + "/" + aotName;
                if (!File.Exists(filePath))
                {
                    Debug.LogError("The Hotfix dll is not exist! " + filePath);
                    continue;
                }

                var targetFilPath = hotfixInProjectFolder + "/" + aotName + ".bytes";
                File.Copy(filePath, targetFilPath);
                var bytes = File.ReadAllBytes(targetFilPath);
                var encryptByte = AESUtility.Encrypt(bytes);
                File.WriteAllBytes(targetFilPath, encryptByte);
                if (!File.Exists(targetFilPath))
                {
                    Debug.LogError("The Hotfix dll copy failed! " + targetFilPath);
                }

                targetFilesPath.Add(targetFilPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!registrar.RegisterHotfixDlls(targetFilesPath, firstHybridClrSetting, buildSetting.buildType))
            {
                return false;
            }

            return true;
        }

        private static bool CopyDhePlaceholdersToProject(
            BuildSetting buildSetting,
            IDllResourceRegistrar registrar,
            HybridCLRSetting setting)
        {
            string outputRoot = GetHotfixPathInProject(setting);
            IReadOnlyList<string> assemblyNames = GetConfiguredDheAssemblyNames(setting);
            PrepareDheOutputDirectories(
                outputRoot,
                assemblyNames,
                out string codeOutput,
                out string currentMetaOutput,
                out string originalMetaOutput);

            var dllPaths = new List<string>();
            var currentMetaPaths = new List<string>();
            var originalMetaPaths = new List<string>();
            foreach (string assemblyName in assemblyNames)
            {
                string targetDll = Path.Combine(codeOutput, $"{assemblyName}.dll.bytes");
                string targetCurrentMeta = Path.Combine(currentMetaOutput,
                    $"{assemblyName}.mv.bytes");
                string targetOriginalMeta = Path.Combine(originalMetaOutput,
                    $"{assemblyName}.mv.bytes");
                File.WriteAllBytes(targetDll, new byte[] { 1 });
                File.WriteAllBytes(targetCurrentMeta, new byte[] { 1 });
                File.WriteAllBytes(targetOriginalMeta, new byte[] { 1 });
                dllPaths.Add(targetDll);
                currentMetaPaths.Add(targetCurrentMeta);
                originalMetaPaths.Add(targetOriginalMeta);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return registrar.RegisterDheArtifacts(
                dllPaths,
                currentMetaPaths,
                originalMetaPaths,
                setting,
                buildSetting.buildType);
        }

        private static bool CopyDheArtifactsToProject(
            BuildSetting buildSetting,
            IDllResourceRegistrar registrar,
            HybridCLRSetting setting,
            string backupPath)
        {
            string outputRoot = GetHotfixPathInProject(setting);
            IReadOnlyList<string> assemblyNames = GetConfiguredDheAssemblyNames(setting);

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            string hotUpdateDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            string currentMetaDirectory = HybridCLRBuildWorkflow.GetMetaVersionDirectory(
                hotUpdateDirectory);
            string baselineDirectory = GetDheSnapshotDirectory(backupPath, target);
            string originalMetaDirectory = HybridCLRBuildWorkflow.GetMetaVersionDirectory(
                baselineDirectory);

            var sourceArtifacts = assemblyNames.Select(assemblyName => new
            {
                AssemblyName = assemblyName,
                Dll = Path.Combine(hotUpdateDirectory, $"{assemblyName}.dll"),
                CurrentMeta = Path.Combine(currentMetaDirectory, $"{assemblyName}.mv.bytes"),
                OriginalMeta = Path.Combine(originalMetaDirectory, $"{assemblyName}.mv.bytes")
            }).ToList();
            foreach (var artifact in sourceArtifacts)
            {
                if (!File.Exists(artifact.Dll) || !File.Exists(artifact.CurrentMeta) ||
                    !File.Exists(artifact.OriginalMeta))
                {
                    Debug.LogError(
                        $"DHE artifacts are incomplete for '{artifact.AssemblyName}'. " +
                        $"DLL={File.Exists(artifact.Dll)}, currentMeta={File.Exists(artifact.CurrentMeta)}, " +
                        $"originalMeta={File.Exists(artifact.OriginalMeta)}");
                    return false;
                }
            }

            PrepareDheOutputDirectories(
                outputRoot,
                assemblyNames,
                out string codeOutput,
                out string currentMetaOutput,
                out string originalMetaOutput);

            var dllPaths = new List<string>();
            var currentMetaPaths = new List<string>();
            var originalMetaPaths = new List<string>();
            foreach (var artifact in sourceArtifacts)
            {
                string targetDll = Path.Combine(codeOutput, $"{artifact.AssemblyName}.dll.bytes");
                string targetCurrentMeta = Path.Combine(currentMetaOutput,
                    $"{artifact.AssemblyName}.mv.bytes");
                string targetOriginalMeta = Path.Combine(originalMetaOutput,
                    $"{artifact.AssemblyName}.mv.bytes");
                File.WriteAllBytes(targetDll, AESUtility.Encrypt(File.ReadAllBytes(artifact.Dll)));
                File.Copy(artifact.CurrentMeta, targetCurrentMeta, true);
                File.Copy(artifact.OriginalMeta, targetOriginalMeta, true);
                dllPaths.Add(targetDll);
                currentMetaPaths.Add(targetCurrentMeta);
                originalMetaPaths.Add(targetOriginalMeta);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return registrar.RegisterDheArtifacts(
                dllPaths,
                currentMetaPaths,
                originalMetaPaths,
                setting,
                buildSetting.buildType);
        }

        private static void PrepareDheOutputDirectories(
            string outputRoot,
            IReadOnlyList<string> assemblyNames,
            out string codeOutput,
            out string currentMetaOutput,
            out string originalMetaOutput)
        {
            codeOutput = Path.Combine(outputRoot, "DHE", "Code");
            currentMetaOutput = Path.Combine(outputRoot, "DHE", "CurrentMeta");
            originalMetaOutput = Path.Combine(outputRoot, "DHE", "OriginalMeta");
            Directory.CreateDirectory(codeOutput);
            Directory.CreateDirectory(currentMetaOutput);
            Directory.CreateDirectory(originalMetaOutput);

            DeleteStaleDheArtifacts(
                codeOutput,
                assemblyNames.Select(name => $"{name}.dll.bytes"));
            DeleteStaleDheArtifacts(
                currentMetaOutput,
                assemblyNames.Select(name => $"{name}.mv.bytes"));
            DeleteStaleDheArtifacts(
                originalMetaOutput,
                assemblyNames.Select(name => $"{name}.mv.bytes"));
        }

        private static void DeleteStaleDheArtifacts(
            string directory,
            IEnumerable<string> expectedFileNames)
        {
            var expected = new HashSet<string>(expectedFileNames, StringComparer.OrdinalIgnoreCase);
            foreach (string filePath in Directory.EnumerateFiles(directory, "*.bytes"))
            {
                if (expected.Contains(Path.GetFileName(filePath)))
                {
                    continue;
                }

                string assetPath = FullPathToUnityPath(filePath);
                if (!string.IsNullOrWhiteSpace(assetPath) && AssetDatabase.DeleteAsset(assetPath))
                {
                    continue;
                }

                File.Delete(filePath);
                string metaPath = filePath + ".meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }

        private static IReadOnlyList<string> GetConfiguredDheAssemblyNames(HybridCLRSetting setting)
        {
            var assemblyNames = setting?.differentialHybridAssembliesSort?
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.Ordinal)
                .ToList() ?? new List<string>();
            if (setting != null && !string.IsNullOrWhiteSpace(setting.logicMainDllName) &&
                !assemblyNames.Contains(setting.logicMainDllName, StringComparer.Ordinal))
            {
                assemblyNames.Add(setting.logicMainDllName);
            }

            if (assemblyNames.Count == 0)
            {
                throw new InvalidOperationException("DHE mode has no configured assemblies.");
            }

            if (setting == null || string.IsNullOrWhiteSpace(setting.logicMainDllName))
            {
                throw new InvalidOperationException("DHE mode requires a main logic assembly name.");
            }

            return assemblyNames;
        }

        [MenuItem("HybridCLR/Check Access Missing Metadata")]
        private static void CheckAccessMissingMetadata()
        {
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            // aotDir指向 构建主包时生成的裁剪aot dll目录，而不是最新的SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录。
            // 一般来说，发布热更新包时，由于中间可能调用过generate/all，SettingsUtil.GetAssembliesPostIl2CppStripDir(target)目录中包含了最新的aot dll，
            // 肯定无法检查出类型或者函数裁剪的问题。
            // 需要在构建完主包后，将当时的aot dll保存下来，供后面补充元数据或者裁剪检查。
            string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);

            // 第2个参数hotUpdateAssNames为热更新程序集列表。对于旗舰版本，该列表需要包含DHE程序集，即SettingsUtil.HotUpdateAndDHEAssemblyNamesIncludePreserved。
            var hybridClrSetting = SettingManager.GetSetting<HybridCLRSetting>();
            if (IsDheEnabled(hybridClrSetting))
            {
                var hotUpdateAndDheAssemblies = SettingsUtil.HotUpdateAssemblyNamesIncludePreserved
                    .Concat(GetConfiguredDheAssemblyNames(hybridClrSetting))
                    .Distinct(StringComparer.Ordinal);
                var checker = new MissingMetadataChecker(aotDir, hotUpdateAndDheAssemblies);
                CheckHotUpdateAssemblies(checker, target, hotUpdateAndDheAssemblies);
                return;
            }

            {
                var checker = new MissingMetadataChecker(aotDir,
                    SettingsUtil.HotUpdateAssemblyNamesIncludePreserved);
                CheckHotUpdateAssemblies(
                    checker,
                    target,
                    SettingsUtil.HotUpdateAssemblyNamesExcludePreserved);
            }
        }

        private static void CheckHotUpdateAssemblies(
            MissingMetadataChecker checker,
            BuildTarget target,
            IEnumerable<string> assemblyNames)
        {
            string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            foreach (string assemblyName in assemblyNames.Distinct(StringComparer.Ordinal))
            {
                string dllPath = $"{hotUpdateDir}/{assemblyName}.dll";
                bool notAnyMissing = checker.Check(dllPath);
                if (!notAnyMissing)
                {
                    Log.Info("Missing metadata found in hot update dll: " + dllPath);
                }
            }
        }

        private static string FullPathToUnityPath(string fullFilePath)
        {
            string normalizedPath = Path.GetFullPath(fullFilePath).Replace('\\', '/');
            string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
            if (!normalizedPath.StartsWith(dataPath + "/", StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }

            return "Assets" + normalizedPath.Substring(dataPath.Length);
        }

        private static string GetHotfixPathInProject(HybridCLRSetting fHybridClrSetting)
        {
            return Application.dataPath + "/" + fHybridClrSetting.hotfixDllFolderPath;
        }

        private static string GetAotPathInProject(HybridCLRSetting firstHybridClrSetting)
        {
            return Application.dataPath + "/" + firstHybridClrSetting.aotDllFolderPath;
        }

        private static string GetHybridClrDataHOtUpdateDllsFolder(BuildSetting buildSetting)
        {
            return GetHybridClrDataRoot() + "/" + "HotUpdateDlls" + "/" + buildSetting.builderTarget.ToString();
        }

        private static string GetHybridClrDataRoot()
        {
            return Application.dataPath + "/../" + "HybridCLRData";
        }

        private static string GetHybridClrBackUpFolder(string backUpPath)
        {
            return backUpPath + "/" + "Aot_Strip";
        }

        private static string GetHybridClrAotTxtFilePath(string backUpPath)
        {
            return backUpPath + "/" + "Aot_Strip" + "/" + "AotGenericReferences.txt";
        }
#endif
    }
}
