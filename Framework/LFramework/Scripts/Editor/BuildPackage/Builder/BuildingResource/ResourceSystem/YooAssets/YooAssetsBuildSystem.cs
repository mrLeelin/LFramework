#if YOOASSET_SUPPORT
using System;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEditor.Build.Pipeline.Utilities;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityGameFramework.Runtime;
using YooAsset;
using YooAsset.Editor;
using BuildResult = YooAsset.Editor.BuildResult;
using ScriptableBuildPipeline = YooAsset.Editor.ScriptableBuildPipeline;

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
        /// </summary>
        public void Build(BuildSetting buildResourcesData)
        {
            Debug.Log("[YooAssets] 开始构建资源...");
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

            var buildPath = GetBuildPath(buildResourcesData);
            var loadPath = GetLoadPath(buildResourcesData);

            Debug.Log($"[YooAssets] Build Path: {buildPath}");
            Debug.Log($"[YooAssets] Load Path: {loadPath}");

            var exportBuildPath = GetExportBuildPath(buildResourcesData);
            var backupPath = GetBackupPath(buildResourcesData);
            var backupSeverDataPath = GetBackupSeverDataBuildPath(buildResourcesData);

            // 创建备份目录
            CreateDirectory(backupPath);

            // 构建 YooAsset 资源
            string outputRoot = GetYooAssetOutputRoot();
            string buildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();
            var param = BuildScriptableBuildParameters(resourceComponentSetting, outputRoot, buildinFileRoot,
                buildResourcesData.resourcesVersion);
            if (buildResourcesData.buildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("[YooAssets] 开始编译增量更新资源");
                // 增量更新：使用 ScriptableBuildPipeline，不清除缓存
                param.ClearBuildCacheFiles = false;
                param.BuildinFileCopyOption = EBuildinFileCopyOption.None;
                ExecuteYooAssetBuild(
                    resourceComponentSetting,
                    param);
            }
            else
            {
                Debug.Log("[YooAssets] 构建全量资源");
                // 全量构建：清除缓存，复制内置文件
                param.ClearBuildCacheFiles = true;
                //TODO 根据标签copy

                param.BuildinFileCopyParams = SettingManager.GetSetting<HybridCLRSetting>().defaultInitLabel;
                if (string.IsNullOrEmpty(param.BuildinFileCopyParams))
                {
                    param.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
                }
                else
                {
                    param.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyByTags;
                }

                ExecuteYooAssetBuild(
                    resourceComponentSetting,
                    param);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 将 YooAsset 构建产物复制到项目的 ServerData 目录
            string yooAssetOutputDir = GetYooAssetPackageOutputDir(resourceComponentSetting, outputRoot,
                buildResourcesData.resourcesVersion);
            if (Directory.Exists(yooAssetOutputDir))
            {
                CreateDirectory(exportBuildPath);
                CopyDirectory(yooAssetOutputDir, exportBuildPath);
                Debug.Log($"[YooAssets] 已复制构建产物: {yooAssetOutputDir} -> {exportBuildPath}");
            }
            else
            {
                Debug.LogWarning($"[YooAssets] 构建产物目录不存在: {yooAssetOutputDir}");
            }

            // 生成版本文件
            GenerateUpdateFile(buildResourcesData);

            // 备份构建结果
            DeleteDirectory(backupSeverDataPath);
            CopyDirectory(exportBuildPath, backupSeverDataPath);

            Debug.Log($"[YooAssets] Build Over, Please upload = {backupSeverDataPath}, upload url = {loadPath}");
        }

        public void BuildInPackage()
        {
        }

        /// <summary>
        /// 构建内置资源包
        /// 将资源打包到 StreamingAssets，不支持热更新
        /// </summary>
        private void BuildInPackage(ResourceComponentSetting resourceComponentSetting)
        {
            Debug.Log("[YooAssets] 开始构建内置资源包...");

            string outputRoot = GetYooAssetOutputRoot();
            string buildinFileRoot = AssetBundleBuilderHelper.GetStreamingAssetsRoot();

            var param = BuildScriptableBuildParameters(resourceComponentSetting, outputRoot, buildinFileRoot,
                "buildin");
            param.ClearBuildCacheFiles = true;
            param.BuildinFileCopyOption = EBuildinFileCopyOption.ClearAndCopyAll;
            ExecuteYooAssetBuild(
                resourceComponentSetting,
                param);

            AssetDatabase.SaveAssets();
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

        #region YooAsset Build Core

        private ScriptableBuildParameters BuildScriptableBuildParameters(
            ResourceComponentSetting resourceComponentSetting,
            string outputRoot,
            string buildinFileRoot,
            string packageVersion
        )
        {
            var buildParameters = new ScriptableBuildParameters
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
            return buildParameters;
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
        /// 执行 YooAsset 构建
        /// 使用 ScriptableBuildPipeline 进行 AssetBundle 构建
        /// </summary>
        private void ExecuteYooAssetBuild(ResourceComponentSetting resourceComponentSetting,
            ScriptableBuildParameters buildParameters)
        {
            // 内置着色器资源包名称
            var uniqueBundleName = AssetBundleCollectorSettingData.Setting.UniqueBundleName;
            var packRuleResult = DefaultPackRule.CreateShadersPackRuleResult();
            buildParameters.BuiltinShadersBundleName =
                packRuleResult.GetBundleName(resourceComponentSetting.YooAssetPackageName, uniqueBundleName);

            var pipeline = new ScriptableBuildPipeline();
            BuildResult buildResult = pipeline.Run(buildParameters, true);

            if (buildResult.Success)
            {
                Debug.Log($"[YooAssets] 构建成功，输出目录: {buildResult.OutputPackageDirectory}");
            }
            else
            {
                Debug.LogError($"[YooAssets] 构建失败: {buildResult.ErrorInfo}");
                throw new Exception($"[YooAssets] Build failed: {buildResult.ErrorInfo}");
            }
        }

        /// <summary>
        /// 获取 YooAsset 默认输出根目录
        /// </summary>
        private string GetYooAssetOutputRoot()
        {
            return AssetBundleBuilderHelper.GetDefaultBuildOutputRoot();
        }

        /// <summary>
        /// 获取 YooAsset 构建产物的包输出目录
        /// 路径格式: {outputRoot}/{BuildTarget}/{PackageName}/{PackageVersion}
        /// </summary>
        private string GetYooAssetPackageOutputDir(ResourceComponentSetting resourceComponentSetting, string outputRoot,
            string packageVersion)
        {
            string buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString();
            return Path.Combine(outputRoot, buildTarget, resourceComponentSetting.YooAssetPackageName, packageVersion);
        }

        #endregion

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
            Debug.LogWarning("[YooAssets] YOOASSET_SUPPORT 未定义，无法构建资源。请在 Player Settings -> Scripting Define Symbols 中添加 YOOASSET_SUPPORT。");
        }

        public void BuildInPackage()
        {
            Debug.LogWarning("[YooAssets] YOOASSET_SUPPORT 未定义，无法构建内置资源包。");
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