using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace LFramework.Editor.Builder.HybridCLR
{
    /// <summary>
    /// Detects the installed HybridCLR edition from the compiled source set and keeps
    /// the HybridCLR support and DHE compilation symbols synchronized.
    /// </summary>
    public static class HybridClrEditionDefineSynchronizer
    {
        public const string HybridClrSupportDefine = "HybridCLR_SUPPORT";
        public const string DheDefine = "LFRAMEWORK_HYBRIDCLR_DHE";

        private static readonly BuildTargetGroup[] TargetGroups =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS
        };

        private static bool s_syncQueued;

        /// <summary>
        /// Returns true only when the paid editor assembly exposes its DHE source tree.
        /// </summary>
        public static bool IsDheEditionInstalled
        {
            get
            {
                TryGetDheEditionInstalled(out bool installed);
                return installed;
            }
        }

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            QueueSynchronize();
        }

        /// <summary>
        /// Testable source-based edition detection. Assembly identity and the DHE source
        /// location are both required so a community package cannot accidentally match.
        /// </summary>
        public static bool HasDheSources(string assemblyName, IEnumerable<string> sourceFiles)
        {
            if (!string.Equals(assemblyName, "HybridCLR.Editor", StringComparison.Ordinal) ||
                sourceFiles == null)
            {
                return false;
            }

            return sourceFiles.Any(IsDheSourcePath);
        }

        /// <summary>
        /// Adds or removes the DHE symbol while preserving other defines.
        /// </summary>
        public static string UpdateDheDefine(string currentDefines, bool enabled)
        {
            return UpdateDefine(currentDefines, DheDefine, enabled);
        }

        /// <summary>
        /// Keeps both HybridCLR symbols synchronized for every supported target group.
        /// </summary>
        public static string UpdateHybridClrDefines(
            string currentDefines,
            bool hybridClrEnabled,
            bool dheEnabled)
        {
            string updated = UpdateDefine(
                currentDefines,
                HybridClrSupportDefine,
                hybridClrEnabled || dheEnabled);
            return UpdateDefine(updated, DheDefine, dheEnabled);
        }

        private static string UpdateDefine(string currentDefines, string defineName, bool enabled)
        {
            var defines = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (!string.IsNullOrWhiteSpace(currentDefines))
            {
                foreach (string define in currentDefines.Split(';'))
                {
                    string normalized = define?.Trim();
                    if (!string.IsNullOrEmpty(normalized) && seen.Add(normalized) &&
                        !string.Equals(normalized, defineName, StringComparison.Ordinal))
                    {
                        defines.Add(normalized);
                    }
                }
            }

            if (enabled)
            {
                defines.Add(defineName);
            }

            return string.Join(";", defines);
        }

        internal static void QueueSynchronize()
        {
            if (s_syncQueued)
            {
                return;
            }

            s_syncQueued = true;
            EditorApplication.delayCall += SynchronizeQueued;
        }

        private static void SynchronizeQueued()
        {
            s_syncQueued = false;
            Synchronize();
        }

        private static void Synchronize()
        {
            if (!TryGetDheEditionInstalled(out bool dheEnabled))
            {
                // Do not remove an existing symbol while Unity is rebuilding the
                // CompilationPipeline snapshot after an import.
                QueueSynchronize();
                return;
            }

            bool hybridClrEnabled = dheEnabled || HasHybridClrRuntimeSource();
            bool changed = false;
            foreach (BuildTargetGroup group in TargetGroups)
            {
                changed |= SynchronizeTargetGroup(group, hybridClrEnabled, dheEnabled);
            }

            BuildTargetGroup activeGroup = BuildPipeline.GetBuildTargetGroup(
                EditorUserBuildSettings.activeBuildTarget);
            if (Array.IndexOf(TargetGroups, activeGroup) < 0)
            {
                changed |= SynchronizeTargetGroup(activeGroup, hybridClrEnabled, dheEnabled);
            }

            if (changed)
            {
                // PlayerSettings is serialized with the project settings asset in Unity 6.
                AssetDatabase.SaveAssets();
            }
        }

        private static bool TryGetDheEditionInstalled(out bool installed)
        {
            try
            {
                installed = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                    .Any(assembly => string.Equals(assembly.name, "HybridCLR.Editor", StringComparison.Ordinal) &&
                                     assembly.sourceFiles.Any(sourceFile =>
                                         IsDheSourcePath(sourceFile) && HasExistingSourceFile(sourceFile)));
                if (installed)
                {
                    return true;
                }

                // The compilation snapshot can lag behind an imported source file.
                // AssetDatabase is the authoritative fallback during that window.
                installed = HasDheSourceAsset();
                return true;
            }
            catch
            {
                // Keep the source-based decision available while Unity rebuilds the
                // CompilationPipeline snapshot. If no DHE file exists, remove a stale
                // symbol instead of preserving it indefinitely.
                installed = HasDheSourceAsset();
                return true;
            }
        }

        private static bool IsDheSourcePath(string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                return false;
            }

            string normalized = sourceFile.Replace('\\', '/');
            return normalized.IndexOf("hybridclr", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   normalized.EndsWith(
                       "/Editor/DHE/MetaVersionWorkflow.cs",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasExistingSourceFile(string sourceFile)
        {
            if (File.Exists(sourceFile))
            {
                return true;
            }

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return File.Exists(Path.Combine(projectRoot, sourceFile));
        }

        private static bool HasDheSourceAsset()
        {
            try
            {
                foreach (string guid in AssetDatabase.FindAssets("MetaVersionWorkflow"))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (IsDheSourcePath(path) && HasExistingSourceFile(path))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // AssetDatabase may also be unavailable during a domain reload.
            }

            return false;
        }

        private static bool SynchronizeTargetGroup(
            BuildTargetGroup group,
            bool hybridClrEnabled,
            bool dheEnabled)
        {
            if (group == BuildTargetGroup.Unknown)
            {
                return false;
            }

            try
            {
                NamedBuildTarget target = NamedBuildTarget.FromBuildTargetGroup(group);
                string currentDefines = PlayerSettings.GetScriptingDefineSymbols(target);
                string updatedDefines = UpdateHybridClrDefines(
                    currentDefines,
                    hybridClrEnabled,
                    dheEnabled);
                if (string.Equals(currentDefines, updatedDefines, StringComparison.Ordinal))
                {
                    return false;
                }

                PlayerSettings.SetScriptingDefineSymbols(target, updatedDefines);
                Debug.Log(
                    $"[HybridClrEditionDefineSynchronizer] " +
                    $"Synchronized {HybridClrSupportDefine}={(hybridClrEnabled ? "on" : "off")}, " +
                    $"{DheDefine}={(dheEnabled ? "on" : "off")} for {group}.");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[HybridClrEditionDefineSynchronizer] Could not synchronize {group}: " +
                    exception.Message);
                return false;
            }
        }

        private static bool HasHybridClrRuntimeSource()
        {
            try
            {
                if (AssetDatabase.FindAssets("HybridCLR.Runtime t:AssemblyDefinitionAsset")
                    .Select(AssetDatabase.GUIDToAssetPath)
                    .Any(path => string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        "HybridCLR.Runtime",
                        StringComparison.Ordinal)))
                {
                    return true;
                }

                return CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                    .Any(assembly => string.Equals(assembly.name, "HybridCLR.Editor", StringComparison.Ordinal));
            }
            catch
            {
                return false;
            }
        }

        private static bool IsRelevantHybridClrPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            string normalized = path.Replace('\\', '/');
            return normalized.IndexOf("hybridclr", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static bool HasRelevantChanges(
            IEnumerable<string> importedAssets,
            IEnumerable<string> deletedAssets,
            IEnumerable<string> movedAssets,
            IEnumerable<string> movedFromAssetPaths)
        {
            return (importedAssets ?? Array.Empty<string>()).Concat(
                    deletedAssets ?? Array.Empty<string>())
                .Concat(movedAssets ?? Array.Empty<string>())
                .Concat(movedFromAssetPaths ?? Array.Empty<string>())
                .Any(IsRelevantHybridClrPath);
        }
    }

    /// <summary>
    /// Runs before the next domain reload when a HybridCLR source/package file changes.
    /// This also removes a stale DHE symbol when the paid source tree is deleted.
    /// </summary>
    internal sealed class HybridClrEditionAssetPostprocessor : AssetPostprocessor
    {
        public static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (HybridClrEditionDefineSynchronizer.HasRelevantChanges(
                    importedAssets,
                    deletedAssets,
                    movedAssets,
                    movedFromAssetPaths))
            {
                HybridClrEditionDefineSynchronizer.QueueSynchronize();
            }
        }
    }
}
