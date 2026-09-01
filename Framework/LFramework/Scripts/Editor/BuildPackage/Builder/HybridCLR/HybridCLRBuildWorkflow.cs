using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;

#if HybridCLR_SUPPORT
using HybridCLR.Editor;
using HybridCLR.Editor.Installer;
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
        public const string RequiredHybridClrVersion = "8.11.1";

        private const string MetaVersionWorkflowTypeName =
            "HybridCLR.Editor.DHE.MetaVersionWorkflow, HybridCLR.Editor";
        private const string RuntimeApiTypeName = "HybridCLR.RuntimeApi, HybridCLR.Runtime";
        private const string DifferentialAssembliesFieldName = "differentialHybridAssemblies";

        private static readonly Type MetaVersionWorkflowType = Type.GetType(MetaVersionWorkflowTypeName);
        private static readonly MethodInfo CreateAotSnapshotMethod = FindMetaVersionMethod(
            "CreateAotSnapshot",
            typeof(BuildTarget),
            typeof(string));
        private static readonly MethodInfo GenerateAotSnapshotMetaVersionFilesMethod = FindMetaVersionMethod(
            "GenerateAotSnapshotMetaVersionFiles",
            typeof(string),
            typeof(string));
        private static readonly MethodInfo GenerateHotUpdateMetaVersionFilesMethod = FindMetaVersionMethod(
            "GenerateHotUpdateMetaVersionFiles",
            typeof(string),
            typeof(string));
        private static readonly Type RuntimeApiType = Type.GetType(RuntimeApiTypeName);
        private static readonly MethodInfo LoadOriginalDifferentialHybridAssemblyMethod = RuntimeApiType?.GetMethod(
            "LoadOriginalDifferentialHybridAssembly",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(string) },
            null);
        private static readonly MethodInfo LoadDifferentialHybridAssemblyWithMetaVersionMethod =
            RuntimeApiType?.GetMethod(
                "LoadDifferentialHybridAssemblyWithMetaVersion",
                BindingFlags.Public | BindingFlags.Static,
                null,
                new[] { typeof(byte[]), typeof(byte[]), typeof(byte[]), typeof(byte[]) },
                null);

        public static bool IsDheAvailable
        {
            get => HasRequiredApis && IsRequiredVersion(GetPackageVersion());
        }

        public static void CreateAotSnapshot(BuildTarget target, string outputDirectory)
        {
            EnsureDheAvailable();
            EnsureDheDirectory(outputDirectory);
            Invoke(CreateAotSnapshotMethod, target, outputDirectory);
            Invoke(GenerateAotSnapshotMetaVersionFilesMethod, null, outputDirectory);
        }

        public static void GenerateHotUpdateMetaVersionFiles(string baselineSnapshotDirectory,
            BuildTarget target)
        {
            EnsureDheAvailable();
            if (!Directory.Exists(baselineSnapshotDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"DHE baseline snapshot directory does not exist: {baselineSnapshotDirectory}");
            }

#if HybridCLR_SUPPORT
            string hotUpdateDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            Invoke(GenerateHotUpdateMetaVersionFilesMethod,
                baselineSnapshotDirectory,
                hotUpdateDirectory);
#else
            throw new NotSupportedException(
                "HybridCLR is not installed in the consuming project.");
#endif
        }

        public static string GetMetaVersionDirectory(string snapshotDirectory)
        {
            if (string.IsNullOrWhiteSpace(snapshotDirectory))
            {
                throw new ArgumentException("DHE snapshot directory is empty.", nameof(snapshotDirectory));
            }

            return Path.Combine(snapshotDirectory, "MetaVersions");
        }

        internal static bool TryUpdateDifferentialHybridAssemblies(
            object hybridClrSettings,
            string[] assemblyNames,
            out bool changed)
        {
            changed = false;
            if (hybridClrSettings == null)
            {
                return false;
            }

            FieldInfo field = hybridClrSettings.GetType().GetField(
                DifferentialAssembliesFieldName,
                BindingFlags.Public | BindingFlags.Instance);
            if (field == null || field.FieldType != typeof(string[]))
            {
                return false;
            }

            string[] newValue = assemblyNames ?? Array.Empty<string>();
            string[] currentValue = field.GetValue(hybridClrSettings) as string[] ?? Array.Empty<string>();
            if (!currentValue.SequenceEqual(newValue, StringComparer.Ordinal))
            {
                field.SetValue(hybridClrSettings, newValue);
                changed = true;
            }

            return true;
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

        internal static void EnsureDheAvailable()
        {
            if (!HasRequiredApis)
            {
                throw new NotSupportedException(
                    "DHE mode requires a HybridCLR flagship package exposing the 8.11.1 MetaVersion APIs.");
            }

            string packageVersion = GetPackageVersion();
            if (!IsRequiredVersion(packageVersion))
            {
                throw new NotSupportedException(
                    $"DHE mode requires HybridCLR flagship {RequiredHybridClrVersion}, " +
                    $"but the installed package is '{packageVersion ?? "unknown"}'.");
            }

            string libil2cppVersion = GetInstalledLibil2cppVersion();
            if (!IsRequiredVersion(libil2cppVersion))
            {
                throw new InvalidOperationException(
                    $"DHE mode requires installed libil2cpp {RequiredHybridClrVersion}, " +
                    $"but found '{libil2cppVersion ?? "not installed"}'. Run the HybridCLR " +
                    $"{RequiredHybridClrVersion} installer before building.");
            }
        }

        internal static bool IsRequiredVersion(string version)
        {
            return string.Equals(version?.Trim(), RequiredHybridClrVersion, StringComparison.Ordinal);
        }

        private static bool HasRequiredApis =>
            CreateAotSnapshotMethod != null &&
            GenerateAotSnapshotMetaVersionFilesMethod != null &&
            GenerateHotUpdateMetaVersionFilesMethod != null &&
            LoadOriginalDifferentialHybridAssemblyMethod != null &&
            LoadDifferentialHybridAssemblyWithMetaVersionMethod != null;

        private static string GetPackageVersion()
        {
#if HybridCLR_SUPPORT
            return new InstallerController().PackageVersion?.Trim();
#else
            return null;
#endif
        }

        private static string GetInstalledLibil2cppVersion()
        {
#if HybridCLR_SUPPORT
            string versionFile = Path.Combine(
                SettingsUtil.LocalIl2CppDir,
                "libil2cpp",
                "hybridclr",
                "generated",
                "libil2cpp-version.txt");
            return File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : null;
#else
            return null;
#endif
        }

        private static MethodInfo FindMetaVersionMethod(string methodName, params Type[] parameterTypes)
        {
            return MetaVersionWorkflowType?.GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.Static,
                null,
                parameterTypes,
                null);
        }

        private static void Invoke(MethodInfo method, params object[] arguments)
        {
            try
            {
                method.Invoke(null, arguments);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                throw new InvalidOperationException(exception.InnerException.Message, exception.InnerException);
            }
        }
    }
}
