using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace LFramework.Editor.Builder.iOS.Installers
{
    /// <summary>
    /// Optionally validates and uploads exported IPA files to App Store Connect with xcrun altool.
    /// Uses the plaintext App Store Connect account configured in iOSSetting.
    /// </summary>
    public sealed class iOSAppStoreUploader
    {
        private readonly iOSBuildConfig _config;

        public iOSAppStoreUploader(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Upload()
        {
            if (!_config.AutoUploadToAppStore)
            {
                iOSBuildLogger.LogInfo("AutoUploadToAppStore is disabled. Skipping App Store upload.");
                return;
            }

            ValidateForUpload();

            string ipaPath = ResolveIpaPath();
            if (_config.ValidateAppBeforeUpload)
            {
                ExecuteAltool(BuildAltoolArguments(_config, ipaPath, validateOnly: true), "App Store IPA validation");
            }

            ExecuteAltool(BuildAltoolArguments(_config, ipaPath, validateOnly: false), "App Store IPA upload");
            iOSBuildLogger.LogInfo($"App Store upload completed: {ipaPath}");
        }

        public void ValidateForUpload()
        {
            if (!_config.AutoUploadToAppStore)
            {
                return;
            }

            if (_config.IsDevelopment)
            {
                throw new InvalidOperationException("AutoUploadToAppStore only supports Release builds.");
            }

            if (!_config.AutoExportIpa)
            {
                throw new InvalidOperationException(
                    "AutoUploadToAppStore requires AutoExportIpa because an IPA must exist before upload.");
            }

            if (IsUnset(_config.AppStoreUserName))
            {
                throw new InvalidOperationException("App Store username is not configured in iOSSetting.");
            }

            if (IsUnset(_config.AppStorePassword))
            {
                throw new InvalidOperationException("App Store app-specific password is not configured in iOSSetting.");
            }

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                throw new InvalidOperationException(
                    "AutoUploadToAppStore requires macOS with Xcode command line tools installed.");
            }
        }

        public static string BuildAltoolArguments(iOSBuildConfig config, string ipaPath, bool validateOnly)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrWhiteSpace(ipaPath))
            {
                throw new ArgumentException("IPA path is required.", nameof(ipaPath));
            }

            string mode = validateOnly ? "--validate-app" : "--upload-app";
            return $"{mode} -f {iOSShellExecutor.EscapeArgument(ipaPath)} -t ios " +
                   $"-u {iOSShellExecutor.EscapeArgument(config.AppStoreUserName)} " +
                   $"-p {iOSShellExecutor.EscapeArgument(config.AppStorePassword)} --verbose";
        }

        private string ResolveIpaPath()
        {
            if (!Directory.Exists(_config.IpaExportPath))
            {
                throw new DirectoryNotFoundException($"IPA export folder was not found: {_config.IpaExportPath}");
            }

            string[] ipaFiles = Directory.GetFiles(_config.IpaExportPath, "*.ipa", SearchOption.TopDirectoryOnly);
            if (ipaFiles.Length == 0)
            {
                throw new FileNotFoundException($"No IPA file was found under {_config.IpaExportPath}.");
            }

            if (ipaFiles.Length > 1)
            {
                string fileList = string.Join(", ", ipaFiles.Select(Path.GetFileName));
                throw new InvalidOperationException(
                    $"Multiple IPA files were found under {_config.IpaExportPath}: {fileList}. Clean the folder and rebuild.");
            }

            return ipaFiles[0];
        }

        private static void ExecuteAltool(string arguments, string operationName)
        {
            iOSBuildLogger.LogInfo($"Running {operationName} with xcrun altool.");
            var result = iOSShellExecutor.Execute("xcrun", $"altool {arguments}");

            if (!string.IsNullOrWhiteSpace(result.Output))
            {
                iOSBuildLogger.LogInfo($"{operationName} output:\n{result.Output}");
            }

            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                iOSBuildLogger.LogWarning($"{operationName} stderr:\n{result.Error}");
            }

            iOSShellExecutor.ValidateResult(result, operationName);
        }

        private static bool IsUnset(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   value.Trim().StartsWith("TODO", StringComparison.OrdinalIgnoreCase);
        }
    }
}
