using System;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// Addressable 资源构建系统实现
    /// 负责使用 Unity Addressable 系统进行资源打包、增量更新和版本管理
    /// </summary>
    public class AddressableBuildSystem : IResourceBuildSystem
    {
        /// <summary>
        /// 构建资源
        /// 自己负责获取所需的 AddressableAssetSettings 和 GameSetting
        /// </summary>
        public void Build(BuildResourcesData buildResourcesData)
        {
            // 获取 Addressable 配置
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                throw new Exception("[AddressableBuildSystem] AddressableAssetSettings not found!");
            }

            // 获取 GameSetting
            var allSettings = Sirenix.Utilities.Editor.AssetUtilities.GetAllAssetsOfType<GameSetting>();
            var gameSetting = allSettings.FirstOrDefault();
            if (gameSetting == null)
            {
                throw new Exception("[AddressableBuildSystem] GameSetting not found in project!");
            }

            // 执行构建
            BuildInternal(buildResourcesData, settings, gameSetting);
        }

        /// <summary>
        /// 内部构建方法
        /// </summary>
        private void BuildInternal(BuildResourcesData buildResourcesData, AddressableAssetSettings settings, GameSetting gameSetting)
        {
            if (buildResourcesData.IsResourcesBuildIn)
            {
                BuildInPackage();
                return;
            }

            var buildPath = GetBuildPath(buildResourcesData);
            var loadPath = GetLoadPath(buildResourcesData);

            var exportPath = AddressableBuildHelper.GetExportPath();
            var exportAdsPath = AddressableBuildHelper.GetExportAdsPath();
            var exportAdsBinPath = AddressableBuildHelper.GetExportAdsBinPath(buildResourcesData);
            var exportBuildPath = AddressableBuildHelper.GetExportBuildPath(buildResourcesData);
            var exportVersionPath = AddressableBuildHelper.GetExportVersionPath(buildResourcesData);
            var debugExportVersionPath = AddressableBuildHelper.GetTempDebugExportVersionPath(buildResourcesData);

            var backupPath = AddressableBuildHelper.GetBackupPath(buildResourcesData);
            var backupAdsBinPath = AddressableBuildHelper.GetBackupAdsBinPath(buildResourcesData);
            var backupSeverDataPath = AddressableBuildHelper.GetBackupSeverDataBuildPath(buildResourcesData);
            var backupLastAssetsDataPath = GetBackupLastBuildPath(buildResourcesData);

            var assetAdsBinPath = GetAssetAdsBinPath(buildResourcesData);
            var assetAdsBinFilePath = GetAssetAdsBinFilePath(buildResourcesData);

            AddressableBuildHelper.SetSetting(settings, buildResourcesData, buildPath, loadPath);

            // 删除exportAds文件夹
            AddressableBuildHelper.DeleteDirectory(exportAdsPath);
            // 删除exportPath文件夹
            AddressableBuildHelper.DeleteDirectory(exportPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            // 刷新资源
            AddressableBuildHelper.AddressableRefresh();
            // 清空空的组
            AddressableBuildHelper.AddressableCleanEmptyGroup(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 创建backupPath
            AddressableBuildHelper.CreateDirectory(backupPath);
            if (buildResourcesData.BuildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("开始编译增量更新资源");
                // 删除assetAdsBinPath文件夹
                AddressableBuildHelper.DeleteDirectory(assetAdsBinPath);
                // 检查上个版本的adsBin文件夹 是否存在
                if (Directory.Exists(backupAdsBinPath))
                {
                    // 复制上个版本的adsBin文件夹 到 assetAdsBinPath文件夹
                    AddressableBuildHelper.CopyDirectory(backupAdsBinPath, assetAdsBinPath);
                    Debug.Log($"copy bin file {backupAdsBinPath} -> {assetAdsBinPath}");
                }

                if (!File.Exists(assetAdsBinFilePath))
                {
                    throw new Exception("找不到bin文件!");
                }

                // 检查之前的列表，需要更新的打包到小包
                AddressableBuildHelper.CheckForUpdateContent(backupPath, settings, buildResourcesData, gameSetting);
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
                var success = string.IsNullOrEmpty(result.Error);
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

            // 构建版本文件
            AddressableBuildHelper.GenerateUpdateFile(exportVersionPath, debugExportVersionPath, buildResourcesData);

            AddressableBuildHelper.DeleteDirectory(backupSeverDataPath);
            AddressableBuildHelper.CopyDirectory(exportBuildPath, backupSeverDataPath);

            // 资源更新
            if (buildResourcesData.BuildType == BuildType.ResourcesUpdate)
            {
                if (Directory.Exists(backupLastAssetsDataPath))
                {
                    // 可以在这里添加差异对比逻辑
                }
            }

            // Copy 到最新的打包文件用于和下一次打包对比
            AddressableBuildHelper.DeleteDirectory(backupLastAssetsDataPath);
            AddressableBuildHelper.CopyDirectory(exportBuildPath, backupLastAssetsDataPath);

            // 提示上传backupBuildPath
            Debug.Log($"Build Over ,Please upLoad = {backupSeverDataPath}, upload url = {loadPath}");
        }

        /// <summary>
        /// 构建内置资源包
        /// 自己负责获取所需的 AddressableAssetSettings
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
            AssetDatabase.Refresh();
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);
            var success = string.IsNullOrEmpty(result.Error);
            if (!success)
            {
                Debug.LogError("Addressables build error encountered: " + result.Error);
                Debug.LogError("Build Failed");
                return;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Build Over");
        }

        /// <summary>
        /// 获取构建路径
        /// </summary>
        public string GetBuildPath(BuildResourcesData data)
        {
            return AddressableBuildHelper.GetBuildPath(data);
        }

        /// <summary>
        /// 获取加载路径
        /// </summary>
        public string GetLoadPath(BuildResourcesData data)
        {
            var path = string.Empty;
            if (data.BuildResourcesServerModel == BuildResourcesServerModel.LocalHost)
            {
                path += AddressableBuildHelper.GetUrl(data.BuildResourcesServerModel);
            }
            else
            {
                path += AddressableBuildHelper.GetUrl(data.BuildResourcesServerModel) +
                        AddressableBuildHelper.GetFolderNameBasedOnAppVersion(data) + "/" +
                        AddressableBuildHelper.GetReplaceVersionName(data);
            }

            return path;
        }

        /// <summary>
        /// 获取备份最后构建路径
        /// </summary>
        private string GetBackupLastBuildPath(BuildResourcesData data)
        {
            string path = AddressableBuildHelper.GetBackupPath(data);
            return path + "/" + AddressableBuildHelper.GetChannelName(data) + "_" +
                   AddressableBuildHelper.BACKUP_LAST_NAME + "_" + data.BuildResourcesServerModel;
        }

        /// <summary>
        /// 获取资产 Addressable Bin 路径
        /// </summary>
        private string GetAssetAdsBinPath(BuildResourcesData data)
        {
            return Application.dataPath + "/AddressableAssetsData/" + data.BuilderTarget;
        }

        /// <summary>
        /// 获取资产 Addressable Bin 文件路径
        /// </summary>
        private string GetAssetAdsBinFilePath(BuildResourcesData data)
        {
            return GetAssetAdsBinPath(data) + "/addressables_content_state.bin";
        }
    }
}
