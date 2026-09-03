using HybridCLR.Editor.BuildProcessors;
using HybridCLR.Editor.Installer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HybridCLR.Editor.Commands
{
    public static class StripAOTDllCommand
    {
        static BuildOptions GetBuildPlayerOptions(BuildTarget buildTarget)
        {
            BuildOptions options = BuildOptions.None;
            bool development = EditorUserBuildSettings.development;
            if (development)
            {
                options |= BuildOptions.Development;
            }

            if (EditorUserBuildSettings.allowDebugging && development)
            {
                options |= BuildOptions.AllowDebugging;
            }

            if (EditorUserBuildSettings.connectProfiler && (development || buildTarget == BuildTarget.WSAPlayer))
            {
                options |= BuildOptions.ConnectWithProfiler;
            }

            if (EditorUserBuildSettings.buildWithDeepProfilingSupport && development)
            {
                options |= BuildOptions.EnableDeepProfilingSupport;
            }

#if UNITY_2021_2_OR_NEWER
            options |= BuildOptions.CleanBuildCache;
#endif

            return options;
        }

        private static string GetLocationPathName(string buildDir, BuildTarget target)
        {
            switch(target)
            {
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64: return $"{buildDir}/{PlayerSettings.productName}.exe";
                case BuildTarget.StandaloneOSX: return buildDir;
                case BuildTarget.iOS: return buildDir;
                case BuildTarget.Android: return buildDir;
                case BuildTarget.StandaloneLinux64: return $"{buildDir}/{PlayerSettings.productName}";
                default: return buildDir;
            }
        }

        public static void GenerateStripedAOTDlls(BuildTarget target)
        {
            string outputPath = $"{SettingsUtil.HybridCLRDataDir}/StrippedAOTDllsTempProj/{target}";
            BashUtil.RemoveDir(outputPath);

            var buildOptions = GetBuildPlayerOptions(target);

            bool oldExportAndroidProj = EditorUserBuildSettings.exportAsGoogleAndroidProject;
#if UNITY_EDITOR_OSX
            bool oldCreateSolution = UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject;
#elif UNITY_EDITOR_WIN
            bool oldCreateSolution = UnityEditor.WindowsStandalone.UserBuildSettings.createSolution;
#endif
#if TUANJIE_2022_3_OR_NEWER
            bool oldOpenHarmonyProj = EditorUserBuildSettings.exportAsOpenHarmonyProject;
#endif
            bool oldBuildScriptsOnly = EditorUserBuildSettings.buildScriptsOnly;

            string oldBuildLocation = EditorUserBuildSettings.GetBuildLocation(target);
            try
            {
                CheckSettings.DisableMethodBridgeDevelopmentFlagChecking = true;
                EditorUserBuildSettings.buildScriptsOnly = true;

                string location = GetLocationPathName(outputPath, target);
                EditorUserBuildSettings.SetBuildLocation(target, location);

                switch (target)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                    {
    #if UNITY_EDITOR_WIN
                        UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = true;
    #endif
                            break;
                    }
                    case BuildTarget.StandaloneOSX:
                    {
    #if UNITY_EDITOR_OSX
                        UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = true;
    #endif
                        break;
                    }
    #if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.HMIAndroid:
    #endif
                    case BuildTarget.Android:
                    {
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = true;
                        break;
                    }
    #if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.OpenHarmony:
                    {
                        EditorUserBuildSettings.exportAsOpenHarmonyProject = true;
                        break;
                    }
    #endif
                }

                Debug.Log($"GenerateStripedAOTDlls build option:{buildOptions}");

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions()
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = location,
                    options = buildOptions,
                    target = target,
                    targetGroup = BuildPipeline.GetBuildTargetGroup(target),
#if UNITY_SERVER
                    subtarget = (int)StandaloneBuildSubtarget.Server,
#endif
                };

                var report = BuildPipeline.BuildPlayer(buildPlayerOptions);



                if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                {
                    throw CreateBuildFailureException(target, report);
                }
            }
            finally
            {
                CheckSettings.DisableMethodBridgeDevelopmentFlagChecking = false;
                EditorUserBuildSettings.buildScriptsOnly = oldBuildScriptsOnly;
                EditorUserBuildSettings.SetBuildLocation(target, oldBuildLocation);

                switch (target)
                {
                    case BuildTarget.StandaloneWindows:
                    case BuildTarget.StandaloneWindows64:
                    {
#if UNITY_EDITOR_WIN
                        UnityEditor.WindowsStandalone.UserBuildSettings.createSolution = oldCreateSolution;
#endif
                        break;
                    }
                    case BuildTarget.StandaloneOSX:
                    {
#if UNITY_EDITOR_OSX
                            UnityEditor.OSXStandalone.UserBuildSettings.createXcodeProject = oldCreateSolution;
#endif
                        break;
                    }
#if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.HMIAndroid:
#endif
                    case BuildTarget.Android:
                    {
                        EditorUserBuildSettings.exportAsGoogleAndroidProject = oldExportAndroidProj;
                        break;
                    }
#if TUANJIE_2022_3_OR_NEWER
                    case BuildTarget.OpenHarmony:
                    {
                        EditorUserBuildSettings.exportAsOpenHarmonyProject = oldOpenHarmonyProj;
                        break;
                    }
#endif
                }
            }
            Debug.Log($"GenerateStripedAOTDlls target:{target} path:{outputPath}");
        }

        private static Exception CreateBuildFailureException(BuildTarget target, BuildReport report)
        {
            IEnumerable<string> errorMessages = report.steps
                .SelectMany(step => step.messages)
                .Where(message => message.type == LogType.Error
                    || message.type == LogType.Exception
                    || message.type == LogType.Assert)
                .Select(message => message.content);

            return new Exception(FormatBuildFailureMessage(
                target.ToString(),
                report.summary.result.ToString(),
                report.summary.totalErrors,
                report.summary.totalWarnings,
                report.summary.outputPath,
                errorMessages));
        }

        // Keep the BuildPlayer diagnostic when HybridCLR's temporary stripping build fails.
        private static string FormatBuildFailureMessage(
            string target,
            string result,
            int totalErrors,
            int totalWarnings,
            string outputPath,
            IEnumerable<string> errorMessages)
        {
            const int MaxReportedErrors = 20;
            string[] reportedErrors = errorMessages?
                .Where(message => !string.IsNullOrWhiteSpace(message))
                .Distinct()
                .Take(MaxReportedErrors)
                .ToArray() ?? Array.Empty<string>();

            var builder = new StringBuilder();
            builder.Append("GenerateStripedAOTDlls failed: ")
                .Append($"target={target}, result={result}, errors={totalErrors}, ")
                .Append($"warnings={totalWarnings}, output={outputPath}.");

            if (reportedErrors.Length == 0)
            {
                builder.Append(" BuildReport did not retain an error message.");
                return builder.ToString();
            }

            builder.AppendLine().Append("BuildReport errors:");
            foreach (string error in reportedErrors)
            {
                builder.AppendLine(error);
            }
            return builder.ToString();
        }
    }
}
