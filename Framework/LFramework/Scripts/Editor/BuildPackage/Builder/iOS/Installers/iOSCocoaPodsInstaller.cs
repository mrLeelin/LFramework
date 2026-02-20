using System;
using System.IO;
using System.Text;

namespace LFramework.Editor.Builder.iOS.Installers
{
    /// <summary>
    /// iOS CocoaPods 安装器
    /// 负责执行 CocoaPods 安装，自动检测 pod 命令路径
    /// </summary>
    public class iOSCocoaPodsInstaller
    {
        private readonly iOSBuildConfig _config;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="config">构建配置</param>
        public iOSCocoaPodsInstaller(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        /// <summary>
        /// 执行 CocoaPods 安装
        /// </summary>
        public void Install()
        {
            iOSBuildLogger.LogStep("CocoaPods installation");

            string podfilePath = Path.Combine(_config.OutputPath, iOSBuildConstants.PODFILE_PATH);
            iOSBuildLogger.LogInfo($"Podfile path: {podfilePath}");

            // 检查 Podfile 是否存在
            if (!File.Exists(podfilePath))
            {
                iOSBuildLogger.LogError("Podfile not found, skipping pod install");
                return;
            }

            try
            {
                // 创建安装脚本
                string scriptPath = CreateInstallScript();

                // 设置脚本执行权限
                SetScriptExecutable(scriptPath);

                // 执行安装脚本
                ExecuteInstallScript(scriptPath);

                iOSBuildLogger.LogSuccess("CocoaPods installation");
            }
            catch (Exception ex)
            {
                iOSBuildLogger.LogException("CocoaPods installation", ex);
                throw;
            }
        }

        /// <summary>
        /// 创建 pod install 脚本
        /// </summary>
        /// <returns>脚本文件路径</returns>
        private string CreateInstallScript()
        {
            string scriptPath = Path.Combine(_config.OutputPath, iOSBuildConstants.POD_INSTALL_SCRIPT_NAME);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("#!/bin/sh");
            sb.AppendLine("export LANG=en_US.UTF-8");
            sb.AppendLine($"cd {iOSShellExecutor.EscapeArgument(_config.OutputPath)}");
            sb.AppendLine($"{_config.PodCommandPath} install");

            // 写入脚本文件
            using (FileStream file = new FileStream(scriptPath, FileMode.Create))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(sb.ToString());
                file.Write(bytes, 0, bytes.Length);
                file.Flush();
            }

            iOSBuildLogger.LogInfo($"Created install script: {scriptPath}");
            return scriptPath;
        }

        /// <summary>
        /// 设置脚本可执行权限
        /// </summary>
        /// <param name="scriptPath">脚本路径</param>
        private void SetScriptExecutable(string scriptPath)
        {
            var result = iOSShellExecutor.Execute(
                "chmod",
                $"+x {Path.GetFileName(scriptPath)}",
                _config.OutputPath);

            if (!result.IsSuccess)
            {
                iOSBuildLogger.LogWarning($"Failed to set script executable: {result.Error}");
            }
            else
            {
                iOSBuildLogger.LogInfo("Set script executable permission");
            }
        }

        /// <summary>
        /// 执行安装脚本
        /// </summary>
        /// <param name="scriptPath">脚本路径</param>
        private void ExecuteInstallScript(string scriptPath)
        {
            iOSBuildLogger.LogInfo("Executing pod install...");

            var result = iOSShellExecutor.Execute(
                "sh",
                Path.GetFileName(scriptPath),
                _config.OutputPath);

            // 打印输出
            if (!string.IsNullOrEmpty(result.Output))
            {
                iOSBuildLogger.LogInfo($"Pod install output:\n{result.Output}");
            }

            // 打印错误（如果有）
            if (!string.IsNullOrEmpty(result.Error))
            {
                // CocoaPods 有时会将正常信息输出到 stderr，所以这里用 Warning 而不是 Error
                iOSBuildLogger.LogWarning($"Pod install stderr:\n{result.Error}");
            }

            // 验证执行结果
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Pod install failed with exit code {result.ExitCode}. " +
                    $"Please check the output above for details.");
            }

            iOSBuildLogger.LogInfo("Pod install completed successfully");
        }
    }
}
