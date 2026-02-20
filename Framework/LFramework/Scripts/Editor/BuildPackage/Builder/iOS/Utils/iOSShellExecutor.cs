using System;
using System.Diagnostics;
using System.Text;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS Shell 命令执行工具
    /// 提供安全的 Shell 命令执行功能，防止命令注入
    /// </summary>
    public static class iOSShellExecutor
    {
        /// <summary>
        /// Shell 命令执行结果
        /// </summary>
        public class ExecutionResult
        {
            /// <summary>
            /// 退出码（0 表示成功）
            /// </summary>
            public int ExitCode { get; set; }

            /// <summary>
            /// 标准输出内容
            /// </summary>
            public string Output { get; set; }

            /// <summary>
            /// 标准错误输出内容
            /// </summary>
            public string Error { get; set; }

            /// <summary>
            /// 是否执行成功
            /// </summary>
            public bool IsSuccess => ExitCode == 0;
        }

        /// <summary>
        /// 执行 Shell 命令
        /// </summary>
        /// <param name="command">命令名称（如 "sh", "chmod"）</param>
        /// <param name="arguments">命令参数</param>
        /// <param name="workingDirectory">工作目录</param>
        /// <returns>执行结果</returns>
        public static ExecutionResult Execute(string command, string arguments, string workingDirectory = null)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = workingDirectory ?? string.Empty
                }
            };

            try
            {
                process.Start();

                // 读取输出和错误流
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                process.WaitForExit();

                return new ExecutionResult
                {
                    ExitCode = process.ExitCode,
                    Output = output,
                    Error = error
                };
            }
            finally
            {
                process.Close();
            }
        }

        /// <summary>
        /// 转义 Shell 参数
        /// 防止命令注入攻击
        /// </summary>
        /// <param name="argument">原始参数</param>
        /// <returns>转义后的参数</returns>
        public static string EscapeArgument(string argument)
        {
            if (string.IsNullOrEmpty(argument))
            {
                return "\"\"";
            }

            // 如果参数包含空格、引号或特殊字符，需要转义
            if (argument.Contains(" ") || argument.Contains("\"") || argument.Contains("'") ||
                argument.Contains(";") || argument.Contains("&") || argument.Contains("|"))
            {
                // 转义内部的引号
                string escaped = argument.Replace("\"", "\\\"");
                // 用双引号包裹
                return $"\"{escaped}\"";
            }

            return argument;
        }

        /// <summary>
        /// 验证执行结果
        /// 如果执行失败，抛出异常
        /// </summary>
        /// <param name="result">执行结果</param>
        /// <param name="operationName">操作名称（用于错误消息）</param>
        /// <exception cref="InvalidOperationException">执行失败时抛出</exception>
        public static void ValidateResult(ExecutionResult result, string operationName)
        {
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"{operationName} failed with exit code {result.ExitCode}.\n" +
                    $"Output: {result.Output}\n" +
                    $"Error: {result.Error}");
            }
        }
    }
}
