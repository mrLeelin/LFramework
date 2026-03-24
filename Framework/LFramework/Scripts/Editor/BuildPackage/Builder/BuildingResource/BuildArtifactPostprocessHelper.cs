using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LFramework.Runtime;

namespace LFramework.Editor.Builder.BuildingResource
{
    /// <summary>
    /// 资源构建产物后处理工具。
    /// 负责生成差异目录、删除清单，并刷新 LastBuild 快照。
    /// </summary>
    public static class BuildArtifactPostprocessHelper
    {
        /// <summary>
        /// 删除文件清单名称。
        /// </summary>
        public const string DeletedFilesManifestName = "DeletedFiles.txt";

        /// <summary>
        /// 使用构建配置对当前导出目录执行统一后处理。
        /// </summary>
        /// <param name="buildSetting">构建配置。</param>
        /// <param name="currentBuildPath">当前导出目录。</param>
        public static void ProcessBuildArtifacts(BuildSetting buildSetting, string currentBuildPath)
        {
            if (buildSetting == null)
            {
                throw new ArgumentNullException(nameof(buildSetting));
            }

            ProcessBuildArtifacts(
                currentBuildPath,
                BuildResourcePathHelper.GetBackupLastBuildPath(buildSetting),
                BuildResourcePathHelper.GetBackupDiffBuildPath(buildSetting));
        }

        /// <summary>
        /// 对当前导出目录执行统一后处理。
        /// </summary>
        /// <param name="currentBuildPath">当前导出目录。</param>
        /// <param name="lastBuildPath">上一次构建快照目录。</param>
        /// <param name="diffBuildPath">差异输出目录。</param>
        public static void ProcessBuildArtifacts(string currentBuildPath, string lastBuildPath, string diffBuildPath)
        {
            ValidateCurrentBuildPath(currentBuildPath);

            DeleteDirectory(diffBuildPath);
            if (Directory.Exists(lastBuildPath))
            {
                Directory.CreateDirectory(diffBuildPath);
                FolderComparer.CompareAndCopyDifferentFiles(currentBuildPath, lastBuildPath, diffBuildPath);
                WriteDeletedFilesManifest(lastBuildPath, currentBuildPath, diffBuildPath);
            }

            ReplaceDirectory(currentBuildPath, lastBuildPath);
        }

        private static void ValidateCurrentBuildPath(string currentBuildPath)
        {
            if (string.IsNullOrWhiteSpace(currentBuildPath))
            {
                throw new ArgumentException("Current build path is invalid.", nameof(currentBuildPath));
            }

            if (!Directory.Exists(currentBuildPath))
            {
                throw new DirectoryNotFoundException(
                    $"Current build path does not exist: {currentBuildPath}");
            }
        }

        private static void WriteDeletedFilesManifest(
            string lastBuildPath,
            string currentBuildPath,
            string diffBuildPath)
        {
            string manifestPath = Path.Combine(diffBuildPath, DeletedFilesManifestName);
            IReadOnlyList<string> deletedFiles = CollectDeletedFiles(lastBuildPath, currentBuildPath);
            File.WriteAllLines(manifestPath, deletedFiles);
        }

        private static IReadOnlyList<string> CollectDeletedFiles(string lastBuildPath, string currentBuildPath)
        {
            var currentFiles = new HashSet<string>(
                EnumerateRelativeFiles(currentBuildPath),
                StringComparer.OrdinalIgnoreCase);
            var deletedFiles = new List<string>();

            foreach (string previousFile in EnumerateRelativeFiles(lastBuildPath))
            {
                if (!currentFiles.Contains(previousFile))
                {
                    deletedFiles.Add(previousFile);
                }
            }

            deletedFiles.Sort(StringComparer.OrdinalIgnoreCase);
            return deletedFiles;
        }

        private static IEnumerable<string> EnumerateRelativeFiles(string rootPath)
        {
            return Directory
                .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .Select(filePath => Path.GetRelativePath(rootPath, filePath).Replace("\\", "/"));
        }

        private static void ReplaceDirectory(string sourcePath, string targetPath)
        {
            DeleteDirectory(targetPath);
            CopyDirectory(sourcePath, targetPath);
        }

        private static void CopyDirectory(string sourcePath, string targetPath)
        {
            Directory.CreateDirectory(targetPath);
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                string targetFilePath = Path.Combine(targetPath, Path.GetFileName(filePath));
                File.Copy(filePath, targetFilePath, true);
            }

            foreach (string directoryPath in Directory.GetDirectories(sourcePath))
            {
                string directoryName = Path.GetFileName(directoryPath);
                string targetDirectoryPath = Path.Combine(targetPath, directoryName);
                CopyDirectory(directoryPath, targetDirectoryPath);
            }
        }

        private static void DeleteDirectory(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
