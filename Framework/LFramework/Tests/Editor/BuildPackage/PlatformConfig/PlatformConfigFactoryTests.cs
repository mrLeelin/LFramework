using System;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.PlatformConfig;
using NUnit.Framework;

namespace LFramework.Editor.Tests.BuildPackage.PlatformConfig
{
    public class PlatformConfigFactoryTests
    {
        [SetUp]
        public void SetUp()
        {
            TestPlatformConfigRegistryProviders.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            TestPlatformConfigRegistryProviders.Reset();
        }

        [TestCase(BuilderTarget.Windows, typeof(WindowsPlatformConfig))]
        [TestCase(BuilderTarget.Android, typeof(AndroidPlatformConfig))]
        [TestCase(BuilderTarget.iOS, typeof(iOSPlatformConfig))]
        public void DefaultProvider_ShouldReturnFrameworkPlatformConfig(BuilderTarget builderTarget, Type expectedType)
        {
            var provider = new DefaultPlatformConfigRegistryProvider();

            IPlatformConfig config = provider.CreateConfig(builderTarget, CreateBuildSetting(builderTarget));

            Assert.That(config, Is.TypeOf(expectedType));
        }

        [Test]
        public void CreateConfig_ShouldFallbackToDefaultProvider_WhenNoCustomProviderIsActive()
        {
            IPlatformConfig config = PlatformConfigFactory.CreateConfig(
                BuilderTarget.Android,
                CreateBuildSetting(BuilderTarget.Android));

            Assert.That(config, Is.TypeOf<AndroidPlatformConfig>());
        }

        [Test]
        public void CreateConfig_ShouldUseHighestPriorityProvider()
        {
            TestPlatformConfigRegistryProviders.HighPriorityEnabled = true;
            TestPlatformConfigRegistryProviders.LowPriorityEnabled = true;

            IPlatformConfig config = PlatformConfigFactory.CreateConfig(
                BuilderTarget.Android,
                CreateBuildSetting(BuilderTarget.Android));

            Assert.That(config, Is.TypeOf<HighPriorityAndroidPlatformConfig>());
        }

        [Test]
        public void CreateConfig_ShouldThrow_WhenMultipleProvidersHaveSameHighestPriority()
        {
            TestPlatformConfigRegistryProviders.ConflictProviderAEnabled = true;
            TestPlatformConfigRegistryProviders.ConflictProviderBEnabled = true;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                PlatformConfigFactory.CreateConfig(BuilderTarget.Android, CreateBuildSetting(BuilderTarget.Android)));

            Assert.That(exception.Message, Does.Contain("ConflictProviderA"));
            Assert.That(exception.Message, Does.Contain("ConflictProviderB"));
        }

        [Test]
        public void CreateConfig_ShouldThrow_WhenSelectedProviderReturnsNull()
        {
            TestPlatformConfigRegistryProviders.NullReturningProviderEnabled = true;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                PlatformConfigFactory.CreateConfig(BuilderTarget.Android, CreateBuildSetting(BuilderTarget.Android)));

            Assert.That(exception.Message, Does.Contain("NullReturningProvider"));
        }

        [Test]
        public void CreateConfig_ShouldThrow_WhenSelectedProviderDoesNotSupportTarget()
        {
            TestPlatformConfigRegistryProviders.HighPriorityEnabled = true;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
                PlatformConfigFactory.CreateConfig(BuilderTarget.Windows, CreateBuildSetting(BuilderTarget.Windows)));

            Assert.That(exception.Message, Does.Contain("HighPriorityProvider"));
            Assert.That(exception.Message, Does.Contain(nameof(BuilderTarget.Windows)));
        }

        private static BuildSetting CreateBuildSetting(BuilderTarget builderTarget)
        {
            return new BuildSetting
            {
                builderTarget = builderTarget,
                isRelease = false,
                appVersion = "1.0.0",
                versionCode = 1
            };
        }
    }
}
