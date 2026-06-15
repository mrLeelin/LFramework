using System;
using System.Reflection;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.PlatformConfig;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage.PlatformConfig
{
    public class PlatformBundleVersionTests
    {
        private string _originalBundleVersion;
        private string _originalIosBuildNumber;
        private int _originalAndroidBundleVersionCode;

        [SetUp]
        public void SetUp()
        {
            _originalBundleVersion = PlayerSettings.bundleVersion;
            _originalIosBuildNumber = PlayerSettings.iOS.buildNumber;
            _originalAndroidBundleVersionCode = PlayerSettings.Android.bundleVersionCode;
        }

        [TearDown]
        public void TearDown()
        {
            PlayerSettings.bundleVersion = _originalBundleVersion;
            PlayerSettings.iOS.buildNumber = _originalIosBuildNumber;
            PlayerSettings.Android.bundleVersionCode = _originalAndroidBundleVersionCode;
        }

        [Test]
        public void AndroidConfigurePlatformSettings_ShouldApplyAppVersionToBundleVersion()
        {
            var buildSetting = new BuildSetting
            {
                builderTarget = BuilderTarget.Android,
                buildAndroidAppType = BuildAndroidAppType.ExportAndroidProject,
                appVersion = "2.3.4",
                versionCode = 56
            };
            var androidSetting = CreateValidAndroidSetting();
            var config = new AndroidPlatformConfig(buildSetting);
            SetPrivateField(config, "_androidSetting", androidSetting);

            try
            {
                config.ConfigurePlatformSettings(buildSetting);

                Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("2.3.4"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(androidSetting);
            }
        }

        [Test]
        public void iOSConfigurePlatformSettings_ShouldApplyAppVersionToBundleVersion()
        {
            var buildSetting = new BuildSetting
            {
                builderTarget = BuilderTarget.iOS,
                appVersion = "3.4.5",
                versionCode = 67,
                isRelease = false
            };
            var iosSetting = CreateValidIosSetting();
            var config = new iOSPlatformConfig(buildSetting);
            SetPrivateField(config, "_iOSSetting", iosSetting);

            try
            {
                config.ConfigurePlatformSettings(buildSetting);

                Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("3.4.5"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(iosSetting);
            }
        }

        private static iOSSetting CreateValidIosSetting()
        {
            var setting = ScriptableObject.CreateInstance<iOSSetting>();
            SetPrivateField(setting, "bundleIdentifier", "com.company.game");
            SetPrivateField(setting, "targetOSVersion", "12.0");
            SetPrivateField(setting, "appleDevelopTeamId", "TEAM123456");
            SetPrivateField(setting, "developmentMobileProvisionUUid", "development-profile-uuid");
            SetPrivateField(setting, "developmentMobileProvisionProfileName", "Development Profile");
            SetPrivateField(setting, "developmentCodeSignIdentity", "Apple Development");
            return setting;
        }

        private static AndroidSetting CreateValidAndroidSetting()
        {
            var setting = ScriptableObject.CreateInstance<AndroidSetting>();
            SetPrivateField(setting, "bundleIdentifier", "com.company.game");
            SetPrivateField(setting, "useCustomKeystore", false);
            return setting;
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
