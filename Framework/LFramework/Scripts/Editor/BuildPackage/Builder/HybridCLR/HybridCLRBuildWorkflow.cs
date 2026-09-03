using System;
using System.IO;
using System.Linq;
using UnityEditor;

#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
using HybridCLR.Editor;
using HybridCLR.Editor.Installer;
#if LFRAMEWORK_HYBRIDCLR_DHE
using HybridCLR.Editor.DHE;
#endif
#endif

#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
using HybridCLR.Editor.Settings;
#endif

namespace LFramework.Editor.Builder.HybridCLR
{
    /// <summary>
    /// LFramework 构建流程使用的 DHE 快照辅助工具。
    /// 快照的保存位置以及生成产物的发布方式由调用方的资源系统决定。
    /// </summary>
    public static class HybridCLRBuildWorkflow
    {
        /// <summary>
        /// 当前 DHE 集成要求的 HybridCLR 版本。
        /// </summary>
        public const string RequiredHybridClrVersion = "8.11.1";

        /// <summary>
        /// 当前编译是否由自动版型同步器标记为旗舰版。
        /// </summary>
        public static bool IsDheEdition
        {
            get
            {
#if LFRAMEWORK_HYBRIDCLR_DHE
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 创建指定平台的 AOT 快照，并生成初始 MetaVersion 文件。
        /// </summary>
        public static void CreateAotSnapshot(BuildTarget target, string outputDirectory)
        {
            // 先验证 HybridCLR 和本地 libil2cpp 版本，避免生成不兼容的快照。
            EnsureDheAvailable();
            // 确保快照输出目录存在。
            EnsureDheDirectory(outputDirectory);
#if LFRAMEWORK_HYBRIDCLR_DHE
            // 直接调用固定版本 HybridCLR 提供的 DHE 工作流 API。
            MetaVersionWorkflow.CreateAotSnapshot(target, outputDirectory);
            MetaVersionWorkflow.GenerateAotSnapshotMetaVersionFiles(null, outputDirectory);
#else
            // 当前检出未启用 HybridCLR 时，保留明确的功能不可用错误。
            throw new NotSupportedException(
                "HybridCLR is not installed in the consuming project.");
#endif
        }

        /// <summary>
        /// 根据基线 AOT 快照，生成当前热更新程序集的 MetaVersion 文件。
        /// </summary>
        public static void GenerateHotUpdateMetaVersionFiles(string baselineSnapshotDirectory,
            BuildTarget target)
        {
            // 生成前必须确认版本和本地原生环境匹配。
            EnsureDheAvailable();
            // 基线快照是差分 MetaVersion 计算的输入，缺失时立即终止构建。
            if (!Directory.Exists(baselineSnapshotDirectory))
            {
                throw new DirectoryNotFoundException(
                    $"DHE baseline snapshot directory does not exist: {baselineSnapshotDirectory}");
            }

#if LFRAMEWORK_HYBRIDCLR_DHE
            // 热更新 DLL 输出目录由 HybridCLR 设置统一计算。
            string hotUpdateDirectory = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
            // 直接调用固定版本 HybridCLR 的 MetaVersion 生成 API。
            MetaVersionWorkflow.GenerateHotUpdateMetaVersionFiles(
                baselineSnapshotDirectory,
                hotUpdateDirectory);
#else
            // 没有 HybridCLR 编译宏时不能执行 DHE 生成流程。
            throw new NotSupportedException(
                "HybridCLR is not installed in the consuming project.");
#endif
        }

        /// <summary>
        /// 获取快照目录下保存 MetaVersion 文件的子目录。
        /// </summary>
        public static string GetMetaVersionDirectory(string snapshotDirectory)
        {
            // 空目录通常表示调用方没有正确准备 DHE 快照路径。
            if (string.IsNullOrWhiteSpace(snapshotDirectory))
            {
                throw new ArgumentException("DHE snapshot directory is empty.", nameof(snapshotDirectory));
            }

            return Path.Combine(snapshotDirectory, "MetaVersions");
        }

        /// <summary>
        /// 同步 DHE 差分程序集配置，并返回配置对象是否支持该字段。
        /// </summary>
#if LFRAMEWORK_HYBRIDCLR_DHE
        internal static bool TryUpdateDifferentialHybridAssemblies(
            HybridCLRSettings hybridClrSettings,
            string[] assemblyNames,
            out bool changed)
        {
            // 调用方传入空配置时无法同步。
            changed = false;
            if (hybridClrSettings == null)
            {
                return false;
            }

            string[] newValue = assemblyNames ?? Array.Empty<string>();
            string[] currentValue = hybridClrSettings.differentialHybridAssemblies ?? Array.Empty<string>();
            if (!currentValue.SequenceEqual(newValue, StringComparer.Ordinal))
            {
                hybridClrSettings.differentialHybridAssemblies = newValue;
                changed = true;
            }

            return true;
        }
#endif

        /// <summary>
        /// 获取指定平台的默认 AOT 快照目录。
        /// </summary>
        public static string GetDefaultSnapshotDirectory(BuildTarget target)
        {
            return Path.GetFullPath(Path.Combine(
                "HybridCLRData",
                "LFrameworkDHE",
                target.ToString(),
                "AotSnapshot"));
        }

        /// <summary>
        /// 创建指定输出目录；空路径被视为调用错误。
        /// </summary>
        private static void EnsureDheDirectory(string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("DHE snapshot output directory is empty.", nameof(outputDirectory));
            }

            Directory.CreateDirectory(outputDirectory);
        }

        /// <summary>
        /// 校验 DHE API、HybridCLR 包版本和本地 libil2cpp 版本是否全部满足要求。
        /// </summary>
        internal static void EnsureDheAvailable()
        {
#if !LFRAMEWORK_HYBRIDCLR_DHE
            throw new NotSupportedException(
                "DHE mode requires the HybridCLR flagship source package. " +
                "The community package cannot provide DHE APIs.");
#else
            // 旗舰宏只由源码检测器设置；这里校验安装版本和原生运行时完整性。
            if (!IsDheEnvironmentAvailable(
                    true,
                    GetPackageVersion(),
                    GetInstalledLibil2cppVersion(),
                    HasDheNativeFiles()))
            {
                throw new NotSupportedException(
                    "DHE mode requires a HybridCLR flagship package exposing the 8.11.1 MetaVersion APIs.");
            }

            string packageVersion = GetPackageVersion();
            // 编辑器包版本必须与当前 DHE 集成版本完全一致。
            if (!IsRequiredVersion(packageVersion))
            {
                throw new NotSupportedException(
                    $"DHE mode requires HybridCLR flagship {RequiredHybridClrVersion}, " +
                    $"but the installed package is '{packageVersion ?? "unknown"}'.");
            }

            string libil2cppVersion = GetInstalledLibil2cppVersion();
            // 本地原生 libil2cpp 版本也必须匹配，防止使用错误的安装分支构建。
            if (!IsRequiredVersion(libil2cppVersion))
            {
                throw new InvalidOperationException(
                    $"DHE mode requires installed libil2cpp {RequiredHybridClrVersion}, " +
                    $"but found '{libil2cppVersion ?? "not installed"}'. Run the HybridCLR " +
                    $"{RequiredHybridClrVersion} installer before building.");
            }

            if (!HasDheNativeFiles())
            {
                throw new InvalidOperationException(
                    "DHE native files are missing from the installed libil2cpp. " +
                    "Run the HybridCLR installer for the DHE package before building.");
            }
#endif
        }

        /// <summary>
        /// 判断版本字符串是否严格等于要求的 HybridCLR 版本。
        /// </summary>
        internal static bool IsRequiredVersion(string version)
        {
            return string.Equals(version?.Trim(), RequiredHybridClrVersion, StringComparison.Ordinal);
        }

        internal static bool IsDheEnvironmentAvailable(
            bool hasRequiredApis,
            string packageVersion,
            string libil2cppVersion,
            bool hasDheNativeFiles)
        {
            return hasRequiredApis &&
                   IsRequiredVersion(packageVersion) &&
                   IsRequiredVersion(libil2cppVersion) &&
                   hasDheNativeFiles;
        }
        /// <summary>
        /// 读取当前 HybridCLR 编辑器包的安装版本。
        /// </summary>
        private static string GetPackageVersion()
        {
#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
            try
            {
                // InstallerController 返回包元数据中的版本号。
                return new InstallerController().PackageVersion?.Trim();
            }
            catch
            {
                return null;
            }
#else
            return null;
#endif
        }

        /// <summary>
        /// 读取安装器写入的本地 libil2cpp 版本标记。
        /// </summary>
        private static string GetInstalledLibil2cppVersion()
        {
#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
            // 该文件由 HybridCLR 安装器在成功安装后写入。
            string versionFile = Path.Combine(
                SettingsUtil.LocalIl2CppDir,
                "libil2cpp",
                "hybridclr",
                "generated",
                "libil2cpp-version.txt");
            // 文件不存在表示尚未安装或安装不完整。
            try
            {
                return File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : null;
            }
            catch
            {
                return null;
            }
#else
            // 没有 HybridCLR 宏时不声明本地原生环境可用。
            return null;
#endif
        }

        private static bool HasDheNativeFiles()
        {
#if HybridCLR_SUPPORT || LFRAMEWORK_HYBRIDCLR_DHE
            try
            {
                string metadataDirectory = Path.Combine(
                    SettingsUtil.LocalIl2CppDir,
                    "libil2cpp",
                    "hybridclr",
                    "metadata");
                return HasFileContent(Path.Combine(metadataDirectory, "DifferentialHybridImage.cpp")) &&
                       HasFileContent(Path.Combine(metadataDirectory, "DifferentialHybridImage.h"));
            }
            catch
            {
                return false;
            }
#else
            return false;
#endif
        }

        private static bool HasFileContent(string filePath)
        {
            return File.Exists(filePath) && new FileInfo(filePath).Length > 0;
        }

       
    }
}
