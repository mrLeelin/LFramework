using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    /// <summary>
    /// V1 Setting 同步服务：负责状态分析与安全同步。
    /// </summary>
    public static class SettingSyncService
    {
        public static SettingSyncReport SyncTemplates(
            ProjectSettingSelector selector,
            SettingSyncState syncState,
            IEnumerable<ScriptableObject> templates,
            Func<ScriptableObject, string> projectAssetPathResolver)
        {
            if (selector == null) throw new ArgumentNullException(nameof(selector));
            if (syncState == null) throw new ArgumentNullException(nameof(syncState));
            if (projectAssetPathResolver == null) throw new ArgumentNullException(nameof(projectAssetPathResolver));

            SettingSyncReport report = AnalyzeTemplates(selector, syncState, templates);
            foreach (SettingSyncItemReport item in report.Items.Where(item =>
                         item.templateAsset != null &&
                         item.status != SettingSyncStatus.ManualReview &&
                         item.status != SettingSyncStatus.Deprecated &&
                         item.status != SettingSyncStatus.UpToDate))
            {
                if (item.localAsset == null)
                {
                    string targetPath = projectAssetPathResolver(item.templateAsset);
                    ScriptableObject localAsset = CloneTemplateAsset(item.templateAsset, targetPath);
                    RegisterLocalAsset(selector, localAsset);
                    item.localAsset = localAsset;
                    syncState.UpsertRecord(BuildSyncRecord(item.templateAsset, localAsset));
                    continue;
                }

                SettingSyncRecord record = syncState.GetRecord(item.settingId);
                if (record == null || string.IsNullOrWhiteSpace(record.lastSnapshotJson))
                {
                    syncState.UpsertRecord(BuildSyncRecord(item.templateAsset, item.localAsset));
                    continue;
                }

                ScriptableObject baseOld = CreateSnapshotInstance(item.templateAsset.GetType(), record.lastSnapshotJson);
                SettingMergeResult mergeResult = SettingMergeUtility.Merge(baseOld, item.templateAsset, item.localAsset);
                item.fieldChanges.Clear();
                item.fieldChanges.AddRange(mergeResult.FieldChanges);
                if (!mergeResult.RequiresManualReview)
                {
                    EditorUtility.SetDirty(item.localAsset);
                    syncState.UpsertRecord(BuildSyncRecord(item.templateAsset, item.localAsset));
                }
            }

            EditorUtility.SetDirty(selector);
            EditorUtility.SetDirty(syncState);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return report;
        }

        public static SettingSyncReport AnalyzeTemplates(
            ProjectSettingSelector selector,
            SettingSyncState syncState,
            IEnumerable<ScriptableObject> templates)
        {
            var report = new SettingSyncReport();
            selector ??= ScriptableObject.CreateInstance<ProjectSettingSelector>();
            syncState ??= ScriptableObject.CreateInstance<SettingSyncState>();

            var templateList = templates?.Where(template => template != null).ToList() ?? new List<ScriptableObject>();
            foreach (ScriptableObject template in templateList)
            {
                string settingId = GetSettingId(template);
                ScriptableObject localAsset = FindLocalAsset(selector, settingId, template);
                SettingSyncRecord record = syncState.GetRecord(settingId);
                var item = new SettingSyncItemReport
                {
                    settingId = settingId,
                    settingTypeName = template.GetType().AssemblyQualifiedName,
                    templateAsset = template,
                    localAsset = localAsset
                };

                if (localAsset == null)
                {
                    item.status = record != null ? SettingSyncStatus.Missing : SettingSyncStatus.New;
                    report.Items.Add(item);
                    continue;
                }

                if (record == null || string.IsNullOrWhiteSpace(record.lastSnapshotJson))
                {
                    item.status = SettingSyncStatus.Updatable;
                    report.Items.Add(item);
                    continue;
                }

                ScriptableObject baseOld = CreateSnapshotInstance(template.GetType(), record.lastSnapshotJson);
                SettingMergeResult mergeResult = SettingMergeUtility.Merge(baseOld, template, localAsset);
                item.fieldChanges.AddRange(mergeResult.FieldChanges);
                item.status = mergeResult.RequiresManualReview
                    ? SettingSyncStatus.ManualReview
                    : mergeResult.FieldChanges.Count > 0
                        ? SettingSyncStatus.Updatable
                        : SettingSyncStatus.UpToDate;
                report.Items.Add(item);
            }

            HashSet<string> templateIds = templateList.Select(GetSettingId).ToHashSet();
            foreach (BaseSetting localSetting in selector.GetAllSettings())
            {
                if (!templateIds.Contains(localSetting.SettingId))
                {
                    report.Items.Add(new SettingSyncItemReport
                    {
                        settingId = localSetting.SettingId,
                        settingTypeName = localSetting.GetType().AssemblyQualifiedName,
                        localAsset = localSetting,
                        status = SettingSyncStatus.Deprecated
                    });
                }
            }

            foreach (ComponentSetting localSetting in selector.GetAllComponentSettings())
            {
                if (!templateIds.Contains(localSetting.SettingId))
                {
                    report.Items.Add(new SettingSyncItemReport
                    {
                        settingId = localSetting.SettingId,
                        settingTypeName = localSetting.GetType().AssemblyQualifiedName,
                        localAsset = localSetting,
                        status = SettingSyncStatus.Deprecated
                    });
                }
            }

            return report;
        }

        public static string ComputeTemplateHash(ScriptableObject template)
        {
            string json = EditorJsonUtility.ToJson(template, true);
            return json.GetHashCode().ToString();
        }

        private static SettingSyncRecord BuildSyncRecord(ScriptableObject template, ScriptableObject localAsset)
        {
            string snapshotJson = EditorJsonUtility.ToJson(template, true);
            return new SettingSyncRecord
            {
                settingId = GetSettingId(template),
                settingTypeName = template.GetType().AssemblyQualifiedName,
                localAsset = localAsset,
                lastTemplateVersion = 1,
                lastTemplateHash = ComputeTemplateHash(template),
                lastSnapshotJson = snapshotJson,
                lastSyncTimeUtc = DateTime.UtcNow.ToString("O"),
                migrationNotes = string.Empty
            };
        }

        private static ScriptableObject CloneTemplateAsset(ScriptableObject template, string targetPath)
        {
            EnsureParentFolderExists(targetPath);
            ScriptableObject existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(template), targetPath))
            {
                throw new InvalidOperationException($"[SettingSyncService] Failed to clone template to '{targetPath}'.");
            }

            ScriptableObject localAsset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(targetPath);
            if (localAsset == null)
            {
                throw new InvalidOperationException($"[SettingSyncService] Failed to load cloned asset '{targetPath}'.");
            }

            return localAsset;
        }

        private static void RegisterLocalAsset(ProjectSettingSelector selector, ScriptableObject localAsset)
        {
            switch (localAsset)
            {
                case BaseSetting baseSetting:
                    selector.SetSetting(baseSetting);
                    break;
                case ComponentSetting componentSetting:
                    selector.SetComponentSetting(componentSetting);
                    break;
            }
        }

        private static ScriptableObject FindLocalAsset(ProjectSettingSelector selector, string settingId, ScriptableObject template)
        {
            if (template is BaseSetting)
            {
                return selector.GetSetting(settingId);
            }

            if (template is ComponentSetting)
            {
                return selector.GetComponentSetting(settingId);
            }

            return null;
        }

        private static string GetSettingId(ScriptableObject setting)
        {
            return setting switch
            {
                BaseSetting baseSetting => baseSetting.SettingId,
                ComponentSetting componentSetting => componentSetting.SettingId,
                _ => setting.GetType().FullName
            };
        }

        private static ScriptableObject CreateSnapshotInstance(Type type, string json)
        {
            var instance = ScriptableObject.CreateInstance(type) as ScriptableObject;
            EditorJsonUtility.FromJsonOverwrite(json, instance);
            return instance;
        }

        private static void EnsureParentFolderExists(string assetPath)
        {
            string parentPath = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(parentPath) || AssetDatabase.IsValidFolder(parentPath))
            {
                return;
            }

            string[] segments = parentPath.Split('/');
            string current = segments[0];
            for (int i = 1; i < segments.Length; i++)
            {
                string next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }
    }
}
