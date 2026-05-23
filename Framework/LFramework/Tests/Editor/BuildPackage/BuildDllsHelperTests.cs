using System;
using System.Reflection;
using LFramework.Editor.Builder;
using NUnit.Framework;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class BuildDllsHelperTests
    {
        [TestCase(BuildType.App, true)]
        [TestCase(BuildType.ResourcesUpdate, false)]
        public void ShouldCopyAotDlls_MatchesBuildType(BuildType buildType, bool expected)
        {
            Type helperType = Type.GetType("LFramework.Editor.BuildDllsHelper, LFramework.Editor");
            Assert.That(helperType, Is.Not.Null, "BuildDllsHelper type is missing.");

            MethodInfo method = helperType.GetMethod(
                "ShouldCopyAotDlls",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected BuildDllsHelper.ShouldCopyAotDlls to define AOT copy policy.");

            object result = method.Invoke(null, new object[] { buildType });

            Assert.That(result, Is.EqualTo(expected));
        }
    }
}
