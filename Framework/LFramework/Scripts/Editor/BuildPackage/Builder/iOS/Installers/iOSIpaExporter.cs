using System;
using System.IO;
using LFramework.Editor.Builder.iOS;
using LFramework.Editor.Builder.iOS.Configurators;
using UnityEngine;

namespace LFramework.Editor.Builder.iOS.Installers
{
    /// <summary>
    /// Optionally runs xcodebuild archive/exportArchive on macOS.
    /// </summary>
    public sealed class iOSIpaExporter
    {
        public const string ArchiveExportShellCommand = "bash";

        private readonly iOSBuildConfig _config;

        public iOSIpaExporter(iOSBuildConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        public void Export()
        {
            iOSExportOptionsWriter.Write(_config);
            string scriptPath = iOSArchiveExportScriptWriter.Write(_config);

            if (!_config.AutoExportIpa)
            {
                iOSBuildLogger.LogInfo("AutoExportIpa is disabled. Run export_ipa.sh on macOS to generate the IPA.");
                return;
            }

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                throw new InvalidOperationException(
                    "AutoExportIpa requires macOS with Xcode installed. Disable AutoExportIpa on non-macOS machines.");
            }

            if (!Directory.Exists(_config.XcodeWorkspacePath) && !Directory.Exists(_config.XcodeProjectPath))
            {
                throw new InvalidOperationException($"Xcode project was not found under {_config.OutputPath}.");
            }

            var chmodResult = iOSShellExecutor.Execute("chmod", $"+x {iOSShellExecutor.EscapeArgument(scriptPath)}");
            iOSShellExecutor.ValidateResult(chmodResult, "chmod export_ipa.sh");

            var exportResult = iOSShellExecutor.Execute(
                ArchiveExportShellCommand,
                iOSShellExecutor.EscapeArgument(scriptPath),
                _config.BuildRootPath);
            iOSShellExecutor.ValidateResult(exportResult, "xcodebuild archive/exportArchive");

            iOSBuildLogger.LogInfo($"IPA export completed: {_config.IpaExportPath}");
        }
    }
}
