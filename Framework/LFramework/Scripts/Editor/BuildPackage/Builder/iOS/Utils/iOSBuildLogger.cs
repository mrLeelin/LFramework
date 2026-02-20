using System;
using UnityEngine;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS 构建日志工具
    /// 提供统一的日志记录格式，便于调试和问题追踪
    /// </summary>
    public static class iOSBuildLogger
    {
        /// <summary>
        /// 日志前缀
        /// 所有日志消息都会带上这个前缀，便于过滤和查找
        /// </summary>
        private const string LOG_PREFIX = "[iOSBuildEventHandler]";

        /// <summary>
        /// 记录步骤开始
        /// </summary>
        /// <param name="stepName">步骤名称</param>
        public static void LogStep(string stepName)
        {
            Debug.Log($"{LOG_PREFIX} Starting {stepName}...");
        }

        /// <summary>
        /// 记录步骤成功完成
        /// </summary>
        /// <param name="stepName">步骤名称</param>
        public static void LogSuccess(string stepName)
        {
            Debug.Log($"{LOG_PREFIX} {stepName} completed successfully.");
        }

        /// <summary>
        /// 记录警告信息
        /// </summary>
        /// <param name="message">警告消息</param>
        public static void LogWarning(string message)
        {
            Debug.LogWarning($"{LOG_PREFIX} {message}");
        }

        /// <summary>
        /// 记录错误信息
        /// </summary>
        /// <param name="message">错误消息</param>
        public static void LogError(string message)
        {
            Debug.LogError($"{LOG_PREFIX} {message}");
        }

        /// <summary>
        /// 记录异常信息
        /// 包含异常消息和堆栈跟踪
        /// </summary>
        /// <param name="stepName">步骤名称</param>
        /// <param name="ex">异常对象</param>
        public static void LogException(string stepName, Exception ex)
        {
            Debug.LogError($"{LOG_PREFIX} {stepName} failed: {ex.Message}");
            Debug.LogError($"{LOG_PREFIX} Stack trace: {ex.StackTrace}");
        }

        /// <summary>
        /// 记录普通信息
        /// </summary>
        /// <param name="message">信息消息</param>
        public static void LogInfo(string message)
        {
            Debug.Log($"{LOG_PREFIX} {message}");
        }
    }
}
