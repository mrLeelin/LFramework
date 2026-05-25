using System;
using System.IO;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Editor.Builder.iOS
{
    /// <summary>
    /// iOS build configuration resolved from BuildSetting and iOSSetting.
    /// </summary>
    public class iOSBuildConfig
    {
        public string OutputPath { get; set; }
        public string BuildRootPath { get; set; }
        public string LocalizationFolderPath { get; set; }
        public string URLScheme { get; set; }
        public string BundleURLName { get; set; }
        public string CustomAppControllerName { get; set; }
        public string PodCommandPath { get; set; }
        public bool IsDevelopment { get; set; }
        public bool AutoExportIpa { get; set; }
        public string IOSChannel { get; set; }
        public string BundleIdentifier { get; set; }
        public string AppleDevelopTeamId { get; set; }
        public string CodeSignIdentity { get; set; }
        public string MobileProvisionUuid { get; set; }
        public string MobileProvisionProfileName { get; set; }
        public string CameraUsageDescription { get; set; }
        public string LocationUsageDescription { get; set; }
        public string ExportMethod { get; set; }
        public string ExportOptionsPath { get; set; }
        public string ArchivePath { get; set; }
        public string IpaExportPath { get; set; }
        public string IpaName { get; set; }
        public bool AutoUploadToAppStore { get; set; }
        public bool ValidateAppBeforeUpload { get; set; }
        public string AppStoreUserName { get; set; }
        public string AppStorePassword { get; set; }
        public bool EnableKeychainSharing { get; set; }
        public bool EnablePushNotifications { get; set; }
        public bool EnableGameCenter { get; set; }
        public bool EnableInAppPurchase { get; set; }
        public bool EnableSignInWithApple { get; set; }
        public string XcodeConfiguration => IsDevelopment ? "Debug" : "Release";

        public string XcodeWorkspacePath => Path.Combine(OutputPath, "Unity-iPhone.xcworkspace");
        public string XcodeProjectPath => Path.Combine(OutputPath, "Unity-iPhone.xcodeproj");

        public static iOSBuildConfig CreateFromBuildSetting(BuildSetting buildSetting, iOSSetting iOSSetting, string outputPath)
        {
            if (buildSetting == null)
            {
                throw new ArgumentNullException(nameof(buildSetting));
            }

            if (iOSSetting == null)
            {
                throw new ArgumentNullException(nameof(iOSSetting));
            }

            string normalizedOutputPath = Path.GetFullPath(outputPath);
            string buildRootPath = Path.GetFullPath(Path.Combine(normalizedOutputPath, ".."));
            bool isDevelopment = !buildSetting.isRelease;
            string releaseName = isDevelopment ? "Debug" : "Release";
            string channelName = string.IsNullOrWhiteSpace(buildSetting.iosChannel)
                ? "AppStore"
                : SanitizePathPart(buildSetting.iosChannel);
            string exportOptionsFileName = isDevelopment
                ? iOSSetting.DevelopmentExportOptionsFileName
                : iOSSetting.ExportOptionsFileName;
            string exportMethod = isDevelopment
                ? iOSSetting.DevelopmentExportMethod
                : iOSSetting.DistributionExportMethod;

            return new iOSBuildConfig
            {
                OutputPath = normalizedOutputPath,
                BuildRootPath = buildRootPath,
                LocalizationFolderPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../ExportData/IOS/InfoPlist")),
                URLScheme = iOSSetting.URLScheme,
                BundleURLName = iOSSetting.BundleURLName,
                CustomAppControllerName = iOSSetting.AppControllerName,
                AppleDevelopTeamId = iOSSetting.AppleDevelopTeamId,
                CodeSignIdentity = isDevelopment
                    ? iOSSetting.DevelopmentCodeSignIdentity
                    : iOSSetting.DistributionCodeSignIdentity,
                MobileProvisionUuid = isDevelopment
                    ? iOSSetting.DevelopmentMobileProvisionUUid
                    : iOSSetting.DistributionMobileProvisionUUid,
                MobileProvisionProfileName = isDevelopment
                    ? iOSSetting.DevelopmentMobileProvisionProfileName
                    : iOSSetting.DistributionMobileProvisionProfileName,
                BundleIdentifier = iOSSetting.BundleIdentifier,
                IOSChannel = string.IsNullOrWhiteSpace(buildSetting.iosChannel)
                    ? "AppStore"
                    : buildSetting.iosChannel,
                CameraUsageDescription = iOSSetting.CameraUsageDescription,
                LocationUsageDescription = iOSSetting.LocationUsageDescription,
                IsDevelopment = isDevelopment,
                AutoExportIpa = iOSSetting.AutoExportIpa,
                ExportMethod = exportMethod,
                ExportOptionsPath = Path.Combine(buildRootPath, exportOptionsFileName),
                ArchivePath = Path.Combine(buildRootPath, "Archive", $"{channelName}_{releaseName}_{buildSetting.GetAppVersion()}", "Build.xcarchive"),
                IpaExportPath = Path.Combine(buildRootPath, "IPA", $"{channelName}_{releaseName}_{buildSetting.GetAppVersion()}"),
                IpaName = $"Build_{channelName}_{releaseName}_{buildSetting.GetAppVersion()}.ipa",
                AutoUploadToAppStore = iOSSetting.AutoUploadToAppStore,
                ValidateAppBeforeUpload = iOSSetting.ValidateAppBeforeUpload,
                AppStoreUserName = iOSSetting.AppStoreUserName,
                AppStorePassword = iOSSetting.AppStorePassword,
                EnableKeychainSharing = iOSSetting.EnableKeychainSharing,
                EnablePushNotifications = iOSSetting.EnablePushNotifications,
                EnableGameCenter = iOSSetting.EnableGameCenter,
                EnableInAppPurchase = iOSSetting.EnableInAppPurchase,
                EnableSignInWithApple = iOSSetting.EnableSignInWithApple,
                PodCommandPath = DetectPodCommandPath()
            };
        }

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                throw new InvalidOperationException("Output path is not configured.");
            }

            if (IsUnset(BundleIdentifier))
            {
                throw new InvalidOperationException("Bundle identifier is not configured in iOSSetting.");
            }

            if (IsUnset(AppleDevelopTeamId))
            {
                throw new InvalidOperationException("Apple Team ID is not configured in iOSSetting.");
            }

            if (IsUnset(CodeSignIdentity))
            {
                throw new InvalidOperationException("Code sign identity is not configured in iOSSetting.");
            }

            if (IsUnset(MobileProvisionUuid))
            {
                throw new InvalidOperationException("Mobile provisioning profile UUID is not configured in iOSSetting.");
            }

            if (IsUnset(MobileProvisionProfileName))
            {
                throw new InvalidOperationException("Mobile provisioning profile name is not configured in iOSSetting.");
            }

            if (IsUnset(ExportMethod))
            {
                throw new InvalidOperationException("iOS export method is not configured.");
            }

            if (AutoUploadToAppStore)
            {
                ValidateAppStoreUploadSettings();
            }

            string podfilePath = Path.Combine(OutputPath, iOSBuildConstants.PODFILE_PATH);
            if (Application.platform == RuntimePlatform.OSXEditor &&
                File.Exists(podfilePath) &&
                string.IsNullOrEmpty(PodCommandPath))
            {
                throw new InvalidOperationException(
                    "CocoaPods command not found. Please install CocoaPods: https://cocoapods.org/");
            }
        }

        private static string DetectPodCommandPath()
        {
            string[] fixedPaths =
            {
                "/opt/homebrew/bin/pod",
                "/usr/local/bin/pod"
            };

            foreach (string path in fixedPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                return null;
            }

            var whichResult = iOSShellExecutor.Execute("which", "pod");
            if (whichResult.IsSuccess)
            {
                string podPath = whichResult.Output.Trim();
                if (!string.IsNullOrWhiteSpace(podPath))
                {
                    return podPath;
                }
            }

            return null;
        }

        private static string SanitizePathPart(string value)
        {
            string result = value;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                result = result.Replace(invalidChar, '_');
            }

            return string.IsNullOrWhiteSpace(result) ? "AppStore" : result;
        }

        private static bool IsUnset(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   value.Trim().StartsWith("TODO", StringComparison.OrdinalIgnoreCase);
        }

        private void ValidateAppStoreUploadSettings()
        {
            if (IsDevelopment)
            {
                throw new InvalidOperationException("AutoUploadToAppStore only supports Release builds.");
            }

            if (!AutoExportIpa)
            {
                throw new InvalidOperationException(
                    "AutoUploadToAppStore requires AutoExportIpa because an IPA must exist before upload.");
            }

            if (IsUnset(AppStoreUserName))
            {
                throw new InvalidOperationException("App Store username is not configured in iOSSetting.");
            }

            if (IsUnset(AppStorePassword))
            {
                throw new InvalidOperationException("App Store app-specific password is not configured in iOSSetting.");
            }

            if (Application.platform != RuntimePlatform.OSXEditor)
            {
                throw new InvalidOperationException(
                    "AutoUploadToAppStore requires macOS with Xcode command line tools installed.");
            }
        }
    }
}
