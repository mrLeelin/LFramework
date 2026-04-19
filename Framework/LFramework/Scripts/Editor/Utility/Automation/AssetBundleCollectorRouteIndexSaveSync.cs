#if YOOASSET_SUPPORT
using System;
using System.Linq;
using GameFramework.Resource;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using YooAsset.Editor;

namespace LFramework.Editor
{
    /// <summary>
    /// Regenerates the route index after the YooAsset collector configuration is saved.
    /// </summary>
    public class AssetBundleCollectorRouteIndexSaveSync : AssetModificationProcessor
    {
        private static bool s_IsScheduled;
        private static bool s_IsProcessing;
        private static bool s_SuppressEnqueue;
        private static bool s_PendingSync;

        /// <summary>
        /// Executes a collector save without re-queuing route-index synchronization.
        /// </summary>
        public static void RunWithoutQueue(Action action)
        {
            if (action == null)
            {
                return;
            }

            bool previousSuppressState = s_SuppressEnqueue;
            s_SuppressEnqueue = true;
            try
            {
                action();
            }
            finally
            {
                s_SuppressEnqueue = previousSuppressState;
            }
        }

        /// <summary>
        /// Queues route-index regeneration when the collector setting asset participates in a save operation.
        /// </summary>
        public static string[] OnWillSaveAssets(string[] paths)
        {
            string collectorSettingPath = GetCollectorSettingPath();
            if (!ShouldQueueRouteIndexGeneration(paths, collectorSettingPath, s_SuppressEnqueue))
            {
                return paths;
            }

            s_PendingSync = true;
            if (!s_IsScheduled && !s_IsProcessing)
            {
                s_IsScheduled = true;
                EditorApplication.delayCall += ProcessPending;
            }

            return paths;
        }

        private static void ProcessPending()
        {
            s_IsScheduled = false;
            EditorApplication.delayCall -= ProcessPending;
            if (s_IsProcessing || !s_PendingSync)
            {
                return;
            }

            s_IsProcessing = true;
            s_PendingSync = false;

            try
            {
                RunWithoutQueue(GenerateRouteIndexForCurrentProject);
            }
            finally
            {
                s_IsProcessing = false;

                if (s_PendingSync && !s_IsScheduled)
                {
                    s_IsScheduled = true;
                    EditorApplication.delayCall += ProcessPending;
                }
            }
        }

        private static void GenerateRouteIndexForCurrentProject()
        {
            try
            {
                ResourceComponentSetting setting = LoadResourceComponentSetting();
                if (setting == null ||
                    setting.ResourceMode != ResourceMode.YooAsset ||
                    !setting.YooAssetRouting.enableRouteIndex)
                {
                    return;
                }

                RouteIndexGenerationResult result = RouteIndexGenerator.Generate(setting);
                if (result.Succeeded)
                {
                    Debug.Log(
                        $"[RouteIndex] Collector save synchronized route index: {result.AssetPath} ({result.EntryCount} entries)");
                    return;
                }

                Debug.LogError(
                    $"[RouteIndex] Failed to synchronize route index after collector save: {result.ErrorMessage}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RouteIndex] Exception while synchronizing route index after collector save: {ex}");
            }
        }

        private static bool ShouldQueueRouteIndexGeneration(
            string[] paths,
            string collectorSettingPath,
            bool suppressEnqueue)
        {
            if (suppressEnqueue ||
                paths == null ||
                paths.Length == 0 ||
                string.IsNullOrWhiteSpace(collectorSettingPath))
            {
                return false;
            }

            string normalizedCollectorSettingPath = NormalizeAssetPath(collectorSettingPath);
            for (int i = 0; i < paths.Length; i++)
            {
                if (string.Equals(
                        NormalizeAssetPath(paths[i]),
                        normalizedCollectorSettingPath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetCollectorSettingPath()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AssetBundleCollectorSetting)}");
            if (guids == null || guids.Length == 0)
            {
                return string.Empty;
            }

            return AssetDatabase.GUIDToAssetPath(guids[0]);
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

        private static string NormalizeAssetPath(string path)
        {
            return string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/');
        }
    }
}
#endif
