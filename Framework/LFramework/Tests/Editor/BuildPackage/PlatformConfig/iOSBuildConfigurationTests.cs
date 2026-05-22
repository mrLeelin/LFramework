using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.iOS;
using LFramework.Editor.Builder.iOS.Configurators;
using LFramework.Editor.Builder.iOS.Installers;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage.PlatformConfig
{
    public class iOSBuildConfigurationTests
    {
        [Test]
        public void Validate_ShouldFail_WhenRequiredSigningFieldsAreMissing()
        {
            var setting = ScriptableObject.CreateInstance<iOSSetting>();
            SetPrivateField(setting, "bundleIdentifier", string.Empty);

            try
            {
                bool isValid = setting.Validate(out string errorMessage);

                Assert.That(isValid, Is.False);
                Assert.That(errorMessage, Does.Contain("Bundle Identifier"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void Validate_ShouldPass_WhenRequiredSigningFieldsAreConfigured()
        {
            var setting = CreateValidSetting();

            try
            {
                bool isValid = setting.Validate(out string errorMessage);

                Assert.That(isValid, Is.True);
                Assert.That(errorMessage, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void ValidateForBuild_ShouldOnlyRequireCurrentBuildSigningFields()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "developmentMobileProvisionUUid", "development-profile-uuid");
            SetPrivateField(setting, "developmentMobileProvisionProfileName", "Development Profile");
            SetPrivateField(setting, "developmentCodeSignIdentity", "Apple Development");
            SetPrivateField(setting, "distributionMobileProvisionUUid", "distribution-profile-uuid");
            SetPrivateField(setting, "distributionMobileProvisionProfileName", "Distribution Profile");
            SetPrivateField(setting, "distributionCodeSignIdentity", "Apple Distribution");

            try
            {
                Assert.That(setting.ValidateForBuild(false, out string debugError), Is.True);
                Assert.That(debugError, Is.Null);

                Assert.That(setting.ValidateForBuild(true, out string releaseError), Is.True);
                Assert.That(releaseError, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void Validate_ShouldFail_WhenSigningFieldsUseTodoPlaceholders()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "developmentMobileProvisionUUid", "TODO_PROFILE_UUID");
            SetPrivateField(setting, "developmentMobileProvisionProfileName", "TODO_PROFILE_NAME");

            try
            {
                bool isValid = setting.Validate(out string errorMessage);

                Assert.That(isValid, Is.False);
                Assert.That(errorMessage, Does.Contain("Development Mobile Provision UUID"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void CreateFromBuildSetting_ShouldCarryConfiguredIOSValues()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "urlScheme", "demoapp");
            SetPrivateField(setting, "bundleURLName", "com.company.game");
            SetPrivateField(setting, "appControllerName", "CustomAppController");
            SetPrivateField(setting, "cameraUsageDescription", "Camera access is required to scan cards.");
            SetPrivateField(setting, "locationUsageDescription", "Location access is required for regional services.");
            SetPrivateField(setting, "distributionMobileProvisionUUid", "distribution-profile-uuid");
            SetPrivateField(setting, "distributionMobileProvisionProfileName", "Distribution Profile");
            SetPrivateField(setting, "distributionCodeSignIdentity", "Apple Distribution");
            SetPrivateField(setting, "enablePushNotifications", true);
            SetPrivateField(setting, "enableGameCenter", true);
            SetPrivateField(setting, "enableInAppPurchase", true);
            SetPrivateField(setting, "enableSignInWithApple", true);
            SetPrivateField(setting, "autoUploadToAppStore", true);
            SetPrivateField(setting, "appStoreUserName", "ios-uploader@example.com");
            SetPrivateField(setting, "appStorePassword", "app-specific-password");
            SetPrivateField(setting, "validateAppBeforeUpload", true);

            try
            {
                var config = iOSBuildConfig.CreateFromBuildSetting(CreateBuildSetting(), setting, "Builds/IOS/Project");
                
                Assert.That(config.BundleIdentifier, Is.EqualTo("com.company.game"));
                Assert.That(config.AppleDevelopTeamId, Is.EqualTo("TEAM123456"));
                Assert.That(config.CodeSignIdentity, Is.EqualTo("Apple Distribution"));
                Assert.That(config.MobileProvisionUuid, Is.EqualTo("distribution-profile-uuid"));
                Assert.That(config.MobileProvisionProfileName, Is.EqualTo("Distribution Profile"));
                Assert.That(config.URLScheme, Is.EqualTo("demoapp"));
                Assert.That(config.BundleURLName, Is.EqualTo("com.company.game"));
                Assert.That(config.CustomAppControllerName, Is.EqualTo("CustomAppController"));
                Assert.That(config.CameraUsageDescription, Is.EqualTo("Camera access is required to scan cards."));
                Assert.That(config.LocationUsageDescription, Is.EqualTo("Location access is required for regional services."));
                Assert.That(config.ExportMethod, Is.EqualTo("app-store-connect"));
                Assert.That(config.ExportOptionsPath, Does.EndWith("AppStoreExportOptions.plist"));
                Assert.That(config.ArchivePath, Does.EndWith("Build.xcarchive"));
                Assert.That(config.IpaExportPath, Does.EndWith(
                    Path.Combine("IPA", "AppStore_Release_1.0.0.1")));
                Assert.That(config.XcodeConfiguration, Is.EqualTo("Release"));
                Assert.That(config.EnableKeychainSharing, Is.True);
                Assert.That(config.EnablePushNotifications, Is.True);
                Assert.That(config.EnableGameCenter, Is.True);
                Assert.That(config.EnableInAppPurchase, Is.True);
                Assert.That(config.EnableSignInWithApple, Is.True);
                Assert.That(config.AutoUploadToAppStore, Is.True);
                Assert.That(config.AppStoreUserName, Is.EqualTo("ios-uploader@example.com"));
                Assert.That(config.AppStorePassword, Is.EqualTo("app-specific-password"));
                Assert.That(config.ValidateAppBeforeUpload, Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void CreateFromBuildSetting_ShouldUseDevelopmentSigningForDebugBuilds()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "developmentMobileProvisionUUid", "development-profile-uuid");
            SetPrivateField(setting, "developmentMobileProvisionProfileName", "Development Profile");
            SetPrivateField(setting, "developmentCodeSignIdentity", "Apple Development");
            var buildSetting = CreateBuildSetting();
            buildSetting.isRelease = false;

            try
            {
                var config = iOSBuildConfig.CreateFromBuildSetting(buildSetting, setting, "Builds/IOS/Project");

                Assert.That(config.IsDevelopment, Is.True);
                Assert.That(config.CodeSignIdentity, Is.EqualTo("Apple Development"));
                Assert.That(config.MobileProvisionUuid, Is.EqualTo("development-profile-uuid"));
                Assert.That(config.MobileProvisionProfileName, Is.EqualTo("Development Profile"));
                Assert.That(config.ExportMethod, Is.EqualTo("development"));
                Assert.That(config.ExportOptionsPath, Does.EndWith("AppStoreExportOptionsDev.plist"));
                Assert.That(config.XcodeConfiguration, Is.EqualTo("Debug"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void Validate_ShouldNotRequireOptionalDeepLinkValues()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "urlScheme", string.Empty);
            SetPrivateField(setting, "bundleURLName", string.Empty);
            SetPrivateField(setting, "appControllerName", string.Empty);
            var config = iOSBuildConfig.CreateFromBuildSetting(CreateBuildSetting(), setting, "Builds/IOS/Project");

            try
            {
                Assert.DoesNotThrow(() => config.Validate());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void ExportOptionsWriter_ShouldGenerateReleaseExportOptions()
        {
            string tempRoot = CreateTempDirectory();
            var setting = CreateValidSetting();
            SetPrivateField(setting, "distributionMobileProvisionUUid", "distribution-profile-uuid");
            SetPrivateField(setting, "distributionMobileProvisionProfileName", "Distribution Profile");
            SetPrivateField(setting, "distributionCodeSignIdentity", "Apple Distribution");

            try
            {
                var config = iOSBuildConfig.CreateFromBuildSetting(CreateBuildSetting(), setting, Path.Combine(tempRoot, "Project"));

                iOSExportOptionsWriter.Write(config);

                Assert.That(File.Exists(config.ExportOptionsPath), Is.True);
                XElement root = LoadRootDict(config.ExportOptionsPath);
                Assert.That(GetString(root, "method"), Is.EqualTo("app-store-connect"));
                Assert.That(GetString(root, "signingStyle"), Is.EqualTo("manual"));
                Assert.That(GetString(root, "signingCertificate"), Is.EqualTo("Apple Distribution"));
                Assert.That(GetString(root, "teamID"), Is.EqualTo("TEAM123456"));
                Assert.That(GetString(GetDict(root, "provisioningProfiles"), "com.company.game"), Is.EqualTo("distribution-profile-uuid"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
                Directory.Delete(tempRoot, true);
            }
        }

        [Test]
        public void ArchiveExportScriptWriter_ShouldUseResolvedBuildConfigurationAndSigning()
        {
            string tempRoot = CreateTempDirectory();
            var setting = CreateValidSetting();
            SetPrivateField(setting, "developmentMobileProvisionUUid", "development-profile-uuid");
            SetPrivateField(setting, "developmentMobileProvisionProfileName", "Development Profile");
            SetPrivateField(setting, "developmentCodeSignIdentity", "Apple Development");
            var buildSetting = CreateBuildSetting();
            buildSetting.isRelease = false;

            try
            {
                var config = iOSBuildConfig.CreateFromBuildSetting(buildSetting, setting, Path.Combine(tempRoot, "Project"));

                string scriptPath = iOSArchiveExportScriptWriter.Write(config);

                string script = File.ReadAllText(scriptPath);
                Assert.That(iOSIpaExporter.ArchiveExportShellCommand, Is.EqualTo("bash"));
                Assert.That(script, Does.Contain("project_selector=(-workspace"));
                Assert.That(script, Does.Contain("project_selector=(-project"));
                Assert.That(script, Does.Contain("xcodebuild archive \"${project_selector[@]}\""));
                Assert.That(script, Does.Contain("-configuration 'Debug'"));
                Assert.That(script, Does.Contain("DEVELOPMENT_TEAM='TEAM123456'"));
                Assert.That(script, Does.Contain("PROVISIONING_PROFILE='development-profile-uuid'"));
                Assert.That(script, Does.Contain("PROVISIONING_PROFILE_SPECIFIER='Development Profile'"));
                Assert.That(script, Does.Contain("CODE_SIGN_IDENTITY='Apple Development'"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
                Directory.Delete(tempRoot, true);
            }
        }

        [Test]
        public void PlistConfigurator_ShouldWriteRequiredPrivacyDescriptions()
        {
            string tempRoot = CreateTempDirectory();
            var setting = CreateValidSetting();
            SetPrivateField(setting, "cameraUsageDescription", "Camera access is required to scan cards.");
            SetPrivateField(setting, "locationUsageDescription", "Location access is required for regional services.");
            string outputPath = Path.Combine(tempRoot, "Project");
            Directory.CreateDirectory(outputPath);
            File.WriteAllText(Path.Combine(outputPath, "Info.plist"), CreateEmptyPlist());

            try
            {
                var config = iOSBuildConfig.CreateFromBuildSetting(CreateBuildSetting(), setting, outputPath);

                new iOSPlistConfigurator(config).Configure();

                XElement root = LoadRootDict(Path.Combine(outputPath, "Info.plist"));
                Assert.That(GetBoolean(root, "ITSAppUsesNonExemptEncryption"), Is.False);
                Assert.That(GetString(root, "NSUserTrackingUsageDescription"), Is.Not.Empty);
                Assert.That(GetString(root, "NSCameraUsageDescription"), Is.EqualTo("Camera access is required to scan cards."));
                Assert.That(GetString(root, "NSLocationWhenInUseUsageDescription"), Is.EqualTo("Location access is required for regional services."));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
                Directory.Delete(tempRoot, true);
            }
        }

        [Test]
        public void AppStoreUploader_ShouldRequireReleaseAutoExportAndCredentials()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "autoUploadToAppStore", true);
            SetPrivateField(setting, "appStoreUserName", "ios-uploader@example.com");
            SetPrivateField(setting, "appStorePassword", "app-specific-password");
            var buildSetting = CreateBuildSetting();
            buildSetting.isRelease = false;

            try
            {
                var debugConfig = iOSBuildConfig.CreateFromBuildSetting(buildSetting, setting, "Builds/IOS/Project");

                Assert.That(
                    () => new iOSAppStoreUploader(debugConfig).ValidateForUpload(),
                    Throws.InvalidOperationException.With.Message.Contains("Release"));

                buildSetting.isRelease = true;
                var releaseConfig = iOSBuildConfig.CreateFromBuildSetting(buildSetting, setting, "Builds/IOS/Project");

                Assert.That(
                    () => new iOSAppStoreUploader(releaseConfig).ValidateForUpload(),
                    Throws.InvalidOperationException.With.Message.Contains("AutoExportIpa"));

                SetPrivateField(setting, "autoExportIpa", true);
                SetPrivateField(setting, "appStoreUserName", string.Empty);
                var missingUserConfig = iOSBuildConfig.CreateFromBuildSetting(buildSetting, setting, "Builds/IOS/Project");

                Assert.That(
                    () => new iOSAppStoreUploader(missingUserConfig).ValidateForUpload(),
                    Throws.InvalidOperationException.With.Message.Contains("username"));

                SetPrivateField(setting, "appStoreUserName", "TODO_APP_STORE_USERNAME");
                SetPrivateField(setting, "appStorePassword", "TODO_APP_STORE_APP_PASSWORD");
                var placeholderConfig = iOSBuildConfig.CreateFromBuildSetting(buildSetting, setting, "Builds/IOS/Project");

                Assert.That(
                    () => new iOSAppStoreUploader(placeholderConfig).ValidateForUpload(),
                    Throws.InvalidOperationException.With.Message.Contains("username"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void AppStoreUploader_ShouldBuildAltoolValidateAndUploadCommands()
        {
            var config = new iOSBuildConfig
            {
                AutoExportIpa = true,
                AutoUploadToAppStore = true,
                ValidateAppBeforeUpload = true,
                IsDevelopment = false,
                AppStoreUserName = "ios-uploader@example.com",
                AppStorePassword = "app-specific-password"
            };

            string ipaPath = "/tmp/build/My Game.ipa";

            string validateCommand = iOSAppStoreUploader.BuildAltoolArguments(config, ipaPath, validateOnly: true);
            string uploadCommand = iOSAppStoreUploader.BuildAltoolArguments(config, ipaPath, validateOnly: false);

            Assert.That(validateCommand, Does.Contain("--validate-app"));
            Assert.That(validateCommand, Does.Contain("-f \"/tmp/build/My Game.ipa\""));
            Assert.That(validateCommand, Does.Contain("-u ios-uploader@example.com"));
            Assert.That(validateCommand, Does.Contain("-p app-specific-password"));
            Assert.That(uploadCommand, Does.Contain("--upload-app"));
        }

        private static BuildSetting CreateBuildSetting()
        {
            return new BuildSetting
            {
                builderTarget = BuilderTarget.iOS,
                isRelease = true,
                appVersion = "1.0.0",
                versionCode = 1
            };
        }

        private static iOSSetting CreateValidSetting()
        {
            var setting = ScriptableObject.CreateInstance<iOSSetting>();
            SetPrivateField(setting, "bundleIdentifier", "com.company.game");
            SetPrivateField(setting, "targetOSVersion", "12.0");
            SetPrivateField(setting, "appleDevelopTeamId", "TEAM123456");
            SetPrivateField(setting, "developmentMobileProvisionUUid", "development-profile-uuid");
            SetPrivateField(setting, "developmentMobileProvisionProfileName", "Development Profile");
            SetPrivateField(setting, "developmentCodeSignIdentity", "Apple Development");
            SetPrivateField(setting, "distributionMobileProvisionUUid", "distribution-profile-uuid");
            SetPrivateField(setting, "distributionMobileProvisionProfileName", "Distribution Profile");
            SetPrivateField(setting, "distributionCodeSignIdentity", "Apple Distribution");
            return setting;
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "lframework-ios-build-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static string CreateEmptyPlist()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
                   "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n" +
                   "<plist version=\"1.0\">\n" +
                   "  <dict>\n" +
                   "  </dict>\n" +
                   "</plist>\n";
        }

        private static XElement LoadRootDict(string path)
        {
            return XDocument.Load(path).Root?.Element("dict")
                   ?? throw new InvalidOperationException($"Root dict not found in plist: {path}");
        }

        private static XElement GetDict(XElement dict, string key)
        {
            XElement value = FindValue(dict, key);
            Assert.That(value.Name.LocalName, Is.EqualTo("dict"));
            return value;
        }

        private static string GetString(XElement dict, string key)
        {
            XElement value = FindValue(dict, key);
            Assert.That(value.Name.LocalName, Is.EqualTo("string"));
            return value.Value;
        }

        private static bool GetBoolean(XElement dict, string key)
        {
            XElement value = FindValue(dict, key);
            return value.Name.LocalName == "true";
        }

        private static XElement FindValue(XElement dict, string key)
        {
            XElement keyElement = dict.Elements("key").FirstOrDefault(element => element.Value == key);
            if (keyElement == null)
            {
                throw new InvalidOperationException($"Key not found in plist dict: {key}");
            }

            XElement valueElement = keyElement.ElementsAfterSelf().FirstOrDefault();
            if (valueElement == null)
            {
                throw new InvalidOperationException($"Value not found for plist key: {key}");
            }

            return valueElement;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new MissingFieldException(target.GetType().FullName, fieldName);
            }

            field.SetValue(target, value);
        }
    }
}
