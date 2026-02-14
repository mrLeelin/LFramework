using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor
{
    public static class FolderComparer
    {
        /// <summary>
        /// 对比两个文件夹中的文件，找出不一样的文件并复制到目标文件夹。
        /// </summary>
        /// <param name="sourceFolder1">文件夹1路径</param>
        /// <param name="sourceFolder2">文件夹2路径</param>
        /// <param name="outputFolder">输出文件夹路径</param>
        public static void CompareAndCopyDifferentFiles(string sourceFolder1, string sourceFolder2, string outputFolder)
        {
            if (!Directory.Exists(sourceFolder1) || !Directory.Exists(sourceFolder2))
            {
                Debug.LogError("源文件夹不存在！");
                return;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            // 获取两个文件夹中的所有文件路径
            var files1 = Directory.GetFiles(sourceFolder1, "*", SearchOption.AllDirectories);
            var files2 = Directory.GetFiles(sourceFolder2, "*", SearchOption.AllDirectories);

            // 创建哈希字典
            var fileHashes2 = new Dictionary<string, string>();
            foreach (var filePath in files2)
            {
                if (filePath.Contains("catalog_"))
                {
                    continue;
                }
                string relativePath = Path.GetRelativePath(sourceFolder2, filePath).Replace("\\", "/");
                fileHashes2[relativePath] = GetFileHash(filePath);
            }

            // 对比文件夹1的文件
            foreach (var filePath in files1)
            {
                string relativePath = Path.GetRelativePath(sourceFolder1, filePath).Replace("\\", "/");
                string fileHash = GetFileHash(filePath);

                if (!fileHashes2.TryGetValue(relativePath, out var hash2) || hash2 != fileHash ||
                    fileHash.Equals("0") || hash2.Equals("0"))
                {
                    // 文件不同或不存在于文件夹2，复制到目标文件夹
                    string outputFilePath = Path.Combine(outputFolder, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath) ?? string.Empty);
                    File.Copy(filePath, outputFilePath, true);
                    Debug.Log($"不同文件已复制: {relativePath}");
                }
            }
        }

        /// <summary>
        /// 计算文件的 MD5 哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>哈希值字符串</returns>
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
                        return System.BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }

            return "0";
        }
    }
}