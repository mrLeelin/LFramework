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
        [SerializeField] private string mobileProvisionUUid;

        [FoldoutGroup("Signing/Base")]
        [SerializeField] private string mobileProvisionProfileName;

        [FoldoutGroup("Signing/Base")]
        [SerializeField] private string appleDevelopTeamId;

        [FoldoutGroup("Signing/Base")]
        [SerializeField] private string codeSignIdentity;

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
        public string MobileProvisionUUid => mobileProvisionUUid;
        public string MobileProvisionProfileName => mobileProvisionProfileName;
        public string AppleDevelopTeamId => appleDevelopTeamId;
        public string CodeSignIdentity => codeSignIdentity;
        public bool AutoExportIpa => autoExportIpa;
        public bool EnableKeychainSharing => enableKeychainSharing;
        public bool EnablePushNotifications => enablePushNotifications;
        public bool EnableGameCenter => enableGameCenter;
        public bool EnableInAppPurchase => enableInAppPurchase;
        public bool EnableSignInWithApple => enableSignInWithApple;

        public string DevelopmentMobileProvisionUUid => IsUnset(developmentMobileProvisionUUid)
            ? mobileProvisionUUid
            : developmentMobileProvisionUUid;

        public string DevelopmentMobileProvisionProfileName => IsUnset(developmentMobileProvisionProfileName)
            ? mobileProvisionProfileName
            : developmentMobileProvisionProfileName;

        public string DistributionMobileProvisionUUid => IsUnset(distributionMobileProvisionUUid)
            ? mobileProvisionUUid
            : distributionMobileProvisionUUid;

        public string DistributionMobileProvisionProfileName => IsUnset(distributionMobileProvisionProfileName)
            ? mobileProvisionProfileName
            : distributionMobileProvisionProfileName;

        public string DevelopmentCodeSignIdentity => IsUnset(developmentCodeSignIdentity)
            ? codeSignIdentity
            : developmentCodeSignIdentity;

        public string DistributionCodeSignIdentity => IsUnset(distributionCodeSignIdentity)
            ? codeSignIdentity
            : distributionCodeSignIdentity;

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

            if (IsUnset(mobileProvisionUUid))
            {
                errorMessage = "Mobile Provision UUID is required.";
                return false;
            }

            if (IsUnset(mobileProvisionProfileName))
            {
                errorMessage = "Mobile Provision Profile Name is required.";
                return false;
            }

            if (IsUnset(codeSignIdentity))
            {
                errorMessage = "Code Sign Identity is required.";
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
