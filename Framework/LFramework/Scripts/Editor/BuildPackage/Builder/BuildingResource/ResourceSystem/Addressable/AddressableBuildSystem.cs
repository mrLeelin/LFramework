#if USE_ADDRESSABLE
using System;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// Addressables resource build system implementation.
    /// </summary>
    public class AddressableBuildSystem : IResourceBuildSystem
    {
        /// <summary>
        /// Builds resource content with the current Addressables configuration.
        /// </summary>
        /// <param name="buildResourcesData">Build settings.</param>
        public void Build(BuildSetting buildResourcesData)
        {
            var resourceComponentSetting =
                AssetUtilities.GetAllAssetsOfType<ResourceComponentSetting>().FirstOrDefault();
            if (resourceComponentSetting == null)
            {
                Debug.LogError("ResourceComponent is null in Build.");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new Exception("[AddressableBuildSystem] AddressableAssetSettings not found!");
            }

            var gameSetting = SettingManager.GetSetting<HybridCLRSetting>();
            if (gameSetting == null)
            {
                throw new Exception("[AddressableBuildSystem] GameSetting not found in project!");
            }

            BuildInternal(buildResourcesData, settings, gameSetting, resourceComponentSetting);
        }

        /// <summary>
        /// Builds Addressables content.
        /// </summary>
        private void BuildInternal(
            BuildSetting buildResourcesData,
            AddressableAssetSettings settings,
            HybridCLRSetting gameSetting,
            ResourceComponentSetting resourceComponentSetting)
        {
            if (buildResourcesData.isResourcesBuildIn)
            {
                BuildInPackage();
                return;
            }

            string buildPath = GetBuildPath(buildResourcesData);
            string loadPath = GetLoadPath(buildResourcesData);

            string exportPath = BuildResourcePathHelper.GetExportPath();
            string exportAdsPath = AddressableBuildHelper.GetExportAdsPath();
            string exportAdsBinPath = AddressableBuildHelper.GetExportAdsBinPath(buildResourcesData);
            string exportBuildPath = BuildResourcePathHelper.GetExportBuildPath(buildResourcesData);

            string backupPath = BuildResourcePathHelper.GetBackupPath(buildResourcesData);
            string backupAdsBinPath = AddressableBuildHelper.GetBackupAdsBinPath(buildResourcesData);
            string backupSeverDataPath = BuildResourcePathHelper.GetBackupSeverDataBuildPath(buildResourcesData);

            string assetAdsBinPath = GetAssetAdsBinPath(buildResourcesData);
            string assetAdsBinFilePath = GetAssetAdsBinFilePath(buildResourcesData);

            AddressableBuildHelper.SetSetting(
                settings,
                resourceComponentSetting,
                buildResourcesData,
                buildPath,
                loadPath);
            AddressableBuildHelper.EnsureBuildLayoutPreferences();
            AddressableBuildHelper.EnsurePlayerDataBuilder(settings);

            AddressableBuildHelper.DeleteDirectory(exportAdsPath);
            AddressableBuildHelper.DeleteDirectory(exportPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddressableBuildHelper.AddressableRefresh();
            AddressableBuildHelper.AddressableCleanEmptyGroup(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddressableBuildHelper.CreateDirectory(backupPath);
            if (buildResourcesData.buildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("Start content update build.");
                AddressableBuildHelper.DeleteDirectory(assetAdsBinPath);
                if (Directory.Exists(backupAdsBinPath))
                {
                    AddressableBuildHelper.CopyDirectory(backupAdsBinPath, assetAdsBinPath);
                    Debug.Log($"copy bin file {backupAdsBinPath} -> {assetAdsBinPath}");
                }

                if (!File.Exists(assetAdsBinFilePath))
                {
                    throw new Exception("Addressables content state bin file not found.");
                }

                AddressableBuildHelper.CheckForUpdateContent(
                    backupPath,
                    settings,
                    buildResourcesData,
                    gameSetting);
                string assetContentPath = ContentUpdateScript.GetContentStateDataPath(false);
                Debug.Log($"use bin file : {assetContentPath}");
                var result = ContentUpdateScript.BuildContentUpdate(settings, assetContentPath);
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Debug.LogError("Addressables build error encountered: " + result.Error);
                    Debug.LogError("Build Failed");
                    return;
                }

                AddressableBuildHelper.CopyReportToBackUp(buildResourcesData);
            }
            else
            {
                Debug.Log("build all resources");
                AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
                bool success = string.IsNullOrEmpty(result.Error);
                if (!success)
                {
                    Debug.LogError("Addressables build error encountered: " + result.Error);
                    Debug.LogError("Build Failed");
                    return;
                }

                AddressableBuildHelper.CopyReportToBackUp(buildResourcesData);
                AddressableBuildHelper.CopyDirectory(exportAdsBinPath, backupAdsBinPath);
                AddressableBuildHelper.CheckForRemoteResource(settings);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AddressableBuildHelper.DeleteDirectory(backupSeverDataPath);
            AddressableBuildHelper.CopyDirectory(exportBuildPath, backupSeverDataPath);
            BuildArtifactPostprocessHelper.ProcessBuildArtifacts(buildResourcesData, exportBuildPath);

            Debug.Log($"Build Over ,Please upLoad = {backupSeverDataPath}, upload url = {loadPath}");
        }

        /// <summary>
        /// Builds the built-in resource package.
        /// </summary>
        public void BuildInPackage()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new Exception("[AddressableBuildSystem] AddressableAssetSettings not found!");
            }

            AddressableBuildHelper.SetProfile(settings, "Default");
            settings.BuildRemoteCatalog = false;
            AddressableBuildHelper.EnsureBuildLayoutPreferences();
            AddressableBuildHelper.EnsurePlayerDataBuilder(settings);
            AssetDatabase.Refresh();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            bool success = string.IsNullOrEmpty(result.Error);
            if (!success)
            {
                Debug.LogError("Addressables build error encountered: " + result.Error);
                Debug.LogError("Build Failed");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log("Build Over");
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

        /// <summary>
        /// Gets the Addressables state-bin directory under Assets.
        /// </summary>
        private string GetAssetAdsBinPath(BuildSetting data)
        {
            return Application.dataPath + "/AddressableAssetsData/" + data.builderTarget;
        }

        /// <summary>
        /// Gets the Addressables state-bin file path.
        /// </summary>
        private string GetAssetAdsBinFilePath(BuildSetting data)
        {
            return GetAssetAdsBinPath(data) + "/addressables_content_state.bin";
        }
    }
}
#endif
