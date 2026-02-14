using System.Diagnostics;
using System.IO;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace LFramework.Editor.Builder
{
    public static class PythonRunner
    {
        public static bool RunPythonScript(string scriptName, string args = "")
        {
            // 获取脚本完整路径（放在Assets/StreamingAssets下）
            var scriptPath = Path.Combine(Application.dataPath + "/../BuildBat/", scriptName);

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = GetPythonExecutable(), // 自动获取正确的Python路径
                Arguments = $"\"{scriptPath}\" {args}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            var isExitErrorMessage = false;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Debug.Log(result);
                }

                string error = process.StandardError.ReadToEnd();
                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError(error);
                    isExitErrorMessage = true;
                }
            }

            return !isExitErrorMessage; 
        }

        private static string GetPythonExecutable()
        {
            // 根据平台返回正确的Python命令
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.WindowsPlayer:
                    // Windows尝试多个可能的Python路径
                    string[] winPaths =
                    {
                        "python", // 如果已在PATH中
                        "python3", // Python 3
                        "C:/Python39/python.exe", // 常见安装路径
                        "C:/Python38/python.exe",
                        "C:/Python37/python.exe"
                    };
                    return FindValidExecutable(winPaths);

                case RuntimePlatform.OSXEditor:
                case RuntimePlatform.OSXPlayer:
                    // Mac尝试多个可能的Python路径
                    string[] macPaths =
                    {
                        "python3", // 推荐使用python3
                        "python", // 系统Python（不推荐）
                        "/usr/local/bin/python3", // Homebrew安装路径
                        "/opt/homebrew/bin/python3" // M1 Homebrew路径
                    };
                    return FindValidExecutable(macPaths);

                default:
                    return "python3"; // 其他平台默认尝试python3
            }
        }

        private static string FindValidExecutable(string[] possiblePaths)
        {
            foreach (var path in possiblePaths)
            {
                try
                {
                    ProcessStartInfo info = new ProcessStartInfo(path, "--version")
                    {
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (Process p = Process.Start(info))
                    {
                        p.WaitForExit(1000);
                        if (p.ExitCode == 0)
                        {
                            Debug.Log($"Using Python at: {path}");
                            return path;
                        }
                    }
                }
                catch
                {
                }
            }

            Debug.LogError("No valid Python executable found!");
            return "python3"; // 最后尝试
        }
    }
}