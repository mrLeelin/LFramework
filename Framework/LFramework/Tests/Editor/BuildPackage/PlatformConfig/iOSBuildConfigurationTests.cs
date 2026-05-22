using System;
using System.Reflection;
using LFramework.Editor;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.iOS;
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
        public void Validate_ShouldFail_WhenSigningFieldsUseTodoPlaceholders()
        {
            var setting = CreateValidSetting();
            SetPrivateField(setting, "mobileProvisionUUid", "TODO_PROFILE_UUID");
            SetPrivateField(setting, "appleDevelopTeamId", "TODO_TEAM_ID");

            try
            {
                bool isValid = setting.Validate(out string errorMessage);

                Assert.That(isValid, Is.False);
                Assert.That(errorMessage, Does.Contain("Mobile Provision UUID"));
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

            try
            {
                var config = iOSBuildConfig.CreateFromBuildSetting(CreateBuildSetting(), setting, "Builds/IOS/Project");
                
                Assert.That(config.AppleDevelopTeamId, Is.EqualTo("TEAM123456"));
                Assert.That(config.CodeSignIdentity, Is.EqualTo("Apple Distribution"));
                Assert.That(config.MobileProvisionUuid, Is.EqualTo("profile-uuid"));
                Assert.That(config.URLScheme, Is.EqualTo("demoapp"));
                Assert.That(config.BundleURLName, Is.EqualTo("com.company.game"));
                Assert.That(config.CustomAppControllerName, Is.EqualTo("CustomAppController"));
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
            SetPrivateField(setting, "mobileProvisionUUid", "profile-uuid");
            SetPrivateField(setting, "appleDevelopTeamId", "TEAM123456");
            SetPrivateField(setting, "codeSignIdentity", "Apple Distribution");
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
