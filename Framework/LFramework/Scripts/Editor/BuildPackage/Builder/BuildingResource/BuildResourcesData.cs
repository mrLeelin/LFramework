using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LFramework.Runtime;
using Sirenix.OdinInspector;
using Sirenix.Utilities.Editor;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Compilation;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder
{
    public class BuildResourcesData
    {
        public BuildResourcesData()
        {
            BuilderTarget = BuilderTarget.Windows;
#if UNITY_ANDROID
            BuilderTarget = BuilderTarget.Android;
#elif UNITY_IOS
            BuilderTarget = BuilderTarget.iOS;
#endif
        }

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
        [HideIf("IsResourcesBuildIn")] public BuildResourcesSeverModel BuildResourcesSeverModel;

        [InfoBox("点击按钮打包")]
        [Button("打包")]
        public void Build()
        {
            BuildResourcesData.Build(this);
        }


        private const string SERVER_DATA_FOLDER_NAME = "ServerData";
        private const string BACKUP_FOLDER_NAME = "PartyGame_BackUp_BuildResource";
        private const string BACKUP_LAST_NAME = "LastBuild";
        private const string BACKUP_FILE_NAME = "Version";
        private const string Last_Report_File_Name = "LastBuildReport.json";
        private const string Replace_Remote = "remote_";
        private const string Replace_Version = "_resource_version_";


        public static void Build(BuildResourcesData buildResourcesData)
        {
            Debug.Log($"The build active target is '{EditorUserBuildSettings.activeBuildTarget}'");
            if (buildResourcesData == null)
            {
                return;
            }

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
            BuildAddressable(buildResourcesData, settings,gameSetting);
        }

        private static void BuildAddressable(BuildResourcesData buildResourcesData, AddressableAssetSettings settings,GameSetting gameSetting)
        {
            if (buildResourcesData.IsResourcesBuildIn)
            {
                BuildInPackageResource(settings);
                return;
            }


            var buildPath = GetBuildPath(buildResourcesData);
            var loadPath = GetLoadPath(buildResourcesData);

            var exportPath = GetExportPath();
            var exportAdsPath = GetExportAdsPath();
            var exportAdsBinPath = GetExportAdsBinPath(buildResourcesData);
            var exportBuildPath = GetExportBuildPath(buildResourcesData);
            var exportVersionPath = GetExportVersionPath(buildResourcesData);
            var debugExportVersionPath = GetTempDebugExportVersionPath(buildResourcesData);

            var backupPath = GetBackupPath(buildResourcesData);
            var backupAdsBinPath = GetBackupAdsBinPath(buildResourcesData);
            var backupSeverDataPath = GetBackupSeverDataBuildPath(buildResourcesData);
            //var backupVersionFolderPath = GetBackupVersionFolderPath(buildResourcesData);
            //var backupVersionPath = GetBackupVersionPath(buildResourcesData);
            var backupLastAssetsDataPath = GetBackupLastBuildPath(buildResourcesData);

            // string lastBackupAdsPath = GetLastBackupAdsPath(data);
            // string lastBackupAdsBinPath = GetLastBackupAdsBinPath(data);

            var assetAdsBinPath = GetAssetAdsBinPath(buildResourcesData);
            var assetAdsBinFilePath = GetAssetAdsBinFilePath(buildResourcesData);

            SetSetting(settings, buildResourcesData, buildPath, loadPath);

            //删除exportAds文件夹
            DeleteDirectory(exportAdsPath);
            //删除exportPath文件夹
            DeleteDirectory(exportPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            //刷新资源
            AddressableRefresh();
            //清空空的组
            AddressableCleanEmptyGroup(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //创建backupPath
            CreateDirectory(backupPath);
            if (buildResourcesData.BuildType == BuildType.ResourcesUpdate)
            {
                Debug.Log("开始编译增量更新资源");
                //删除assetAdsBinPath文件夹
                DeleteDirectory(assetAdsBinPath);
                //检查上个版本的adsBin文件夹 是否存在
                if (Directory.Exists(backupAdsBinPath))
                {
                    //复制上个版本的adsBin文件夹 到 assetAdsBinPath文件夹
                    CopyDirectory(backupAdsBinPath, assetAdsBinPath);
                    Debug.Log($"copy bin file {backupAdsBinPath} -> {assetAdsBinPath}");
                }

                if (!File.Exists(assetAdsBinFilePath))
                {
                    throw new Exception("找不到bin文件!");
                }

                //检查之前的列表，需要更新的打包到小包
                CheckForUpdateContent(backupPath, settings, buildResourcesData, gameSetting);
                // CheckForUpdateContent(settings);
                string assetContentPath = ContentUpdateScript.GetContentStateDataPath(false);
                Debug.Log($"use bin file : {assetContentPath}");
                var result = ContentUpdateScript.BuildContentUpdate(settings, assetContentPath);
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Debug.LogError("Addressables build error encountered: " + result.Error);
                    Debug.LogError("Build Failed");
                    return;
                }

                CopyReportToBackUp(buildResourcesData);
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

                CopyReportToBackUp(buildResourcesData);
                CopyDirectory(exportAdsBinPath, backupAdsBinPath);
                CheckForRemoteResource(settings);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            //构建版本文件
            GenerateUpdateFile(exportVersionPath, debugExportVersionPath, buildResourcesData);

            DeleteDirectory(backupSeverDataPath);
            CopyDirectory(exportBuildPath, backupSeverDataPath);
            //拷贝HotUpdate清单文件到backupBuildPath
            //GenerateHotUpdateList(buildResourcesData);

            // 是否是强更新
            //GenerateForce(buildResourcesData);


            //资源更新
            if (buildResourcesData.BuildType == BuildType.ResourcesUpdate)
            {
                if (Directory.Exists(backupLastAssetsDataPath))
                {
                    /*
                    var lastBuildResourceVersion = GetLastBuildVersion(buildResourcesData);
                    var diffPath = GetBackupServerDataDiffPath(lastBuildResourceVersion, buildResourcesData);
                    DeleteDirectory(diffPath);
                    //拷贝buildPath 到 backupSeverDataPath
                    FolderComparer.CompareAndCopyDifferentFiles(backupSeverDataPath, backupLastAssetsDataPath,
                        diffPath
                    );
                    */
                }
            }

            //Coby 到最新的打包文件用于和下一次打包对比
            DeleteDirectory(backupLastAssetsDataPath);
            CopyDirectory(exportBuildPath, backupLastAssetsDataPath);

            //提示上传backupBuildPath
            Debug.Log($"Build Over ,Please upLoad = {backupSeverDataPath}, upload url = {loadPath}");
        }

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
                //Self 拷贝之后删除
                File.Delete(hotUpdateConfigPath);
            }
        }

        private static void GenerateForce(BuildResourcesData buildResourcesData)
        {
            var isForcePath = GetHotUpdateIsForceBackupPath(buildResourcesData);
            var isForceContent = buildResourcesData.IsResourcesBuildIn ? "1" : "0";
            File.WriteAllText(isForcePath, isForceContent);
        }

        private static string GetLastBuildVersion(BuildResourcesData buildResourcesData)
        {
            var filePath = GetLastVersionPath(buildResourcesData);
            if (File.Exists(filePath))
            {
                var json = JsonUtility.FromJson<GameVersion>(File.ReadAllText(filePath));
                return "";
            }

            return string.Empty;
        }

        /// <summary>
        /// 更新文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="debugFilePath"></param>
        /// <param name="buildResourcesData"></param>
        private static void GenerateUpdateFile(string filePath, string debugFilePath,
            BuildResourcesData buildResourcesData)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (File.Exists(debugFilePath))
            {
                File.Delete(debugFilePath);
            }

            var setting = new GameVersion
            {
                appVersion = buildResourcesData.AppVersion,
            };
            var json = JsonUtility.ToJson(setting);
            File.WriteAllText(filePath, json);
            var dirPath = Path.GetDirectoryName(debugFilePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            File.Copy(filePath, debugFilePath);
        }

        private static void BuildInPackageResource(AddressableAssetSettings settings)
        {
            SetProfile(settings, "Default");
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
            return;
        }

        private static void SetSetting(AddressableAssetSettings settings, BuildResourcesData buildResourcesData,
            string buildPath, string loadPath)
        {
            var dynamicProfileId = SetProfile(settings, "Dynamic");
            settings.profileSettings.SetValue(dynamicProfileId, AddressableAssetSettings.kRemoteBuildPath, buildPath);
            settings.profileSettings.SetValue(dynamicProfileId, AddressableAssetSettings.kRemoteLoadPath, loadPath);
            settings.BuildRemoteCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.OverridePlayerVersion = buildResourcesData.AppVersion;
            AddressableAssetSettings.CleanPlayerContent();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string SetProfile(AddressableAssetSettings settings, string profile)
        {
            string profileId = settings.profileSettings.GetProfileId(profile);
            if (String.IsNullOrEmpty(profileId))
                Debug.LogWarning($"Couldn't find a profile named, {profile}, " +
                                 $"using current profile instead.");
            else
                settings.activeProfileId = profileId;
            return profileId;
        }

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

        private static void AddressableRefresh()
        {
            //刷新目录结构
            /*
            AddressableImporter.FolderImporter.ReimportFolders(new string[] { "Assets" }, false);
            */
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private static void CheckForRemoteResource(AddressableAssetSettings settings)
        {
            var updateConfig = new HotUpdateConfig
            {
                hotUpdateList = new List<string>()
            };
            var groups = settings.groups;
            foreach (var assetGroup in groups)
            {
                var groupSchema = assetGroup.GetSchema<BundledAssetGroupSchema>();
                if (groupSchema == null)
                {
                    continue;
                }

                var loadPath = groupSchema.LoadPath.GetValue(settings);
                if (loadPath.StartsWith("http"))
                {
                    foreach (var entry in assetGroup.entries)
                    {
                        updateConfig.hotUpdateList.Add(entry.address);
                    }
                }
            }

            var updateKeys = JsonUtility.ToJson(updateConfig);
            File.WriteAllText(GetHotUpdateConfigPath(), updateKeys);
        }

        private static void AddressableCleanEmptyGroup(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            var dirty = false;
            var emptyGroups = settings.groups.Where(x => x != null && x.entries.Count == 0 && !x.IsDefaultGroup())
                .ToArray();
            for (var i = 0; i < emptyGroups.Length; i++)
            {
                dirty = true;
                settings.RemoveGroup(emptyGroups[i]);
            }

            if (dirty)
            {
                AssetDatabase.SaveAssets();
            }

            AssetDatabase.Refresh();
        }

        private static void CheckForUpdateContent(string backUpPath, AddressableAssetSettings settings,
            BuildResourcesData data, GameSetting gameSetting)
        {
            var reportPath = backUpPath + "/" + Last_Report_File_Name; //Path.Combine(Application.dataPath, ".json");
            Debug.Log($"reportPath:{reportPath}");
            var buildLayout = BuildLayout.Open(reportPath, true, true);
            var guidExplicitAssets = new SortedDictionary<string, BuildLayout.ExplicitAsset>();
            foreach (BuildLayout.ExplicitAsset asset in BuildLayoutHelpers.EnumerateAssets(buildLayout))
            {
                guidExplicitAssets.Add(asset.Guid, asset);
            }

            string buildPath = ContentUpdateScript.GetContentStateDataPath(false);
            List<AddressableAssetEntry> entries = ContentUpdateScript.GatherModifiedEntries(settings, buildPath);
            if (entries.Count == 0)
            {
                return;
            }

            var keyList = new List<string>();
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Need Update Assets:");
            foreach (var entry in entries)
            {
                builder.AppendLine(entry.address);
                keyList.Add(entry.address);
            }

            var updateConfig = new HotUpdateConfig
            {
                hotUpdateList = keyList
            };
            var updateKeys = JsonUtility.ToJson(updateConfig);
            File.WriteAllText(GetHotUpdateConfigPath(), updateKeys);
            Debug.Log(builder.ToString());

            // 国内网络环境较好4mb一个组,国际版有些地区网络环境较差3mb一个组
#if CHANNEL_CN
        const float maxGroupSize = 4 * 1024 * 1024; // 4mb
#else
            const float maxGroupSize = 2.5f * 1024 * 1024; // 2mb
#endif
            const float singleGroupSize = 1 * 1024 * 1024; // 1mb

            StringBuilder sb = new StringBuilder();
            //将被修改过的资源分组
            var groupPrefix = $"UpdateGroup_{DateTime.Now:yyyyMMddhhmmss}";
            var curIndex = 1;
            var curGroupName = $"{groupPrefix}_{curIndex}";
            var curGroupSize = 0UL;
            var allEntriesSet = new HashSet<AddressableAssetEntry>();
            var curUpdateGroupEntries = new List<AddressableAssetEntry>();
            var len = entries.Count;
            for (var n = 0; n < len; n++)
            {
                var entry = entries[n];
                ulong assetsSize = 0L;
                if (guidExplicitAssets.TryGetValue(entry.guid, out var asset))
                {
                    assetsSize = asset.SerializedSize + asset.StreamedSize;
                }
                else
                {
                    Log.Error($"未找到热更文件的大小! path={entry.AssetPath}");
                }

                Debug.Log("Entry : " + entry.AssetPath);
                //通过Update生成的资源全部打上init label
                entry.SetLabel(gameSetting.hybridClrSetting.defaultInitLabel, true, true);

                // 对于图集来说size根本不准,只能图集单独创建资源组
                if (entry.AssetPath.EndsWith(".spriteatlas"))
                {
                    CreateAtlasUpdateGroup("spriteatlas", settings, entry, curIndex, groupPrefix, sb);
                    curIndex++;
                    allEntriesSet.Add(entry);
                    continue;
                }

                if (entry.AssetPath.EndsWith(".spriteatlasv2"))
                {
                    CreateAtlasUpdateGroup("spriteatlasv2", settings, entry, curIndex, groupPrefix, sb);
                    curIndex++;
                    allEntriesSet.Add(entry);
                    continue;
                }

                // 判断单个文件是不是比较大 如果超过了1MB
                var size = assetsSize;
                if (size >= singleGroupSize)
                {
                    CreateSingleUpdateGroup(size, settings, entry, curIndex, groupPrefix, sb);
                    curIndex++;
                    allEntriesSet.Add(entry);
                    continue;
                }

                // 增加后就超过2MB,以之前的所有资源创建组,不包含这个超过的组
                if (curGroupSize + size >= maxGroupSize)
                {
                    CreateMaxSizeUpdateGroup(curIndex, settings, curUpdateGroupEntries, curGroupSize, groupPrefix,
                        sb);
                    curIndex++;
                    curGroupSize = 0;
                    curUpdateGroupEntries.Clear();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }

                curGroupSize += size;
                curUpdateGroupEntries.Add(entry);
                allEntriesSet.Add(entry);
                sb.AppendLine($"curGroupSize={curGroupSize}, addSize={size} assetPath={entry.AssetPath} ");
            }

            if (curUpdateGroupEntries.Count > 0)
            {
                // 处理最后一组
                CreateLastUpdateGroup(settings, curUpdateGroupEntries, curGroupSize, groupPrefix, sb);
                curUpdateGroupEntries.Clear();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            // 验证是否所有entry都进入热更了
            foreach (var entry in entries)
            {
                if (!allEntriesSet.Contains(entry))
                {
                    throw new Exception($"检查到未进入热更的资源, path={entry.AssetPath}");
                }
            }

            var log = sb.ToString();
            var backupPath = GetBackupPath(data);
            File.WriteAllText(Path.Combine(backupPath, "hotfixGroup.log"), log);
            buildLayout.Close();
        }

        private static void CreateAtlasUpdateGroup(string extension, AddressableAssetSettings settings,
            AddressableAssetEntry entry, int index, string groupPrefix, StringBuilder sb)
        {
            var atlasName = Path.GetFileNameWithoutExtension(entry.AssetPath);
            var spriteGroupName = $"{groupPrefix}_{extension}_{atlasName}_{index}";
            sb.AppendLine(
                $"-----------create {extension} group name={spriteGroupName} assetPath={entry.AssetPath}");
            GenerateUpdateGroup(spriteGroupName, settings, new List<AddressableAssetEntry>() { entry });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateSingleUpdateGroup(ulong size, AddressableAssetSettings settings,
            AddressableAssetEntry entry, int index, string groupPrefix, StringBuilder sb)
        {
            var singleFileName = Path.GetFileNameWithoutExtension(entry.AssetPath);
            var singleGroupName = $"{groupPrefix}_single_{singleFileName}_{index}";
            sb.AppendLine(
                $"-----------create single group name={singleGroupName} groupSize={size} assetPath={entry.AssetPath}");
            GenerateUpdateGroup(singleGroupName, settings, new List<AddressableAssetEntry>() { entry });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateMaxSizeUpdateGroup(int index, AddressableAssetSettings settings,
            List<AddressableAssetEntry> curUpdateGroupEntries, ulong curGroupSize, string groupPrefix,
            StringBuilder sb)
        {
            var groupName = $"{groupPrefix}_{index}";
            sb.AppendLine($"-----------create normal group name={groupName} groupSize={curGroupSize}");
            GenerateUpdateGroup(groupName, settings, curUpdateGroupEntries);
        }

        private static void CreateLastUpdateGroup(AddressableAssetSettings settings,
            List<AddressableAssetEntry> curUpdateGroupEntries, ulong curGroupSize, string groupPrefix,
            StringBuilder sb)
        {
            var groupName = $"{groupPrefix}_Last";
            sb.AppendLine($"-----------create last group name={groupName} groupSize={curGroupSize}");
            GenerateUpdateGroup(groupName, settings, curUpdateGroupEntries);
        }

        private static void GenerateUpdateGroup(string groupName, AddressableAssetSettings settings,
            List<AddressableAssetEntry> entries)
        {
            AddressableHelper.GenerateUpdateGroup(groupName, settings, entries, out var group, out var groupSchema);
            groupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        }

        public static string GetChannelName(BuildResourcesData data)
        {
            string name = string.Empty;
            if (data == null) return name;
            switch (data.BuilderTarget)
            {
                case BuilderTarget.Windows:
                    name = data.WindowsChannel.ToString();
                    break;
                case BuilderTarget.Android:
                    name = data.AndroidChannel.ToString();
                    break;
                case BuilderTarget.iOS:
                    name = data.IOSChannel.ToString();
                    break;
            }

            return name;
        }


        private static string GetUrl(BuildResourcesSeverModel model)
        {
            string url = "";
            switch (model)
            {
                case BuildResourcesSeverModel.LocalHost:
                    url = "http://[PrivateIpAddress]:[HostingServicePort]";
                    break;
                /*
                case BuildResourcesSeverModel.Debug:
                    //url = PathManager.Instance.GetCdnUrl(CdnType.Debug);
                    break;
                case BuildResourcesSeverModel.Release:
                    //url = PathManager.Instance.GetCdnUrl(CdnType.Release);
                    break;
                    */
                default:
                    url = Replace_Remote;
                    break;
            }

            return url;
        }


        private static void CopyReportToBackUp(BuildResourcesData buildResourcesData)
        {
            var originFilePath = GetBuildReportFilePath();
            var newFilePath = GetBackupPath(buildResourcesData) + "/" + Last_Report_File_Name;
            CopyFile(originFilePath, newFilePath);
        }

        #region Build or Load Path

        private static string GetBuildPath(BuildResourcesData data)
        {
            return SERVER_DATA_FOLDER_NAME + "/" + GetFolderNameBasedOnAppVersion(data) + "/" + GetFolderName(data);
        }

        private static string GetLoadPath(BuildResourcesData data)
        {
            var path = string.Empty;
            if (data.BuildResourcesSeverModel == BuildResourcesSeverModel.LocalHost)
            {
                path += GetUrl(data.BuildResourcesSeverModel);
            }
            else
            {
                path += GetUrl(data.BuildResourcesSeverModel) + GetFolderNameBasedOnAppVersion(data) + "/" +
                        GetReplaceVersionName(data);
            }

            return path;
        }

        private static string GetHotUpdateConfigPath()
        {
            return "./hotUpdateConfig.json";
        }

        private static string GetHotUpdateConfigBackupPath(BuildResourcesData data)
        {
            var rootDir = GetBackupPath(data);
            var subDir = GetFolderName(data);
            return Path.Combine(rootDir, subDir, "hotUpdateConfig.json");
        }

        private static string GetHotUpdateIsForceBackupPath(BuildResourcesData data)
        {
            var rootDir = GetBackupPath(data);
            var subDir = GetFolderName(data);
            return Path.Combine(rootDir, subDir, "isForce");
        }

        private static string GetBuildReportFilePath()
        {
            var exportAdsPath = GetExportAdsPath();
            return exportAdsPath + "/" + "buildlayout.json";
        }

        #endregion


        #region Export Path

        private static string GetExportPath()
        {
            return Application.dataPath + "/../" + SERVER_DATA_FOLDER_NAME;
        }

        private static string GetExportBuildPath(BuildResourcesData data)
        {
            return Application.dataPath + "/../" + GetBuildPath(data);
        }

        private static string GetExportAdsPath()
        {
            return Application.dataPath + "/../Library/com.unity.addressables";
        }

        private static string GetExportAdsBinPath(BuildResourcesData data)
        {
            return GetExportAdsPath() + "/" + data.BuilderTarget;
        }

        #endregion


        private static string GetFolderName(BuildResourcesData data)
        {
            return GetChannelName(data) + "_" + data.ResourcesVersion + "_" +
                   data.BuildResourcesSeverModel;
        }

        private static string GetReplaceVersionName(BuildResourcesData data)
        {
            return GetChannelName(data) + "_" + Replace_Version + "_" +
                   data.BuildResourcesSeverModel;
        }

        public static string GetFolderNameBasedOnAppVersion(BuildResourcesData data)
        {
            return GetChannelName(data) + "_" + data.AppVersion + "_" +
                   data.BuildResourcesSeverModel;
        }

        #region Backup Path

        private static string GetBackupPath(BuildResourcesData data)
        {
            return Application.dataPath + "/../" + BACKUP_FOLDER_NAME + "/" + GetFolderNameBasedOnAppVersion(data);
        }

        private static string GetBackupSeverDataBuildPath(BuildResourcesData data)
        {
            string path = GetBackupPath(data);
            return path + "/" + GetFolderName(data);
        }

        private static string GetBackupServerDataDiffPath(string lastVersion, BuildResourcesData data)
        {
            string path = GetBackupPath(data);
            return path + "/" + GetFolderName(data) + $"_diff_{lastVersion}";
        }


        private static string GetBackupAdsBuildPath(BuildResourcesData data)
        {
            var path = GetBackupPath(data);
            return path + "/com.unity.addressables";
        }

        private static string GetBackupAdsBinPath(BuildResourcesData data)
        {
            return GetBackupAdsBuildPath(data) + "/" + data.BuilderTarget.ToString();
        }

        private static string GetBackupVersionFolderPath(BuildResourcesData data)
        {
            return Application.dataPath + "/../" + BACKUP_FOLDER_NAME + "/" + GetChannelName(data) + "_" +
                   data.BuildResourcesSeverModel;
        }

        private static string GetLastVersionPath(BuildResourcesData data)
        {
            string path = GetBackupLastBuildPath(data);
            return path + "/" + BACKUP_FILE_NAME;
        }


        private static string GetBackupVersionPath(BuildResourcesData data)
        {
            return GetBackupSeverDataBuildPath(data) + "/" + BACKUP_FILE_NAME;
        }

        private static string GetExportVersionPath(BuildResourcesData data)
        {
            string path = GetExportBuildPath(data);
            return path + "/" + BACKUP_FILE_NAME;
        }

        private static string GetTempDebugExportVersionPath(BuildResourcesData data)
        {
            string path = GetExportPath();
            return path + "/" + GetChannelName(data) + "_" + data.BuildResourcesSeverModel + "/" + BACKUP_FILE_NAME;
        }

        private static string GetBackupLastBuildPath(BuildResourcesData data)
        {
            string path = GetBackupPath(data);
            return path + "/" + GetChannelName(data) + "_" + BACKUP_LAST_NAME + "_" + data.BuildResourcesSeverModel;
        }

        #endregion

        #region Asset Bin

        private static string GetAssetAdsBinPath(BuildResourcesData data)
        {
            return Application.dataPath + "/AddressableAssetsData/" + data.BuilderTarget;
        }

        private static string GetAssetAdsBinFilePath(BuildResourcesData data)
        {
            return GetAssetAdsBinPath(data) + "/addressables_content_state.bin";
        }

        #endregion

        #region IO

        private static void CopyFile(string from, string to)
        {
            if (!File.Exists(from))
            {
                return;
            }

            if (File.Exists(to))
            {
                File.Delete(to);
            }

            File.Copy(from, to);
        }

        public static void CopyDirectory(string from, string to)
        {
            //检查是否存在目的目录  
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            if (Directory.Exists(from) == true)
            {
                //先来复制文件  
                DirectoryInfo directoryInfo = new DirectoryInfo(from);
                FileInfo[] files = directoryInfo.GetFiles();

                //复制所有文件  
                for (int i = 0; i < files.Length; i++)
                {
                    string _toPath = Path.Combine(to, files[i].Name);
                    DeleteFile(_toPath);
                    files[i].CopyTo(_toPath);
                }

                //最后复制目录  
                DirectoryInfo[] directoryInfoArray = directoryInfo.GetDirectories();
                for (int d = 0; d < directoryInfoArray.Length; d++)
                {
                    CopyDirectory(Path.Combine(from, directoryInfoArray[d].Name),
                        Path.Combine(to, directoryInfoArray[d].Name));
                }
            }
        }

        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path"></param>
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 创建文件夹根据有后缀的目录
        /// </summary>
        /// <param name="path">有后缀的目录</param>
        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 删除文件夹根据有后缀的目录
        /// </summary>
        /// <param name="path">有后缀的目录</param>
        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        #endregion
    }
}