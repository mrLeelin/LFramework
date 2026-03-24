#if YOOASSET_SUPPORT
using System;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
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
            var resourceComponentSetting =
                AssetUtilities.GetAllAssetsOfType<ResourceComponentSetting>().FirstOrDefault();
            if (resourceComponentSetting == null)
            {
                Debug.LogError("ResourceComponent is null in Build.");
                return;
            }

            if (buildResourcesData.isResourcesBuildIn)
            {
                BuildInPackage(resourceComponentSetting);
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
            var buildParameters = BuildScriptableBuildParameters(
                resourceComponentSetting,
                outputRoot,
                buildinFileRoot,
                buildResourcesData.resourcesVersion);

            string yooAssetOutputDir;
            if (buildResourcesData.buildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("[YooAssets] Start incremental resource build.");
                buildParameters.ClearBuildCacheFiles = false;
                buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.None;
                yooAssetOutputDir = ExecuteYooAssetBuild(resourceComponentSetting, buildParameters);
            }
            else
            {
                Debug.Log("[YooAssets] Build all resources.");
                buildParameters.ClearBuildCacheFiles = true;

                buildParameters.BuildinFileCopyParams =
                    SettingManager.GetSetting<HybridCLRSetting>().defaultInitLabel;
                buildParameters.BuildinFileCopyOption =
                    string.IsNullOrEmpty(buildParameters.BuildinFileCopyParams)
                        ? EBuildinFileCopyOption.ClearAndCopyAll
                        : EBuildinFileCopyOption.ClearAndCopyByTags;

                yooAssetOutputDir = ExecuteYooAssetBuild(resourceComponentSetting, buildParameters);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (Directory.Exists(yooAssetOutputDir))
            {
                DeleteDirectory(exportBuildPath);
                CreateDirectory(exportBuildPath);
                CopyDirectory(yooAssetOutputDir, exportBuildPath);
                Debug.Log($"[YooAssets] Copied build output: {yooAssetOutputDir} -> {exportBuildPath}");
            }
            else
            {
                Debug.LogWarning($"[YooAssets] Build output directory does not exist: {yooAssetOutputDir}");
            }

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
        private void BuildInPackage(ResourceComponentSetting resourceComponentSetting)
        {
            Debug.Log("[YooAssets] Start building built-in resource package...");

            string outputRoot = GetYooAssetOutputRoot();
            string buildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();

            var buildParameters = BuildScriptableBuildParameters(
                resourceComponentSetting,
                outputRoot,
                buildinFileRoot,
                "buildin");
            buildParameters.ClearBuildCacheFiles = true;
            buildParameters.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
            ExecuteYooAssetBuild(resourceComponentSetting, buildParameters);

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
            ResourceComponentSetting resourceComponentSetting,
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
                PackageName = resourceComponentSetting.YooAssetPackageName,
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

        /// <summary>
        /// Executes the YooAssets build and returns the actual output package directory.
        /// </summary>
        /// <param name="resourceComponentSetting">Resource component configuration.</param>
        /// <param name="buildParameters">Build parameters.</param>
        /// <returns>Resolved output package directory.</returns>
        private string ExecuteYooAssetBuild(
            ResourceComponentSetting resourceComponentSetting,
            ScriptableBuildParameters buildParameters)
        {
            bool uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            buildParameters.BuiltinShadersBundleName =
                packRuleResult.GetBundleName(resourceComponentSetting.YooAssetPackageName, uniqueBundleName);

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
