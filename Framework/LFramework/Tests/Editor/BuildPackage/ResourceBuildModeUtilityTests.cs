using System;
using System.Reflection;
using LFramework.Editor;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class ResourceBuildModeUtilityTests
    {
        [Test]
        public void IsLocalResourceBuild_ReturnsTrue_WhenCdnTypeIsLocal()
        {
            var buildSetting = new BuildSetting
            {
                isResourcesBuildIn = false,
                cdnType = CdnType.Local
            };

            Assert.That(InvokeIsLocalResourceBuild(buildSetting), Is.True);
        }

        [Test]
        public void IsLocalResourceBuild_ReturnsTrue_WhenResourcesAreBuiltIn()
        {
            var buildSetting = new BuildSetting
            {
                isResourcesBuildIn = true,
                cdnType = CdnType.Debug
            };

            Assert.That(InvokeIsLocalResourceBuild(buildSetting), Is.True);
        }

        [Test]
        public void IsLocalResourceBuild_ReturnsFalse_ForRemoteResourceBuild()
        {
            var buildSetting = new BuildSetting
            {
                isResourcesBuildIn = false,
                cdnType = CdnType.Debug
            };

            Assert.That(InvokeIsLocalResourceBuild(buildSetting), Is.False);
        }

        private static bool InvokeIsLocalResourceBuild(BuildSetting buildSetting)
        {
            Type helperType = Type.GetType(
                "LFramework.Editor.Builder.BuildingResource.ResourceBuildModeUtility, LFramework.Editor");

            Assert.That(helperType, Is.Not.Null, "Expected ResourceBuildModeUtility to exist.");

            MethodInfo method = helperType.GetMethod(
                "IsLocalResourceBuild",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected ResourceBuildModeUtility.IsLocalResourceBuild to exist.");
            return (bool)method.Invoke(null, new object[] { buildSetting });
        }
    }
}
