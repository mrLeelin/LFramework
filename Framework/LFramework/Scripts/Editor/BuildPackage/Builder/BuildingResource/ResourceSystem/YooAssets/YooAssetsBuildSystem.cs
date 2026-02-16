#if YOOASSET_SUPPORT
using System;
using System.IO;
using LFramework.Runtime;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// YooAssets 资源构建系统实现
    /// 负责使用 YooAssets 系统进行资源打包、增量更新和版本管理
    /// </summary>
    public class YooAssetsBuildSystem : IResourceBuildSystem
    {
        private const string SERVER_DATA_FOLDER_NAME = "ServerData";
        private const string BACKUP_FOLDER_NAME = "PartyGame_BackUp_BuildResource";
        private const string BACKUP_FILE_NAME = "Version";
        private const string Replace_Remote = "remote_";
        private const string Replace_Version = "_resource_version_";

        /// <summary>
        /// 构建资源
        /// YooAssets 系统不需要 AddressableAssetSettings
        /// </summary>
        public void Build(BuildSetting buildResourcesData)
        {
            Debug.Log("[YooAssets] 开始构建资源...");

            if (buildResourcesData.isResourcesBuildIn)
            {
                BuildInPackage();
                return;
            }

            var buildPath = GetBuildPath(buildResourcesData);
            var loadPath = GetLoadPath(buildResourcesData);

            Debug.Log($"[YooAssets] Build Path: {buildPath}");
            Debug.Log($"[YooAssets] Load Path: {loadPath}");

            var exportBuildPath = GetExportBuildPath(buildResourcesData);
            var backupPath = GetBackupPath(buildResourcesData);
            var backupSeverDataPath = GetBackupSeverDataBuildPath(buildResourcesData);

            // 创建备份目录
            CreateDirectory(backupPath);

            // TODO: 实现 YooAssets 构建逻辑
            // 1. 配置 YooAssets 构建参数
            // 2. 执行资源构建
            // 3. 处理增量更新（如果需要）
            // 4. 生成版本文件
            // 5. 备份构建结果

            if (buildResourcesData.buildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("[YooAssets] 开始编译增量更新资源");
                // TODO: 实现增量更新逻辑
            }
            else
            {
                Debug.Log("[YooAssets] 构建全量资源");
                // TODO: 实现全量构建逻辑
            }

            // 生成版本文件
            GenerateUpdateFile(buildResourcesData);

            // 备份构建结果
            DeleteDirectory(backupSeverDataPath);
            CopyDirectory(exportBuildPath, backupSeverDataPath);

            Debug.Log($"[YooAssets] Build Over, Please upload = {backupSeverDataPath}, upload url = {loadPath}");
        }

        /// <summary>
        /// 构建内置资源包
        /// YooAssets 系统不需要 AddressableAssetSettings
        /// </summary>
        public void BuildInPackage()
        {
            Debug.Log("[YooAssets] 开始构建内置资源包...");

            // TODO: 实现 YooAssets 内置资源构建逻辑
            // 1. 配置为内置模式
            // 2. 执行构建
            // 3. 将资源复制到 StreamingAssets

            AssetDatabase.Refresh();
            Debug.Log("[YooAssets] Build Over");
        }

        /// <summary>
        /// 获取构建路径
        /// </summary>
        public string GetBuildPath(BuildSetting data)
        {
            return SERVER_DATA_FOLDER_NAME + "/" + GetFolderNameBasedOnAppVersion(data) + "/" + GetFolderName(data);
        }

        /// <summary>
        /// 获取加载路径
        /// </summary>
        public string GetLoadPath(BuildSetting data)
        {
            var path = string.Empty;
            if (data.cdnType == CdnType.Local)
            {
                path += GetUrl(data.cdnType);
            }
            else
            {
                path += GetUrl(data.cdnType) + GetFolderNameBasedOnAppVersion(data) + "/" +
                        GetReplaceVersionName(data);
            }

            return path;
        }

        #region Private Helper Methods

        private void GenerateUpdateFile(BuildSetting buildResourcesData)
        {
            var exportVersionPath = GetExportVersionPath(buildResourcesData);

            if (File.Exists(exportVersionPath))
            {
                File.Delete(exportVersionPath);
            }

            var setting = new GameVersion
            {
                appVersion = buildResourcesData.appVersion,
            };
            var json = JsonUtility.ToJson(setting);
            File.WriteAllText(exportVersionPath, json);
        }

        #endregion

        #region Path Helper Methods

        private string GetChannelName(BuildSetting data)
        {
            string name = string.Empty;
            if (data == null) return name;
            switch (data.builderTarget)
            {
                case BuilderTarget.Windows:
                    name = data.windowsChannel.ToString();
                    break;
                case BuilderTarget.Android:
                    name = data.androidChannel.ToString();
                    break;
                case BuilderTarget.iOS:
                    name = data.iosChannel.ToString();
                    break;
            }

            return name;
        }

        private string GetUrl(CdnType cdnType)
        {
            string url = "";
            switch (cdnType)
            {
                case CdnType.Local:
                    url = "http://[PrivateIpAddress]:[HostingServicePort]";
                    break;
                default:
                    url = Replace_Remote;
                    break;
            }

            return url;
        }

        private string GetFolderName(BuildSetting data)
        {
            return GetChannelName(data) + "_" + data.resourcesVersion + "_" +
                   data.cdnType;
        }

        private string GetReplaceVersionName(BuildSetting data)
        {
            return GetChannelName(data) + "_" + Replace_Version + "_" +
                   data.cdnType;
        }

        private string GetFolderNameBasedOnAppVersion(BuildSetting data)
        {
            return GetChannelName(data) + "_" + data.appVersion + "_" +
                   data.cdnType;
        }

        private string GetExportPath()
        {
            return Application.dataPath + "/../" + SERVER_DATA_FOLDER_NAME;
        }

        private string GetExportBuildPath(BuildSetting data)
        {
            return Application.dataPath + "/../" + GetBuildPath(data);
        }

        private string GetExportVersionPath(BuildSetting data)
        {
            string path = GetExportBuildPath(data);
            return path + "/" + BACKUP_FILE_NAME;
        }

        private string GetBackupPath(BuildSetting data)
        {
            return Application.dataPath + "/../" + BACKUP_FOLDER_NAME + "/" + GetFolderNameBasedOnAppVersion(data);
        }

        private string GetBackupSeverDataBuildPath(BuildSetting data)
        {
            string path = GetBackupPath(data);
            return path + "/" + GetFolderName(data);
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
                    CopyDirectory(Path.Combine(from, directoryInfoArray[d].Name),
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
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// YooAssets 资源构建系统实现（未启用）
    /// 需要定义 YOOASSET_SUPPORT 宏才能使用此功能
    /// </summary>
    public class YooAssetsBuildSystem : IResourceBuildSystem
    {
        public void Build(BuildSetting buildResourcesData)
        {
            
        }

        public void BuildInPackage()
        {
         
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
