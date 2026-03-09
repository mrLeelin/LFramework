using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    /// <summary>
    /// 已下载 Tag 的持久化记录管理器
    /// 用于跟踪哪些 Tag 的资源已经被下载过，以便在强更阶段检查这些 Tag 的增量更新
    /// </summary>
    public static class DownloadedTagTracker
    {
        private const string KeyPrefix = "DownloadedTag_";

        /// <summary>
        /// 标记某个 Tag 已下载完成
        /// </summary>
        public static void MarkTagDownloaded(string packageName, string tag)
        {
            var key = GetKey(packageName, tag);
            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 检查某个 Tag 是否已经下载过
        /// </summary>
        public static bool IsTagDownloaded(string packageName, string tag)
        {
            var key = GetKey(packageName, tag);
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        /// <summary>
        /// 获取指定 Package 下所有已下载的 Tag 列表
        /// 注意：需要传入所有可能的 Tag 名称来检查，因为 PlayerPrefs 不支持枚举 key
        /// </summary>
        public static List<string> GetDownloadedTags(string packageName)
        {
            var result = new List<string>();
            foreach (var tag in _registeredTags)
            {
                if (IsTagDownloaded(packageName, tag))
                {
                    result.Add(tag);
                }
            }
            return result;
        }

        /// <summary>
        /// 注册一个 Tag 到跟踪列表（在游戏初始化时调用，注册所有可能的 Tag）
        /// </summary>
        public static void RegisterTag(string tag)
        {
            if (!_registeredTags.Contains(tag))
            {
                _registeredTags.Add(tag);
            }
        }

        /// <summary>
        /// 批量注册 Tag
        /// </summary>
        public static void RegisterTags(IEnumerable<string> tags)
        {
            foreach (var tag in tags)
            {
                RegisterTag(tag);
            }
        }

        /// <summary>
        /// 清除指定 Package 下所有已下载 Tag 的记录（用于清档或重装）
        /// </summary>
        public static void ClearAll(string packageName)
        {
            foreach (var tag in _registeredTags)
            {
                var key = GetKey(packageName, tag);
                PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 清除指定 Tag 的下载记录
        /// </summary>
        public static void ClearTag(string packageName, string tag)
        {
            var key = GetKey(packageName, tag);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private static string GetKey(string packageName, string tag)
        {
            return $"{KeyPrefix}{packageName}_{tag}";
        }

        private static readonly List<string> _registeredTags = new List<string>();
    }
}
