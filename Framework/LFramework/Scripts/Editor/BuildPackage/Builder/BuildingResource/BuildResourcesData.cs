using System;
using System.IO;
using System.Linq;
using LFramework.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder
{
    public class BuildResourcesData
    {
        public BuildResourcesData()
        {
            BuilderTarget = BuilderTarget.Windows;
            ResourceSystem = BuildingResource.ResourceSystemType.Addressable; // 默认使用 Addressable
#if UNITY_ANDROID
            BuilderTarget = BuilderTarget.Android;
#elif UNITY_IOS
            BuilderTarget = BuilderTarget.iOS;
#endif
        }

        [Title("资源系统选择", null, TitleAlignments.Split, false)]
        public BuildingResource.ResourceSystemType ResourceSystem;

        public BuilderTarget BuilderTarget;

        [ShowIf("BuilderTarget", BuilderTarget.iOS)]
        public BuildIOSChannel IOSChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Windows)]
        public BuildWindowsChannel WindowsChannel;

        [ShowIf("BuilderTarget", BuilderTarget.Android)]
        public BuildAndroidChannel AndroidChannel;

        /// <summary>
        /// 0.0.0.1
        /// </summary>
        [Header("母包版本")] public string AppVersion;

        public string ResourcesVersion;
        public bool IsResourcesBuildIn;

        [Title("是否打包热更Dll", null, TitleAlignments.Split, false)]
        public bool IsBuildDll;

        [HideIf("IsResourcesBuildIn")] public bool IsForceUpdate;
        [HideIf("IsResourcesBuildIn")] public BuildType BuildType;
        [HideIf("IsResourcesBuildIn")] public BuildResourcesServerModel BuildResourcesServerModel;

        [InfoBox("点击按钮打包")]
        [Button("打包")]
        public void Build()
        {
            BuildResourcesData.Build(this);
        }

        /// <summary>
        /// 构建资源
        /// </summary>
        public static void Build(BuildResourcesData buildResourcesData)
        {
            Debug.Log($"The build active target is '{EditorUserBuildSettings.activeBuildTarget}'");
            if (buildResourcesData == null)
            {
                return;
            }

            // 检查资源系统是否受支持
            if (!BuildingResource.ResourceBuildSystemFactory.IsSupported(buildResourcesData.ResourceSystem))
            {
                Debug.LogError($"Resource system '{buildResourcesData.ResourceSystem}' is not supported. Please check your configuration.");
                return;
            }

            Debug.Log($"Using resource system: {BuildingResource.ResourceBuildSystemFactory.GetDisplayName(buildResourcesData.ResourceSystem)}");

            SetBuildTarget(BuildPackageWindow.ConvertToBuilderTarget(buildResourcesData.BuilderTarget));
            var allSettings = AssetUtilities.GetAllAssetsOfType<GameSetting>();
            var gameSetting = allSettings.FirstOrDefault();
            if (gameSetting == null)
            {
                Log.Fatal("GameSetting not found in project!");
                return;
            }

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (buildResourcesData.IsBuildDll)
            {
                if (!BuildDllsHelper.BuildDll(buildResourcesData.BuildType == BuildType.APP,
                        GetBackupPath(buildResourcesData)))
                {
                    throw new Exception("[BuildResourcesData] Build dlls error.");
                }
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                if (!BuildDllsHelper.CopyDll(buildResourcesData,
                        settings,
                        gameSetting,
                        GetBackupPath(buildResourcesData)))
                {
                    Debug.LogError("Copy Hotfix dll error.");
                    throw new Exception("Copy Hotfix dll error.");
                }
            }

            // 使用工厂模式创建资源构建系统
            var buildSystem = BuildingResource.ResourceBuildSystemFactory.Create(buildResourcesData.ResourceSystem);
            buildSystem.Build(buildResourcesData, settings, gameSetting);
        }

        /// <summary>
        /// 设置构建目标平台
        /// </summary>
        private static void SetBuildTarget(BuildTarget newBuildTarget)
        {
            if (newBuildTarget == EditorUserBuildSettings.activeBuildTarget)
            {
                Debug.Log($"NewBuildTarget equal OldBuildTarget  ===  '{newBuildTarget}' ");
                return;
            }

            var targetGroup = BuildPipeline.GetBuildTargetGroup(newBuildTarget);
            var success = EditorUserBuildSettings.SwitchActiveBuildTarget(targetGroup, newBuildTarget);
            if (success)
            {
                Debug.Log($"Set Build Target '{newBuildTarget}' successful.");
            }
            else
            {
                throw new Exception($"Set Build Target '{newBuildTarget}' error.");
            }
        }

        /// <summary>
        /// 获取渠道名称
        /// </summary>
        public static string GetChannelName(BuildResourcesData data)
        {
            return AddressableBuildHelper.GetChannelName(data);
        }

        /// <summary>
        /// 生成热更新列表
        /// </summary>
        private static void GenerateHotUpdateList(BuildResourcesData buildResourcesData)
        {
            var hotUpdateConfigPath = GetHotUpdateConfigPath();
            var hotUpdateConfigBackupPath = GetHotUpdateConfigBackupPath(buildResourcesData);
            if (File.Exists(hotUpdateConfigPath))
            {
                if (File.Exists(hotUpdateConfigBackupPath))
                {
                    File.Delete(hotUpdateConfigBackupPath);
                }

                File.Copy(hotUpdateConfigPath, hotUpdateConfigBackupPath);
                File.Delete(hotUpdateConfigPath);
            }
        }

        /// <summary>
        /// 生成强制更新标记
        /// </summary>
        private static void GenerateForce(BuildResourcesData buildResourcesData)
        {
            var isForcePath = GetHotUpdateIsForceBackupPath(buildResourcesData);
            var isForceContent = buildResourcesData.IsResourcesBuildIn ? "1" : "0";
            File.WriteAllText(isForcePath, isForceContent);
        }

        /// <summary>
        /// 获取上次构建版本
        /// </summary>
        private static string GetLastBuildVersion(BuildResourcesData buildResourcesData)
        {
            var filePath = GetLastVersionPath(buildResourcesData);
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                var gameVersion = JsonUtility.FromJson<GameVersion>(json);
                return gameVersion.appVersion;
            }

            return string.Empty;
        }

        /// <summary>
        /// 构建内置资源包
        /// </summary>
        private static void BuildInPackageResource(AddressableAssetSettings settings)
        {
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
        /// 设置构建器
        /// </summary>
        private static void SetBuilder(AddressableAssetSettings settings, IDataBuilder builder)
        {
            int index = settings.DataBuilders.IndexOf((ScriptableObject)builder);

            if (index > 0)
                settings.ActivePlayerDataBuilderIndex = index;
            else
                Debug.LogWarning($"{builder} must be added to the " +
                                 $"DataBuilders list before it can be made " +
                                 $"active. Using last run builder instead.");
        }

        #region Path Helper Methods

        /// <summary>
        /// 获取热更新配置路径
        /// </summary>
        private static string GetHotUpdateConfigPath()
        {
            return AddressableBuildHelper.GetHotUpdateConfigPath();
        }

        /// <summary>
        /// 获取热更新配置备份路径
        /// </summary>
        private static string GetHotUpdateConfigBackupPath(BuildResourcesData data)
        {
            var rootDir = GetBackupPath(data);
            var subDir = AddressableBuildHelper.GetFolderName(data);
            return Path.Combine(rootDir, subDir, "hotUpdateConfig.json");
        }

        /// <summary>
        /// 获取热更新强制标记备份路径
        /// </summary>
        private static string GetHotUpdateIsForceBackupPath(BuildResourcesData data)
        {
            var rootDir = GetBackupPath(data);
            var subDir = AddressableBuildHelper.GetFolderName(data);
            return Path.Combine(rootDir, subDir, "isForce");
        }

        /// <summary>
        /// 获取备份路径
        /// </summary>
        private static string GetBackupPath(BuildResourcesData data)
        {
            return AddressableBuildHelper.GetBackupPath(data);
        }

        /// <summary>
        /// 获取备份服务器数据差异路径
        /// </summary>
        private static string GetBackupServerDataDiffPath(string lastVersion, BuildResourcesData data)
        {
            string path = GetBackupPath(data);
            return path + "/" + AddressableBuildHelper.GetFolderName(data) + $"_diff_{lastVersion}";
        }

        /// <summary>
        /// 获取备份版本文件夹路径
        /// </summary>
        private static string GetBackupVersionFolderPath(BuildResourcesData data)
        {
            return Application.dataPath + "/../" + AddressableBuildHelper.BACKUP_FOLDER_NAME + "/" +
                   AddressableBuildHelper.GetChannelName(data) + "_" +
                   data.BuildResourcesServerModel;
        }

        /// <summary>
        /// 获取上次版本路径
        /// </summary>
        private static string GetLastVersionPath(BuildResourcesData data)
        {
            string path = GetBackupLastBuildPath(data);
            return path + "/" + AddressableBuildHelper.BACKUP_FILE_NAME;
        }

        /// <summary>
        /// 获取备份版本路径
        /// </summary>
        private static string GetBackupVersionPath(BuildResourcesData data)
        {
            return AddressableBuildHelper.GetBackupSeverDataBuildPath(data) + "/" +
                   AddressableBuildHelper.BACKUP_FILE_NAME;
        }

        /// <summary>
        /// 获取备份最后构建路径
        /// </summary>
        public static string GetBackupLastBuildPath(BuildResourcesData data)
        {
            string path = GetBackupPath(data);
            return path + "/" + AddressableBuildHelper.GetChannelName(data) + "_" +
                   AddressableBuildHelper.BACKUP_LAST_NAME + "_" + data.BuildResourcesServerModel;
        }

        /// <summary>
        /// 获取资产 Addressable Bin 路径
        /// </summary>
        public static string GetAssetAdsBinPath(BuildResourcesData data)
        {
            return Application.dataPath + "/AddressableAssetsData/" + data.BuilderTarget;
        }

        /// <summary>
        /// 获取资产 Addressable Bin 文件路径
        /// </summary>
        public static string GetAssetAdsBinFilePath(BuildResourcesData data)
        {
            return GetAssetAdsBinPath(data) + "/addressables_content_state.bin";
        }

        #endregion
    }
}
