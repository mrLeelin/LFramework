using System;
using System.IO;
using System.Text;

namespace LFramework.Editor.Builder.iOS.Configurators
{
    /// <summary>
    /// Writes a macOS shell script for archiving the generated Xcode project and exporting an IPA.
    /// </summary>
    public static class iOSArchiveExportScriptWriter
    {
        public static string Write(iOSBuildConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string scriptPath = Path.Combine(config.BuildRootPath, "export_ipa.sh");
            Directory.CreateDirectory(config.BuildRootPath);
            Directory.CreateDirectory(Path.GetDirectoryName(config.ArchivePath));
            Directory.CreateDirectory(config.IpaExportPath);

            string script =
                "#!/bin/bash\n" +
                "set -euo pipefail\n\n" +
                $"if [ -d {ShellQuote(config.XcodeWorkspacePath)} ]; then\n" +
                $"  project_selector=(-workspace {ShellQuote(config.XcodeWorkspacePath)})\n" +
                "else\n" +
                $"  project_selector=(-project {ShellQuote(config.XcodeProjectPath)})\n" +
                "fi\n\n" +
                "xcodebuild archive \"${project_selector[@]}\" \\\n" +
                "  -scheme Unity-iPhone \\\n" +
                $"  -configuration {ShellQuote(config.XcodeConfiguration)} \\\n" +
                $"  -archivePath {ShellQuote(config.ArchivePath)}\n\n" +
                "xcodebuild -exportArchive \\\n" +
                $"  -archivePath {ShellQuote(config.ArchivePath)} \\\n" +
                $"  -exportPath {ShellQuote(config.IpaExportPath)} \\\n" +
                $"  -exportOptionsPlist {ShellQuote(config.ExportOptionsPath)}\n";

            File.WriteAllText(scriptPath, script, new UTF8Encoding(false));
            iOSBuildLogger.LogInfo($"IPA export script written: {scriptPath}");
            return scriptPath;
        }

        private static string ShellQuote(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "''";
            }

            return $"'{value.Replace("'", "'\\''")}'";
        }
    }
}
