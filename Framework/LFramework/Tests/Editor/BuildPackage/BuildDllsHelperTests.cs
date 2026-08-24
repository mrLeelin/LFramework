using System;
using System.IO;
using System.Reflection;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.HybridCLR;
using NUnit.Framework;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class BuildDllsHelperTests
    {
        [TestCase(BuildType.App, true)]
        [TestCase(BuildType.ResourcesUpdate, true)]
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

        [Test]
        public void GetMetaVersionDirectory_UsesHybridClr811Layout()
        {
            string result = HybridCLRBuildWorkflow.GetMetaVersionDirectory(
                Path.Combine("Snapshots", "Android", "AotSnapshot"));

            Assert.That(result, Is.EqualTo(
                Path.Combine("Snapshots", "Android", "AotSnapshot", "MetaVersions")));
        }

        [TestCase("8.11.1", true)]
        [TestCase(" 8.11.1 ", true)]
        [TestCase("8.3.0", false)]
        [TestCase("8.11.0", false)]
        [TestCase(null, false)]
        public void IsRequiredVersion_AcceptsOnlyFlagship8111(string version, bool expected)
        {
            Assert.That(HybridCLRBuildWorkflow.IsRequiredVersion(version), Is.EqualTo(expected));
        }

        [Test]
        public void TryUpdateDifferentialHybridAssemblies_UpdatesCompatibleSettingsField()
        {
            var settings = new CompatibleHybridClrSettings
            {
                differentialHybridAssemblies = new[] { "Old.Hotfix" }
            };

            bool supported = HybridCLRBuildWorkflow.TryUpdateDifferentialHybridAssemblies(
                settings,
                new[] { "Foundation.Hotfix", "Game.Hotfix" },
                out bool changed);

            Assert.That(supported, Is.True);
            Assert.That(changed, Is.True);
            Assert.That(settings.differentialHybridAssemblies,
                Is.EqualTo(new[] { "Foundation.Hotfix", "Game.Hotfix" }));

            supported = HybridCLRBuildWorkflow.TryUpdateDifferentialHybridAssemblies(
                settings,
                new[] { "Foundation.Hotfix", "Game.Hotfix" },
                out changed);

            Assert.That(supported, Is.True);
            Assert.That(changed, Is.False);
        }

        [Test]
        public void PrepareDheOutputDirectories_PreservesExpectedMetaAndRemovesStaleArtifacts()
        {
            string outputRoot = Path.Combine(
                Path.GetTempPath(),
                $"LFrameworkDheArtifacts_{Guid.NewGuid():N}");
            string codeDirectory = Path.Combine(outputRoot, "DHE", "Code");
            Directory.CreateDirectory(codeDirectory);
            string expectedArtifact = Path.Combine(codeDirectory, "Game.Hotfix.dll.bytes");
            string expectedMeta = expectedArtifact + ".meta";
            string staleArtifact = Path.Combine(codeDirectory, "Removed.Hotfix.dll.bytes");
            string staleMeta = staleArtifact + ".meta";
            File.WriteAllBytes(expectedArtifact, new byte[] { 7 });
            File.WriteAllText(expectedMeta, "expected-guid");
            File.WriteAllBytes(staleArtifact, new byte[] { 9 });
            File.WriteAllText(staleMeta, "stale-guid");

            try
            {
                Type helperType = Type.GetType("LFramework.Editor.BuildDllsHelper, LFramework.Editor");
                MethodInfo method = helperType?.GetMethod(
                    "PrepareDheOutputDirectories",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);

                object[] arguments =
                {
                    outputRoot,
                    new[] { "Game.Hotfix" },
                    null,
                    null,
                    null
                };
                method.Invoke(null, arguments);

                Assert.That(File.ReadAllText(expectedMeta), Is.EqualTo("expected-guid"));
                Assert.That(File.Exists(staleArtifact), Is.False);
                Assert.That(File.Exists(staleMeta), Is.False);
            }
            finally
            {
                if (Directory.Exists(outputRoot))
                {
                    Directory.Delete(outputRoot, true);
                }
            }
        }

        private sealed class CompatibleHybridClrSettings
        {
            public string[] differentialHybridAssemblies;
        }
    }
}
