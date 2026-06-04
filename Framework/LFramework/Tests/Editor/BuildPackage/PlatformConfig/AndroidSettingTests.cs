using System;
using System.IO;
using System.Reflection;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage.PlatformConfig
{
    public class AndroidSettingTests
    {
        [Test]
        public void GetKeystoreConfig_ShouldReturnBuildModeSpecificValues()
        {
            var setting = CreateSetting();

            try
            {
                AndroidKeystoreConfig debugConfig = setting.GetKeystoreConfig(false);
                AndroidKeystoreConfig releaseConfig = setting.GetKeystoreConfig(true);

                Assert.That(debugConfig.BuildMode, Is.EqualTo("Debug"));
                Assert.That(debugConfig.KeystorePath, Is.EqualTo("debug.keystore"));
                Assert.That(debugConfig.KeystorePass, Is.EqualTo("debug-pass"));
                Assert.That(debugConfig.KeyaliasName, Is.EqualTo("debug-alias"));
                Assert.That(debugConfig.KeyaliasPass, Is.EqualTo("debug-alias-pass"));

                Assert.That(releaseConfig.BuildMode, Is.EqualTo("Release"));
                Assert.That(releaseConfig.KeystorePath, Is.EqualTo("release.keystore"));
                Assert.That(releaseConfig.KeystorePass, Is.EqualTo("release-pass"));
                Assert.That(releaseConfig.KeyaliasName, Is.EqualTo("release-alias"));
                Assert.That(releaseConfig.KeyaliasPass, Is.EqualTo("release-alias-pass"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void ValidateForBuild_ShouldOnlyRequireCurrentBuildKeystore()
        {
            string tempRoot = CreateTempDirectory();
            string debugKeystorePath = Path.Combine(tempRoot, "debug.keystore");
            File.WriteAllText(debugKeystorePath, string.Empty);
            var setting = CreateSetting();
            SetPrivateField(setting, "debugKeystorePath", debugKeystorePath);
            SetPrivateField(setting, "keystorePath", Path.Combine(tempRoot, "missing-release.keystore"));

            try
            {
                Assert.That(setting.ValidateForBuild(false, out string debugError), Is.True);
                Assert.That(debugError, Is.Null);

                Assert.That(setting.ValidateForBuild(true, out string releaseError), Is.False);
                Assert.That(releaseError, Does.Contain("Release Keystore"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
                Directory.Delete(tempRoot, true);
            }
        }

        [Test]
        public void ValidateForBuild_ShouldSkipKeystore_WhenBuildDoesNotRequireUnitySigning()
        {
            var setting = CreateSetting();
            SetPrivateField(setting, "debugKeystorePath", "missing-debug.keystore");
            SetPrivateField(setting, "keystorePath", "missing-release.keystore");

            try
            {
                Assert.That(setting.ValidateForBuild(false, false, out string debugError), Is.True);
                Assert.That(debugError, Is.Null);

                Assert.That(setting.ValidateForBuild(true, false, out string releaseError), Is.True);
                Assert.That(releaseError, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        private static AndroidSetting CreateSetting()
        {
            var setting = ScriptableObject.CreateInstance<AndroidSetting>();
            SetPrivateField(setting, "bundleIdentifier", "com.company.game");
            SetPrivateField(setting, "debugKeystorePath", "debug.keystore");
            SetPrivateField(setting, "debugKeystorePass", "debug-pass");
            SetPrivateField(setting, "debugKeyaliasName", "debug-alias");
            SetPrivateField(setting, "debugKeyaliasPass", "debug-alias-pass");
            SetPrivateField(setting, "keystorePath", "release.keystore");
            SetPrivateField(setting, "keystorePass", "release-pass");
            SetPrivateField(setting, "keyaliasName", "release-alias");
            SetPrivateField(setting, "keyaliasPass", "release-alias-pass");
            return setting;
        }

        private static string CreateTempDirectory()
        {
            string path = Path.Combine(Path.GetTempPath(), "lframework-android-setting-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
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
