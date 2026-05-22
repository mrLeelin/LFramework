using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// iOS platform build settings.
    /// </summary>
    [CreateAssetMenu(fileName = "iOSSetting", menuName = "LFramework/Settings/iOSSetting")]
    public class iOSSetting : BaseSetting
    {
        [FoldoutGroup("Signing")]
        [SerializeField] private string bundleIdentifier = "com.company.game";

        [FoldoutGroup("Signing")]
        [SerializeField] private string targetOSVersion = "12.0";

        [FoldoutGroup("Signing")]
        [SerializeField] private bool requiresFullScreen = true;

        [FoldoutGroup("Signing/Base")]
        [SerializeField] private string appleDevelopTeamId;

        [FoldoutGroup("Signing/Development")]
        [SerializeField] private string developmentMobileProvisionUUid;

        [FoldoutGroup("Signing/Development")]
        [SerializeField] private string developmentMobileProvisionProfileName;

        [FoldoutGroup("Signing/Development")]
        [SerializeField] private string developmentCodeSignIdentity = "Apple Development";

        [FoldoutGroup("Signing/Distribution")]
        [SerializeField] private string distributionMobileProvisionUUid;

        [FoldoutGroup("Signing/Distribution")]
        [SerializeField] private string distributionMobileProvisionProfileName;

        [FoldoutGroup("Signing/Distribution")]
        [SerializeField] private string distributionCodeSignIdentity = "Apple Distribution";

        [FoldoutGroup("IPA Export")]
        [SerializeField] private bool autoExportIpa;

        [FoldoutGroup("IPA Export")]
        [SerializeField] private string exportOptionsFileName = "AppStoreExportOptions.plist";

        [FoldoutGroup("IPA Export")]
        [SerializeField] private string developmentExportOptionsFileName = "AppStoreExportOptionsDev.plist";

        [FoldoutGroup("App Store Upload")]
        [SerializeField] private bool autoUploadToAppStore;

        [FoldoutGroup("App Store Upload")]
        [SerializeField] private bool validateAppBeforeUpload = true;

        [FoldoutGroup("App Store Upload")]
        [SerializeField] private string appStoreUserName;

        [FoldoutGroup("App Store Upload")]
        [SerializeField] private string appStorePassword;

        [FoldoutGroup("Deep Link")]
        [SerializeField] private string urlScheme;

        [FoldoutGroup("Deep Link")]
        [SerializeField] private string bundleURLName;

        [FoldoutGroup("App Controller")]
        [SerializeField] private string appControllerName;

        [FoldoutGroup("Privacy")]
        [SerializeField, TextArea] private string cameraUsageDescription;

        [FoldoutGroup("Privacy")]
        [SerializeField, TextArea] private string locationUsageDescription;

        [FoldoutGroup("Capabilities")]
        [SerializeField] private bool enableKeychainSharing = true;

        [FoldoutGroup("Capabilities")]
        [SerializeField] private bool enablePushNotifications;

        [FoldoutGroup("Capabilities")]
        [SerializeField] private bool enableGameCenter;

        [FoldoutGroup("Capabilities")]
        [SerializeField] private bool enableInAppPurchase;

        [FoldoutGroup("Capabilities")]
        [SerializeField] private bool enableSignInWithApple;

        public string BundleIdentifier => bundleIdentifier;
        public string TargetOSVersion => targetOSVersion;
        public bool RequiresFullScreen => requiresFullScreen;
        public string CameraUsageDescription => cameraUsageDescription;
        public string LocationUsageDescription => locationUsageDescription;
        public string URLScheme => urlScheme;
        public string BundleURLName => bundleURLName;
        public string AppControllerName => appControllerName;
        public string AppleDevelopTeamId => appleDevelopTeamId;
        public bool AutoExportIpa => autoExportIpa;
        public bool AutoUploadToAppStore => autoUploadToAppStore;
        public bool ValidateAppBeforeUpload => validateAppBeforeUpload;
        public string AppStoreUserName => appStoreUserName;
        public string AppStorePassword => appStorePassword;
        public bool EnableKeychainSharing => enableKeychainSharing;
        public bool EnablePushNotifications => enablePushNotifications;
        public bool EnableGameCenter => enableGameCenter;
        public bool EnableInAppPurchase => enableInAppPurchase;
        public bool EnableSignInWithApple => enableSignInWithApple;

        public string DevelopmentMobileProvisionUUid => developmentMobileProvisionUUid;
        public string DevelopmentMobileProvisionProfileName => developmentMobileProvisionProfileName;
        public string DevelopmentCodeSignIdentity => developmentCodeSignIdentity;
        public string DistributionMobileProvisionUUid => distributionMobileProvisionUUid;
        public string DistributionMobileProvisionProfileName => distributionMobileProvisionProfileName;
        public string DistributionCodeSignIdentity => distributionCodeSignIdentity;

        public string ExportOptionsFileName => string.IsNullOrWhiteSpace(exportOptionsFileName)
            ? "AppStoreExportOptions.plist"
            : exportOptionsFileName;

        public string DevelopmentExportOptionsFileName => string.IsNullOrWhiteSpace(developmentExportOptionsFileName)
            ? "AppStoreExportOptionsDev.plist"
            : developmentExportOptionsFileName;

        public override bool Validate(out string errorMessage)
        {
            if (!ValidateCommon(out errorMessage))
            {
                return false;
            }

            if (IsUnset(DevelopmentMobileProvisionUUid))
            {
                errorMessage = "Development Mobile Provision UUID is required.";
                return false;
            }

            if (IsUnset(DevelopmentMobileProvisionProfileName))
            {
                errorMessage = "Development Mobile Provision Profile Name is required.";
                return false;
            }

            if (IsUnset(DevelopmentCodeSignIdentity))
            {
                errorMessage = "Development Code Sign Identity is required.";
                return false;
            }

            if (IsUnset(DistributionMobileProvisionUUid))
            {
                errorMessage = "Distribution Mobile Provision UUID is required.";
                return false;
            }

            if (IsUnset(DistributionMobileProvisionProfileName))
            {
                errorMessage = "Distribution Mobile Provision Profile Name is required.";
                return false;
            }

            if (IsUnset(DistributionCodeSignIdentity))
            {
                errorMessage = "Distribution Code Sign Identity is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        public bool ValidateForBuild(bool isRelease, out string errorMessage)
        {
            if (!ValidateCommon(out errorMessage))
            {
                return false;
            }

            string buildMode = isRelease ? "Distribution" : "Development";
            string profileUuid = isRelease ? DistributionMobileProvisionUUid : DevelopmentMobileProvisionUUid;
            string profileName = isRelease ? DistributionMobileProvisionProfileName : DevelopmentMobileProvisionProfileName;
            string signingIdentity = isRelease ? DistributionCodeSignIdentity : DevelopmentCodeSignIdentity;

            if (IsUnset(profileUuid))
            {
                errorMessage = $"{buildMode} Mobile Provision UUID is required.";
                return false;
            }

            if (IsUnset(profileName))
            {
                errorMessage = $"{buildMode} Mobile Provision Profile Name is required.";
                return false;
            }

            if (IsUnset(signingIdentity))
            {
                errorMessage = $"{buildMode} Code Sign Identity is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private bool ValidateCommon(out string errorMessage)
        {
            if (IsUnset(bundleIdentifier))
            {
                errorMessage = "Bundle Identifier is required.";
                return false;
            }

            if (IsUnset(targetOSVersion))
            {
                errorMessage = "Target OS Version is required.";
                return false;
            }

            if (IsUnset(appleDevelopTeamId))
            {
                errorMessage = "Apple Developer Team ID is required.";
                return false;
            }

            errorMessage = null;
            return true;
        }

        private static bool IsUnset(string value)
        {
            return string.IsNullOrWhiteSpace(value) ||
                   value.Trim().StartsWith("TODO", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
