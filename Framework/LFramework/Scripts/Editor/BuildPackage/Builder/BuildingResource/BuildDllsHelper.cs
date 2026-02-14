using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if USE_HybridCLR
using HybridCLR.Editor;
using HybridCLR.Editor.AOT;
using HybridCLR.Editor.Commands;
using HybridCLR.Editor.HotUpdate;
using HybridCLR.Editor.Installer;
using HybridCLR.Editor.Meta;
using HybridCLR.Editor.Settings;
#endif

using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor
{
    public static class BuildDllsHelper
    {
        public static bool CopyDll(BuildResourcesData buildResourcesData, AddressableAssetSettings settings,
            GameSetting gameSetting, string backAotFolder)
        {
#if USE_HybridCLR
            var hybridClrSettings = AssetUtilities.GetAllAssetsOfType<HybridCLRSetting>();
            var firstHybridClrSetting = hybridClrSettings.FirstOrDefault();
            if (firstHybridClrSetting == null)
            {
                Log.Error("HybridCLRSettings not found in project!");
                return false;
            }

            if (!CopyAotDllToProject(buildResourcesData, settings, firstHybridClrSetting, gameSetting, backAotFolder))
            {
                return false;
            }

            if (!CopyHotfixDllToProject(buildResourcesData, settings, firstHybridClrSetting, gameSetting))
            {
                return false;
            }
#endif
  

            return true;
        }

        public static bool BuildDll(bool isBuildApp, string backAotFolder)
        {
#if USE_HybridCLR
            CheckInstallHybridClr();

            if (isBuildApp)
            {
                PrebuildCommand.GenerateAll();
                //构建整包
                //Copy 裁剪aot 到 备份目录
                StripAOTAssembly(backAotFolder);
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                GenerateAOTGenericReference(target, backAotFolder);
            }
            else
            {
                //编译Dll
                BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
                CompileDllCommand.CompileDll(target, EditorUserBuildSettings.development);
            }

#endif

            return true;
        }

#if USE_HybridCLR
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

        private static bool CopyAotDllToProject(BuildResourcesData buildResourcesData,
            AddressableAssetSettings settings, HybridCLRSetting firstHybridClrSetting, GameSetting gameSetting,
            string backUpFolder)
        {
            if (!GetGroupInSettings(settings, firstHybridClrSetting.aotAddressableGroupName, out var group))
            {
                return false;
            }

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
            foreach (var path in targetFilesPath)
            {
                // 添加资源到 Addressable
                var assetsGuid = AssetDatabase.AssetPathToGUID(FullPathToUnityPath(path));
                var entry = settings.CreateOrMoveEntry(assetsGuid, group);
                entry.SetLabel(gameSetting.hybridClrSetting.defaultAotDllLabel, true);
                entry.SetLabel(gameSetting.hybridClrSetting.defaultInitLabel, true);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return true;
        }

        private static bool CopyHotfixDllToProject(BuildResourcesData buildResourcesData,
            AddressableAssetSettings settings, HybridCLRSetting firstHybridClrSetting, GameSetting gameSetting)
        {
            if (!GetGroupInSettings(settings, firstHybridClrSetting.codeAddressableGroupName, out var group))
            {
                return false;
            }

            var hotfixInProjectFolder = GetHotfixPathInProject(firstHybridClrSetting);
            if (Directory.Exists(hotfixInProjectFolder))
            {
                Directory.Delete(hotfixInProjectFolder, true);
            }

            Directory.CreateDirectory(hotfixInProjectFolder);
            var folder = GetHybridClrDataHOtUpdateDllsFolder(buildResourcesData);
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
            foreach (var path in targetFilesPath)
            {
                // 添加资源到 Addressable
                var assetsGuid = AssetDatabase.AssetPathToGUID(FullPathToUnityPath(path));
                var entry = settings.CreateOrMoveEntry(assetsGuid, group);
                entry.SetLabel(gameSetting.hybridClrSetting.defaultInitLabel, true);
                entry.SetLabel(gameSetting.hybridClrSetting.defaultCodeDllLabel, true);
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
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
            var checker = new MissingMetadataChecker(aotDir, SettingsUtil.HotUpdateAssemblyNamesIncludePreserved);

            string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
            {
                string dllPath = $"{hotUpdateDir}/{dll}";
                bool notAnyMissing = checker.Check(dllPath);
                if (!notAnyMissing)
                {
                    // DO SOMETHING
                    Log.Info("Missing metadata found in hot update dll: " + dllPath);
                }
            }
        }

        private static string FullPathToUnityPath(string fullFilePath)
        {
            return "Assets/" + fullFilePath.Replace(Application.dataPath + "/", "");
        }

        private static bool GetGroupInSettings(AddressableAssetSettings settings, string groupName,
            out AddressableAssetGroup group)
        {
            group = settings.FindGroup(groupName);
            if (group == null)
            {
                AddressableHelper.GenerateDefaultGroup(groupName, settings, null, out group, out var groupSchema);
                groupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackSeparately;
            }

            return true;
        }

        private static string GetHotfixPathInProject(HybridCLRSetting fHybridClrSetting)
        {
            return Application.dataPath + "/" + fHybridClrSetting.hotfixDllFolderPath;
        }

        private static string GetAotPathInProject(HybridCLRSetting firstHybridClrSetting)
        {
            return Application.dataPath + "/" + firstHybridClrSetting.aotDllFolderPath;
        }

        private static string GetHybridClrDataHOtUpdateDllsFolder(BuildResourcesData buildResourcesData)
        {
            return GetHybridClrDataRoot() + "/" + "HotUpdateDlls" + "/" + buildResourcesData.BuilderTarget.ToString();
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