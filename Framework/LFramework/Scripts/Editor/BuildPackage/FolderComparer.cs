using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor
{
    /// <summary>
    /// Compares two folders and copies changed files from the current build output.
    /// </summary>
    public static class FolderComparer
    {
        /// <summary>
        /// Copies files that are new or different in <paramref name="sourceFolder1"/>.
        /// </summary>
        /// <param name="sourceFolder1">Current build folder.</param>
        /// <param name="sourceFolder2">Previous build folder.</param>
        /// <param name="outputFolder">Diff output folder.</param>
        public static void CompareAndCopyDifferentFiles(
            string sourceFolder1,
            string sourceFolder2,
            string outputFolder)
        {
            if (!Directory.Exists(sourceFolder1) || !Directory.Exists(sourceFolder2))
            {
                SafeLogError("Source folders do not exist.");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            var files1 = Directory.GetFiles(sourceFolder1, "*", SearchOption.AllDirectories);
            var files2 = Directory.GetFiles(sourceFolder2, "*", SearchOption.AllDirectories);

            var fileHashes2 = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string filePath in files2)
            {
                string relativePath = Path.GetRelativePath(sourceFolder2, filePath).Replace("\\", "/");
                fileHashes2[relativePath] = GetFileHash(filePath);
            }

            foreach (string filePath in files1)
            {
                string relativePath = Path.GetRelativePath(sourceFolder1, filePath).Replace("\\", "/");
                string fileHash = GetFileHash(filePath);

                if (!fileHashes2.TryGetValue(relativePath, out string hash2) ||
                    hash2 != fileHash ||
                    fileHash.Equals("0", StringComparison.Ordinal) ||
                    hash2.Equals("0", StringComparison.Ordinal))
                {
                    string outputFilePath = Path.Combine(outputFolder, relativePath);
                    string outputDirectory = Path.GetDirectoryName(outputFilePath) ?? string.Empty;
                    Directory.CreateDirectory(outputDirectory);
                    File.Copy(filePath, outputFilePath, true);
                    SafeLog($"Copied different file: {relativePath}");
                }
            }
        }

        /// <summary>
        /// Calculates the MD5 hash for a file.
        /// </summary>
        /// <param name="filePath">Target file path.</param>
        /// <returns>Lowercase hash string, or "0" when unavailable.</returns>
        private static string GetFileHash(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return "0";
            }

            try
            {
                using (var md5 = MD5.Create())
                {
                    string extendedPath = @"\\?\" + filePath;
                    using (var stream = File.OpenRead(extendedPath))
                    {
                        byte[] hashBytes = md5.ComputeHash(stream);
                        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception);
            }

            return "0";
        }

        private static void SafeLog(string message)
        {
            try
            {
                Debug.Log(message);
            }
            catch
            {
            }
        }

        private static void SafeLogError(string message)
        {
            try
            {
                Debug.LogError(message);
            }
            catch
            {
            }
        }
    }
}
