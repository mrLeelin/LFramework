#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using LFramework.Runtime;

namespace LFramework.EditorTools
{
    public class PrefabSubModuleUpdater : AssetModificationProcessor
    {
        private static readonly HashSet<string> SPending = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool _sDelayedScheduled;
        private static bool _sIsProcessing;
        private static bool _sSuppressEnqueue;

        // 在保存资源前回调：只收集 Prefab 路径并安排延迟处理，不在此处写回 Prefab
        public static string[] OnWillSaveAssets(string[] paths)
        {
            //暂时先隐藏
            if (paths == null || paths.Length == 0 || _sSuppressEnqueue)
                return paths;

            foreach (var p in paths)
            {
                if (!string.IsNullOrEmpty(p) && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    SPending.Add(p);
                }
            }

            if (SPending.Count == 0)
            {
                return paths;
            }

            if (!_sDelayedScheduled && !_sIsProcessing)
            {
                _sDelayedScheduled = true;
                EditorApplication.delayCall += ProcessPending;
            }

            return paths;
        }

        private static void ProcessPending()
        {
            _sDelayedScheduled = false;
            EditorApplication.delayCall -= ProcessPending;
            if (_sIsProcessing)
                return;

            _sIsProcessing = true;
            _sSuppressEnqueue = true; // 在处理与保存期间抑制新的收集与调度，避免无限循环

            try
            {
                // 拷贝并清空，允许新的保存请求继续排队到下一轮
                var toProcess = new List<string>(SPending);
                SPending.Clear();

                foreach (var path in toProcess)
                {
                    try
                    {
                        if (string.IsNullOrEmpty(path))
                            continue;

                        var root = PrefabUtility.LoadPrefabContents(path);
                        if (root == null)
                            continue;

                        bool modified = false;

                        try
                        {
                            var targets = root.GetComponent<NoParamEntityLogic>();
                            if (targets != null)
                            {
                                targets.FindSubModules();
                                EditorUtility.SetDirty(targets);
                                modified = true;
                            }

                            var windowTarget = root.GetComponent<Window>();
                            if (windowTarget != null)
                            {
                                windowTarget.FindSubModules();
                                EditorUtility.SetDirty(windowTarget);
                                modified = true;
                            }

                            if (modified)
                            {
                                PrefabUtility.SaveAsPrefabAsset(root, path);
                            }
                        }
                        finally
                        {
                            PrefabUtility.UnloadPrefabContents(root);
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"PrefabSubModuleUpdater 处理 {path} 时出错: {e.Message}\n{e}");
                    }
                }
            }
            finally
            {
                _sSuppressEnqueue = false;
                _sIsProcessing = false;
                if (SPending.Count > 0 && !_sDelayedScheduled)
                {
                    _sDelayedScheduled = true;
                    EditorApplication.delayCall += ProcessPending;
                }
            }
        }
    }
}
#endif