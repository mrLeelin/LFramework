#if YOOASSET_SUPPORT
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameFramework.Resource;
using LFramework.Editor;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEngine;
using YooAsset;
using YooAsset.Editor;
using BuildResult = YooAsset.Editor.BuildResult;
using ScriptableBuildPipeline = YooAsset.Editor.ScriptableBuildPipeline;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// YooAssets resource build system implementation.
    /// </summary>
    public class YooAssetsBuildSystem : IResourceBuildSystem
    {
        /// <summary>
        /// Builds resource content with YooAssets.
        /// </summary>
        /// <param name="buildResourcesData">Build settings.</param>
        public void Build(BuildSetting buildResourcesData)
        {
            Debug.Log("[YooAssets] Start building resources...");
            ResourceComponentSetting resourceComponentSetting = LoadResourceComponentSetting();
            if (resourceComponentSetting == null)
            {
                Debug.LogError("ResourceComponent is null in Build.");
                return;
            }

            GenerateRouteIndexIfEnabled(resourceComponentSetting);
            List<PackageDefinition> buildPackages = ResolveBuildPackages(resourceComponentSetting);
            if (buildPackages.Count == 0)
            {
                throw new InvalidOperationException("[YooAssets] No active package definitions were resolved for the current build target.");
            }

            if (buildResourcesData.isResourcesBuildIn)
            {
                BuildInPackage(resourceComponentSetting, buildPackages);
                return;
            }

            string buildPath = GetBuildPath(buildResourcesData);
            string loadPath = GetLoadPath(buildResourcesData);

            Debug.Log($"[YooAssets] Build Path: {buildPath}");
            Debug.Log($"[YooAssets] Load Path: {loadPath}");

            string exportBuildPath = GetExportBuildPath(buildResourcesData);
            string backupPath = GetBackupPath(buildResourcesData);
            string backupSeverDataPath = GetBackupSeverDataBuildPath(buildResourcesData);

            CreateDirectory(backupPath);

            string outputRoot = GetYooAssetOutputRoot();
            string buildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            DeleteDirectory(exportBuildPath);
            CreateDirectory(exportBuildPath);

            bool buildAllResources = buildResourcesData.buildType != BuildType.ResourcesUpdate;
            bool clearBuildCacheFiles = buildAllResources;
            string buildinCopyTag = buildAllResources
                ? SettingManager.GetSetting<HybridCLRSetting>().defaultInitLabel
                : string.Empty;
            EBuildinFileCopyOption buildinCopyOption = buildAllResources
                ? (string.IsNullOrEmpty(buildinCopyTag)
                    ? EBuildinFileCopyOption.ClearAndCopyAll
                    : EBuildinFileCopyOption.ClearAndCopyByTags)
                : EBuildinFileCopyOption.None;

            Debug.Log(buildAllResources
                ? "[YooAssets] Build all resources for active multi-package definitions."
                : "[YooAssets] Start incremental resource build for active multi-package definitions.");

            ExecuteBuildForPackages(
                buildPackages,
                outputRoot,
                buildinFileRoot,
                buildResourcesData.resourcesVersion,
                exportBuildPath,
                clearBuildCacheFiles,
                buildinCopyOption,
                buildinCopyTag);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            DeleteDirectory(backupSeverDataPath);
            CopyDirectory(exportBuildPath, backupSeverDataPath);
            BuildArtifactPostprocessHelper.ProcessBuildArtifacts(buildResourcesData, exportBuildPath);

            Debug.Log($"[YooAssets] Build Over, Please upload = {backupSeverDataPath}, upload url = {loadPath}");
        }

        /// <summary>
        /// Interface entry point for built-in package builds.
        /// </summary>
        public void BuildInPackage()
        {
        }

        /// <summary>
        /// Builds the built-in resource package into StreamingAssets.
        /// </summary>
        /// <param name="resourceComponentSetting">Resource component configuration.</param>
        private void BuildInPackage(ResourceComponentSetting resourceComponentSetting, List<PackageDefinition> buildPackages)
        {
            Debug.Log("[YooAssets] Start building built-in resource package...");

            string outputRoot = GetYooAssetOutputRoot();
            string buildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            ExecuteBuildForPackages(
                buildPackages,
                outputRoot,
                buildinFileRoot,
                "buildin",
                exportBuildPath: null,
                clearBuildCacheFiles: true,
                buildinFileCopyOption: EBuildinFileCopyOption.ClearAndCopyAll,
                buildinFileCopyParams: string.Empty);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[YooAssets] Build Over");
        }

        /// <summary>
        /// Gets the build path.
        /// </summary>
        public string GetBuildPath(BuildSetting data)
        {
            return BuildResourcePathHelper.GetBuildPath(data);
        }

        /// <summary>
        /// Gets the remote load path.
        /// </summary>
        public string GetLoadPath(BuildSetting data)
        {
            string path = string.Empty;
            if (data.cdnType == CdnType.Local)
            {
                path += BuildResourcePathHelper.GetUrl(data.cdnType);
            }
            else
            {
                path += BuildResourcePathHelper.GetUrl(data.cdnType) +
                        BuildResourcePathHelper.GetFolderNameBasedOnAppVersion(data) + "/" +
                        BuildResourcePathHelper.GetReplaceVersionName(data);
            }

            return path;
        }

        #region YooAsset Build Core

        private ScriptableBuildParameters BuildScriptableBuildParameters(
            string packageName,
            string outputRoot,
            string buildinFileRoot,
            string packageVersion)
        {
            return new ScriptableBuildParameters
            {
                BuildOutputRoot = outputRoot,
                BuildinFileRoot = buildinFileRoot,
                BuildPipeline = nameof(EBuildPipeline.ScriptableBuildPipeline),
                BuildBundleType = (int)EBuildBundleType.AssetBundle,
                BuildTarget = EditorUserBuildSettings.activeBuildTarget,
                PackageName = packageName,
                PackageVersion = packageVersion,
                PackageNote = null,
                ClearBuildCacheFiles = true,
                UseAssetDependencyDB = true,
                EnableSharePackRule = true,
                SingleReferencedPackAlone = true,
                VerifyBuildingResult = true,
                FileNameStyle = EFileNameStyle.BundleName_HashName,
                BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll,
                BuildinFileCopyParams = string.Empty,
                EncryptionServices = BuildEncryptionServices(),
                ManifestProcessServices = BuildManifestProcessServices(),
                ManifestRestoreServices = BuildManifestRestoreServices(),
                CompressOption = ECompressOption.LZ4,
            };
        }

        private IEncryptionServices BuildEncryptionServices()
        {
            return new EncryptionNone();
        }

        private IManifestProcessServices BuildManifestProcessServices()
        {
            return new ManifestProcessNone();
        }

        private IManifestRestoreServices BuildManifestRestoreServices()
        {
            return new ManifestRestoreNone();
        }

        private static ResourceComponentSetting LoadResourceComponentSetting()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(ResourceComponentSetting)}");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            return guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(path => AssetDatabase.LoadAssetAtPath<ResourceComponentSetting>(path))
                .FirstOrDefault(asset => asset != null);
        }

        /// <summary>
        /// Generates the route index before the build so the latest collector and init tag are always included.
        /// </summary>
        /// <param name="resourceComponentSetting">Resource component configuration.</param>
        private static void GenerateRouteIndexIfEnabled(ResourceComponentSetting resourceComponentSetting)
        {
            if (resourceComponentSetting == null || !resourceComponentSetting.YooAssetRouting.enableRouteIndex)
            {
                return;
            }

            RouteIndexGenerationResult result = RouteIndexGenerator.Generate(resourceComponentSetting);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"[YooAssets] Route index generation failed before build: {result.ErrorMessage}");
            }

            Debug.Log($"[YooAssets] Route index ready: {result.AssetPath} ({result.EntryCount} entries)");
        }

        /// <summary>
        /// Executes the YooAssets build and returns the actual output package directory.
        /// </summary>
        /// <param name="buildParameters">Build parameters.</param>
        /// <param name="packageName">Current build package name.</param>
        /// <returns>Resolved output package directory.</returns>
        private string ExecuteYooAssetBuild(ScriptableBuildParameters buildParameters, string packageName)
        {
            bool uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            buildParameters.BuiltinShadersBundleName =
                packRuleResult.GetBundleName(packageName, uniqueBundleName);

            var pipeline = new ScriptableBuildPipeline();
            BuildResult buildResult = pipeline.Run(buildParameters, true);

            if (!buildResult.Success)
            {
                Debug.LogError($"[YooAssets] Build failed: {buildResult.ErrorInfo}");
                throw new Exception($"[YooAssets] Build failed: {buildResult.ErrorInfo}");
            }

            Debug.Log($"[YooAssets] Build succeeded, output directory: {buildResult.OutputPackageDirectory}");
            return buildResult.OutputPackageDirectory;
        }

        private void ExecuteBuildForPackages(
            List<PackageDefinition> buildPackages,
            string outputRoot,
            string buildinFileRoot,
            string packageVersion,
            string exportBuildPath,
            bool clearBuildCacheFiles,
            EBuildinFileCopyOption buildinFileCopyOption,
            string buildinFileCopyParams)
        {
            for (int i = 0; i < buildPackages.Count; i++)
            {
                PackageDefinition packageDefinition = buildPackages[i];
                var buildParameters = BuildScriptableBuildParameters(
                    packageDefinition.yooPackageName,
                    outputRoot,
                    buildinFileRoot,
                    packageVersion);
                buildParameters.ClearBuildCacheFiles = clearBuildCacheFiles && i == 0;
                buildParameters.BuildinFileCopyOption = ResolvePackageBuildinCopyOption(buildinFileCopyOption, i);
                buildParameters.BuildinFileCopyParams = buildinFileCopyParams ?? string.Empty;

                Debug.Log(
                    $"[YooAssets] Building package '{packageDefinition.packageId}' ({packageDefinition.yooPackageName}), clearCache={buildParameters.ClearBuildCacheFiles}, copyOption={buildParameters.BuildinFileCopyOption}.");
                string outputDirectory = ExecuteYooAssetBuild(buildParameters, packageDefinition.yooPackageName);
                if (string.IsNullOrWhiteSpace(exportBuildPath))
                {
                    continue;
                }

                if (Directory.Exists(outputDirectory))
                {
                    CopyDirectory(outputDirectory, exportBuildPath);
                    Debug.Log($"[YooAssets] Copied build output: {outputDirectory} -> {exportBuildPath}");
                }
                else
                {
                    Debug.LogWarning($"[YooAssets] Build output directory does not exist: {outputDirectory}");
                }
            }
        }

        private static EBuildinFileCopyOption ResolvePackageBuildinCopyOption(
            EBuildinFileCopyOption sourceOption,
            int packageIndex)
        {
            if (packageIndex <= 0)
            {
                return sourceOption;
            }

            return sourceOption switch
            {
                EBuildinFileCopyOption.ClearAndCopyAll => EBuildinFileCopyOption.OnlyCopyAll,
                EBuildinFileCopyOption.ClearAndCopyByTags => EBuildinFileCopyOption.OnlyCopyByTags,
                _ => sourceOption
            };
        }

        private static List<PackageDefinition> ResolveBuildPackages(ResourceComponentSetting resourceComponentSetting)
        {
            return YooAssetMultiPackageUtility.CollectBuildPackages(
                resourceComponentSetting,
                GetPreviewRuntimePlatform(),
                GetPreviewChannel());
        }

        private static RuntimePlatform GetPreviewRuntimePlatform()
        {
            return EditorUserBuildSettings.activeBuildTarget switch
            {
                BuildTarget.Android => RuntimePlatform.Android,
                BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                BuildTarget.StandaloneWindows => RuntimePlatform.WindowsPlayer,
                BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                _ => RuntimePlatform.WindowsEditor
            };
        }

        private static string GetPreviewChannel()
        {
            try
            {
                GameSetting gameSetting = SettingManager.GetSetting<GameSetting>();
                if (gameSetting != null && !string.IsNullOrWhiteSpace(gameSetting.channel))
                {
                    return gameSetting.channel;
                }
            }
            catch
            {
                // Keep build-time package resolution resilient when settings are not initialized yet.
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets the YooAssets default build output root.
        /// </summary>
        private string GetYooAssetOutputRoot()
        {
            return AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        }

        #endregion

        #region Path Helper Methods

        private string GetExportBuildPath(BuildSetting data)
        {
            return BuildResourcePathHelper.GetExportBuildPath(data);
        }

        private string GetBackupPath(BuildSetting data)
        {
            return BuildResourcePathHelper.GetBackupPath(data);
        }

        private string GetBackupSeverDataBuildPath(BuildSetting data)
        {
            return BuildResourcePathHelper.GetBackupSeverDataBuildPath(data);
        }

        #endregion

        #region IO Helper Methods

        private void CopyDirectory(string from, string to)
        {
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            if (Directory.Exists(from))
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(from);
                FileInfo[] files = directoryInfo.GetFiles();

                for (int i = 0; i < files.Length; i++)
                {
                    string toPath = Path.Combine(to, files[i].Name);
                    if (File.Exists(toPath))
                    {
                        File.Delete(toPath);
                    }

                    files[i].CopyTo(toPath);
                }

                DirectoryInfo[] directoryInfoArray = directoryInfo.GetDirectories();
                for (int d = 0; d < directoryInfoArray.Length; d++)
                {
                    CopyDirectory(
                        Path.Combine(from, directoryInfoArray[d].Name),
                        Path.Combine(to, directoryInfoArray[d].Name));
                }
            }
        }

        private void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        #endregion
    }
}
#else
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// YooAssets resource build system implementation when the package is disabled.
    /// </summary>
    public class YooAssetsBuildSystem : IResourceBuildSystem
    {
        public void Build(BuildSetting buildResourcesData)
        {
            Debug.LogWarning(
                "[YooAssets] YOOASSET_SUPPORT is not defined. Add it in Player Settings -> Scripting Define Symbols.");
        }

        public void BuildInPackage()
        {
            Debug.LogWarning(
                "[YooAssets] YOOASSET_SUPPORT is not defined. Built-in resource build is unavailable.");
        }

        public string GetBuildPath(BuildSetting data)
        {
            return string.Empty;
        }

        public string GetLoadPath(BuildSetting data)
        {
            return string.Empty;
        }
    }
}
#endif
