using System;
using System.IO;
using UnityEditor;

#if HybridCLR_SUPPORT && LFRAMEWORK_HYBRIDCLR_DHE
using HybridCLR.Editor;
using HybridCLR.Editor.DHE;
#endif

namespace LFramework.Editor.Builder.HybridCLR
{
    /// <summary>
    /// Host-project independent DHE snapshot helpers for LFramework build pipelines.
    /// Runtime projects decide where snapshots are persisted and how the generated
    /// artifacts are published through their resource system.
    /// </summary>
    public static class HybridCLRBuildWorkflow
    {
        public static bool IsDheAvailable
        {
            get
            {
#if HybridCLR_SUPPORT && LFRAMEWORK_HYBRIDCLR_DHE
                return true;
#else
                return false;
#endif
            }
        }

        public static void CreateAotSnapshot(BuildTarget target, string outputDirectory)
        {
#if HybridCLR_SUPPORT && LFRAMEWORK_HYBRIDCLR_DHE
            EnsureDheDirectory(outputDirectory);
            MetaVersionWorkflow.CreateAotSnapshot(target, outputDirectory);
            MetaVersionWorkflow.GenerateAotSnapshotMetaVersionFiles(null, outputDirectory);
#else
            throw new NotSupportedException(
                "DHE snapshot generation requires HybridCLR 8.6 or newer commercial package.");
#endif
        }

        public static void GenerateHotUpdateMetaVersionFiles(string baselineSnapshotDirectory,
            BuildTarget target)
        {
#if HybridCLR_SUPPORT && LFRAMEWORK_HYBRIDCLR_DHE
            if (!Directory.Exists(baselineSnapshotDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"DHE baseline snapshot directory does not exist: {baselineSnapshotDirectory}");
            }

            string hotUpdateDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            MetaVersionWorkflow.GenerateHotUpdateMetaVersionFiles(
                baselineSnapshotDirectory,
                hotUpdateDirectory);
#else
            throw new NotSupportedException(
                "DHE MetaVersion generation requires HybridCLR 8.6 or newer commercial package.");
#endif
        }

        public static string GetDefaultSnapshotDirectory(BuildTarget target)
        {
            return Path.GetFullPath(Path.Combine(
                "HybridCLRData",
                "LFrameworkDHE",
                target.ToString(),
                "AotSnapshot"));
        }

        private static void EnsureDheDirectory(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("DHE snapshot output directory is empty.", nameof(outputDirectory));
            }

            Directory.CreateDirectory(outputDirectory);
        }
    }
}
