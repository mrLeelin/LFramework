#if USE_ADDRESSABLE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using ThirdParty.Framework.LFramework.Scripts.Editor.BuildPackage.Builder.BuildingResource;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Build.Layout;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// Addressable 构建辅助类
    /// 提供 Addressable 资源构建过程中的通用辅助方法
    /// </summary>
    public static class AddressableBuildHelper
    {
        #region Constants

        public const string Last_Report_File_Name = "LastBuildReport.json";
        private const string DefaultPlayerBuildScriptAssetPath =
            "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.asset";
        private const string RecoveryPlayerBuildScriptAssetPath =
            "Assets/AddressableAssetsData/DataBuilders/BuildScriptPackedMode.Recovered.asset";

        #endregion

        #region Path Helper Methods

        /// <summary>
        /// 获取渠道名称
        /// </summary>

        /// <summary>
        /// 获取服务器 URL
        /// </summary>

        /// <summary>
        /// 获取文件夹名称（包含资源版本）
        /// </summary>

        /// <summary>
        /// 获取替换版本名称
        /// </summary>

        /// <summary>
        /// 获取基于应用版本的文件夹名称
        /// </summary>

        /// <summary>
        /// 获取导出根路径
        /// </summary>

        /// <summary>
        /// 获取导出构建路径
        /// </summary>

        /// <summary>
        /// 获取 Addressable 导出路径
        /// </summary>
        public static string GetExportAdsPath()
        {
            return Application.dataPath + "/../Library/com.unity.addressables";
        }

        /// <summary>
        /// 获取 Addressable Bin 导出路径
        /// </summary>
        public static string GetExportAdsBinPath(BuildSetting data)
        {
            return GetExportAdsPath() + "/" + data.builderTarget;
        }

        /// <summary>
        /// 获取导出版本文件路径
        /// </summary>

        /// <summary>
        /// 获取临时调试导出版本路径
        /// </summary>

        /// <summary>
        /// 获取备份根路径
        /// </summary>

        /// <summary>
        /// 获取备份服务器数据构建路径
        /// </summary>

        /// <summary>
        /// 获取备份 Addressable 构建路径
        /// </summary>
        public static string GetBackupAdsBuildPath(BuildSetting data)
        {
            var path = BuildResourcePathHelper.GetBackupPath(data);
            return path + "/com.unity.addressables";
        }

        /// <summary>
        /// 获取备份 Addressable Bin 路径
        /// </summary>
        public static string GetBackupAdsBinPath(BuildSetting data)
        {
            return GetBackupAdsBuildPath(data) + "/" + data.builderTarget.ToString();
        }

        /// <summary>
        /// 获取构建路径（相对路径）
        /// </summary>

        /// <summary>
        /// 获取热更新配置路径
        /// </summary>
        public static string GetHotUpdateConfigPath()
        {
            return "./hotUpdateConfig.json";
        }

        /// <summary>
        /// 获取构建报告文件路径
        /// </summary>
        public static string GetBuildReportFilePath()
        {
            var exportAdsPath = GetExportAdsPath();
            return exportAdsPath + "/" + "buildlayout.json";
        }

        #endregion

        #region Addressable Configuration Methods

        /// <summary>
        /// 设置 Addressable Profile
        /// </summary>
        public static string SetProfile(AddressableAssetSettings settings, string profile)
        {
            string profileId = settings.profileSettings.GetProfileId(profile);
            if (String.IsNullOrEmpty(profileId))
                Debug.LogWarning($"Couldn't find a profile named, {profile}, " +
                                 $"using current profile instead.");
            else
                settings.activeProfileId = profileId;
            return profileId;
        }

        /// <summary>
        /// 设置 Addressable 构建配置
        /// </summary>
        public static void SetSetting(AddressableAssetSettings settings,
            ResourceComponentSetting resourceComponentSetting, BuildSetting buildResourcesData,
            string buildPath, string loadPath)
        {
            var hotfixProfile = resourceComponentSetting.HotfixProfileName;
            if (string.IsNullOrEmpty(hotfixProfile)) hotfixProfile = "Default";
            var dynamicProfileId = SetProfile(settings,hotfixProfile);
            settings.profileSettings.SetValue(dynamicProfileId, AddressableAssetSettings.kRemoteBuildPath, buildPath);
            settings.profileSettings.SetValue(dynamicProfileId, AddressableAssetSettings.kRemoteLoadPath, loadPath);
            settings.BuildRemoteCatalog = true;
            settings.DisableCatalogUpdateOnStartup = true;
            settings.OverridePlayerVersion = buildResourcesData.GetAppVersion();
            AddressableAssetSettings.CleanPlayerContent();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 刷新 Addressable 资源
        /// </summary>
        internal static IDataBuilder EnsurePlayerDataBuilder(AddressableAssetSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var targetIndex = FindDataBuilderIndex(settings, builder => builder is BuildScriptPackedMode);
            if (targetIndex < 0)
            {
                targetIndex = AddDefaultPlayerBuildScript(settings);
            }

            if (targetIndex < 0)
            {
                targetIndex = FindDataBuilderIndex(settings,
                    builder => builder != null && builder.CanBuildData<AddressablesPlayerBuildResult>());
            }

            if (targetIndex < 0)
            {
                throw new InvalidOperationException(
                    "[AddressableBuildHelper] No valid Addressables player build script is available.");
            }

            if (settings.ActivePlayerDataBuilderIndex != targetIndex)
            {
                settings.ActivePlayerDataBuilderIndex = targetIndex;
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            var activeBuilder = settings.GetDataBuilder(targetIndex);
            if (activeBuilder == null || !activeBuilder.CanBuildData<AddressablesPlayerBuildResult>())
            {
                throw new InvalidOperationException(
                    "[AddressableBuildHelper] Active Addressables player build script is invalid after recovery.");
            }

            return activeBuilder;
        }

        /// <summary>
        /// 确保 Addressables 在构建前输出调试布局，并强制使用 JSON 格式。
        /// </summary>
        internal static void EnsureBuildLayoutPreferences()
        {
            ProjectConfigData.GenerateBuildLayout = true;
            ProjectConfigData.BuildLayoutReportFileFormat = ProjectConfigData.ReportFileFormat.JSON;
        }

        public static void AddressableRefresh()
        {
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 检查远程资源并生成热更新配置
        /// </summary>
        public static void CheckForRemoteResource(AddressableAssetSettings settings)
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

        /// <summary>
        /// 清理空的 Addressable 组
        /// </summary>
        public static void AddressableCleanEmptyGroup(AddressableAssetSettings settings)
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

        private static int FindDataBuilderIndex(AddressableAssetSettings settings, Func<IDataBuilder, bool> predicate)
        {
            for (var index = 0; index < settings.DataBuilders.Count; index++)
            {
                var builder = settings.GetDataBuilder(index);
                if (builder != null && predicate(builder))
                {
                    return index;
                }
            }

            return -1;
        }

        private static int AddDefaultPlayerBuildScript(AddressableAssetSettings settings)
        {
            var builder = LoadOrCreateDefaultPlayerBuildScript();
            if (builder == null)
            {
                return -1;
            }

            var builderObject = builder as ScriptableObject;
            var existingIndex = settings.DataBuilders.IndexOf(builderObject);
            if (existingIndex >= 0)
            {
                return existingIndex;
            }

            settings.AddDataBuilder(builder);
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            return settings.DataBuilders.Count - 1;
        }

        private static IDataBuilder LoadOrCreateDefaultPlayerBuildScript()
        {
            var builder = LoadPlayerBuildScriptAsset(DefaultPlayerBuildScriptAssetPath)
                          ?? LoadPlayerBuildScriptAsset(RecoveryPlayerBuildScriptAssetPath);
            if (builder != null)
            {
                return builder;
            }

            var createPath = ChoosePlayerBuildScriptCreatePath();
            CreateDirectory(Path.GetDirectoryName(createPath));

            var buildScript = ScriptableObject.CreateInstance<BuildScriptPackedMode>();
            AssetDatabase.CreateAsset(buildScript, createPath);
            AssetDatabase.SaveAssets();
            return buildScript;
        }

        private static IDataBuilder LoadPlayerBuildScriptAsset(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath) as IDataBuilder;
        }

        private static string ChoosePlayerBuildScriptCreatePath()
        {
            var existingAsset = AssetDatabase.LoadMainAssetAtPath(DefaultPlayerBuildScriptAssetPath);
            return existingAsset == null && !File.Exists(DefaultPlayerBuildScriptAssetPath)
                ? DefaultPlayerBuildScriptAssetPath
                : RecoveryPlayerBuildScriptAssetPath;
        }

        #endregion

        #region Incremental Update Methods

        /// <summary>
        /// 检查需要更新的内容并创建更新组
        /// </summary>
        public static void CheckForUpdateContent(string backUpPath, AddressableAssetSettings settings,
            BuildSetting data, HybridCLRSetting gameSetting)
        {
            var reportPath = backUpPath + "/" + Last_Report_File_Name;
            Debug.Log($"reportPath:{reportPath}");
            var buildLayout = BuildLayout.Open(reportPath, true, true);
            var guidExplicitAssets = new SortedDictionary<string, BuildLayout.ExplicitAsset>();
            foreach (BuildLayout.ExplicitAsset asset in BuildLayoutHelpers.EnumerateAssets(buildLayout))
            {
                guidExplicitAssets.Add(asset.Guid, asset);
            }

            string buildPath = UnityEditor.AddressableAssets.Build.ContentUpdateScript.GetContentStateDataPath(false);
            List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> entries =
                UnityEditor.AddressableAssets.Build.ContentUpdateScript.GatherModifiedEntries(settings, buildPath);
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
            // 将被修改过的资源分组
            var groupPrefix = $"UpdateGroup_{DateTime.Now:yyyyMMddhhmmss}";
            var curIndex = 1;
            var curGroupSize = 0UL;
            var allEntriesSet = new HashSet<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
            var curUpdateGroupEntries = new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>();
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
                // 通过Update生成的资源全部打上init label
                entry.SetLabel(gameSetting.defaultInitLabel, true, true);

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
            var backupPath = BuildResourcePathHelper.GetBackupPath(data);
            File.WriteAllText(Path.Combine(backupPath, "hotfixGroup.log"), log);
            buildLayout.Close();
        }

        /// <summary>
        /// 创建图集更新组
        /// </summary>
        public static void CreateAtlasUpdateGroup(string extension, AddressableAssetSettings settings,
            UnityEditor.AddressableAssets.Settings.AddressableAssetEntry entry, int index, string groupPrefix, StringBuilder sb)
        {
            var atlasName = Path.GetFileNameWithoutExtension(entry.AssetPath);
            var spriteGroupName = $"{groupPrefix}_{extension}_{atlasName}_{index}";
            sb.AppendLine(
                $"-----------create {extension} group name={spriteGroupName} assetPath={entry.AssetPath}");
            GenerateUpdateGroup(spriteGroupName, settings, new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>() { entry });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 创建单个大文件更新组
        /// </summary>
        public static void CreateSingleUpdateGroup(ulong size, AddressableAssetSettings settings,
            UnityEditor.AddressableAssets.Settings.AddressableAssetEntry entry, int index, string groupPrefix, StringBuilder sb)
        {
            var singleFileName = Path.GetFileNameWithoutExtension(entry.AssetPath);
            var singleGroupName = $"{groupPrefix}_single_{singleFileName}_{index}";
            sb.AppendLine(
                $"-----------create single group name={singleGroupName} groupSize={size} assetPath={entry.AssetPath}");
            GenerateUpdateGroup(singleGroupName, settings, new List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry>() { entry });
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 创建达到最大尺寸的更新组
        /// </summary>
        public static void CreateMaxSizeUpdateGroup(int index, AddressableAssetSettings settings,
            List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> curUpdateGroupEntries, ulong curGroupSize, string groupPrefix,
            StringBuilder sb)
        {
            var groupName = $"{groupPrefix}_{index}";
            sb.AppendLine($"-----------create normal group name={groupName} groupSize={curGroupSize}");
            GenerateUpdateGroup(groupName, settings, curUpdateGroupEntries);
        }

        /// <summary>
        /// 创建最后一个更新组
        /// </summary>
        public static void CreateLastUpdateGroup(AddressableAssetSettings settings,
            List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> curUpdateGroupEntries, ulong curGroupSize, string groupPrefix,
            StringBuilder sb)
        {
            var groupName = $"{groupPrefix}_Last";
            sb.AppendLine($"-----------create last group name={groupName} groupSize={curGroupSize}");
            GenerateUpdateGroup(groupName, settings, curUpdateGroupEntries);
        }

        /// <summary>
        /// 生成更新组
        /// </summary>
        public static void GenerateUpdateGroup(string groupName, AddressableAssetSettings settings,
            List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> entries)
        {
            AddressableHelper.GenerateUpdateGroup(groupName, settings, entries, out var group, out var groupSchema);
            groupSchema.BundleMode = BundledAssetGroupSchema.BundlePackingMode.PackTogether;
        }

        #endregion

        #region File Operation Methods

        /// <summary>
        /// 复制文件
        /// </summary>
        public static void CopyFile(string from, string to)
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

        /// <summary>
        /// 复制目录
        /// </summary>
        public static void CopyDirectory(string from, string to)
        {
            // 检查是否存在目的目录
            if (!Directory.Exists(to))
            {
                Directory.CreateDirectory(to);
            }

            if (Directory.Exists(from) == true)
            {
                // 先来复制文件
                DirectoryInfo directoryInfo = new DirectoryInfo(from);
                FileInfo[] files = directoryInfo.GetFiles();

                // 复制所有文件
                for (int i = 0; i < files.Length; i++)
                {
                    string _toPath = Path.Combine(to, files[i].Name);
                    DeleteFile(_toPath);
                    files[i].CopyTo(_toPath);
                }

                // 最后复制目录
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
        public static void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// 创建目录
        /// </summary>
        public static void CreateDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        public static void DeleteDirectory(string path)
        {
            if (Directory.Exists(path)) Directory.Delete(path, true);
        }

        #endregion

        #region Other Helper Methods

        /// <summary>
        /// 复制构建报告到备份目录
        /// </summary>
        public static void CopyReportToBackUp(BuildSetting buildResourcesData)
        {
            var originFilePath = GetBuildReportFilePath();
            var newFilePath = BuildResourcePathHelper.GetBackupPath(buildResourcesData) + "/" + Last_Report_File_Name;
            CopyFile(originFilePath, newFilePath);
        }

        #endregion
    }
}
#endif


