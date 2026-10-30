#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LFramework.Runtime;

namespace LFramework.EditorTools
{
    /// <summary>
    /// 保存后在稳定的编辑模式下刷新 Prefab 子模块列表。
    /// </summary>
    [InitializeOnLoad]
    public class PrefabSubModuleUpdater : AssetModificationProcessor
    {
        private const string PendingStateKey = "LFramework.PrefabSubModuleUpdater.PendingState";
        private static readonly HashSet<string> SPending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> SPendingGuids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static bool _sDelayedScheduled;
        private static bool _sIsProcessing;
        private static bool _sSuppressEnqueue;
        private static bool _sWaitingForEditMode;

        [Serializable]
        private sealed class PendingState
        {
            public string[] paths;
            public string[] guids;
            public bool waitingForEditMode;
        }

        static PrefabSubModuleUpdater()
        {
            var json = SessionState.GetString(PendingStateKey, string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                var state = JsonUtility.FromJson<PendingState>(json);
                if (state.paths != null)
                {
                    SPending.UnionWith(state.paths);
                    for (var i = 0; i < state.paths.Length; i++)
                        SPendingGuids[state.paths[i]] = state.guids != null && i < state.guids.Length ? state.guids[i] : string.Empty;
                }
                _sWaitingForEditMode = state.waitingForEditMode;
            }

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SchedulePending();
        }

        private static bool ShouldPause => _sWaitingForEditMode ||
                                           EditorApplication.isPlaying ||
                                           EditorApplication.isPlayingOrWillChangePlaymode;

        /// <summary>
        /// 只记录待处理路径；原样返回路径，让用户的正常保存继续执行。
        /// </summary>
        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (paths == null || paths.Length == 0 || _sSuppressEnqueue)
                return paths;

            foreach (var p in paths)
            {
                if (!string.IsNullOrEmpty(p) && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    SPending.Add(p);
                    // 只读取资源标识，不加载 Prefab；同一路径的新保存替换旧任务的标识。
                    SPendingGuids[p] = AssetDatabase.AssetPathToGUID(p, AssetPathToGUIDOptions.OnlyExistingAssets);
                }
            }

            PersistPendingState();
            SchedulePending();
            return paths;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 退出播放的过渡期内两个 isPlaying 标志可能都为 false，必须等到 EnteredEditMode。
            _sWaitingForEditMode = state != PlayModeStateChange.EnteredEditMode;
            PersistPendingState();
            CancelDelayedCall();
            SchedulePending();
        }

        private static void PersistPendingState()
        {
            if (SPending.Count == 0 && !_sWaitingForEditMode)
            {
                SessionState.EraseString(PendingStateKey);
                return;
            }

            var paths = new List<string>(SPending);
            SessionState.SetString(PendingStateKey, JsonUtility.ToJson(new PendingState
            {
                paths = paths.ToArray(),
                guids = paths.ConvertAll(path => SPendingGuids[path]).ToArray(),
                waitingForEditMode = _sWaitingForEditMode
            }));
        }

        private static void SchedulePending()
        {
            if (ShouldPause || SPending.Count == 0 || _sDelayedScheduled || _sIsProcessing)
                return;

            _sDelayedScheduled = true;
            EditorApplication.delayCall += ProcessPending;
        }

        private static void CancelDelayedCall()
        {
            EditorApplication.delayCall -= ProcessPending;
            _sDelayedScheduled = false;
        }

        private static void ProcessPending()
        {
            CancelDelayedCall();
            // 排队后可能已开始切换播放状态，此处必须重新检查，且不能清空队列。
            if (_sIsProcessing || ShouldPause)
                return;

            _sIsProcessing = true;
            _sSuppressEnqueue = true; // 在处理与保存期间抑制新的收集与调度，避免无限循环

            try
            {
                // 每项处理完才移出队列并持久化，未执行的路径可跨域重载恢复。
                var toProcess = new List<string>(SPending);

                foreach (var path in toProcess)
                {
                    if (ShouldPause)
                        break;

                    try
                    {
                        if (!UpdatePrefab(path))
                            break;
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"PrefabSubModuleUpdater 处理 {path} 时出错: {e.Message}\n{e}");
                    }

                    SPending.Remove(path);
                    SPendingGuids.Remove(path);
                    PersistPendingState();
                }
            }
            finally
            {
                _sSuppressEnqueue = false;
                _sIsProcessing = false;
                SchedulePending();
            }
        }

        private static bool UpdatePrefab(string path)
        {
            // 已有资源被移动后，即使旧路径被其他 Prefab 占用，也不能写入替代品。
            if (SPendingGuids.TryGetValue(path, out var guid) && !string.IsNullOrEmpty(guid) &&
                guid != AssetDatabase.AssetPathToGUID(path, AssetPathToGUIDOptions.OnlyExistingAssets))
                return true;

            // 路径被删除或移动时跳过；仅在执行时读取最新资源，不缓存 Prefab 对象。
            if (!IsPendingPrefabStillLoadable(path))
                return true;
            if (ShouldPause)
                return false;

            var root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                return true;

            try
            {
                if (ShouldPause)
                    return false;

                bool modified = false;
                var targets = root.GetComponent<NoParamEntityLogic>();
                if (targets != null)
                {
                    targets.FindSubModules();
                    EditorUtility.SetDirty(targets);
                    modified = true;
                }

                if (ShouldPause)
                    return false;

                var windowTarget = root.GetComponent<Window>();
                if (windowTarget != null)
                {
                    windowTarget.FindSubModules();
                    EditorUtility.SetDirty(windowTarget);
                    modified = true;
                }

                if (ShouldPause)
                    return false;
                if (modified)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        internal static bool IsPendingPrefabStillLoadable(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
        }
    }
}
#endif
