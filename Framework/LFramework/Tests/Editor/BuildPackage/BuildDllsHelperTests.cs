using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HybridCLR.Editor.Settings;
using HybridCLR.Editor.Installer;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.BuildingResource;
using LFramework.Editor.Builder.HybridCLR;
using LFramework.Editor.Builder.Pipeline.Pipelines;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class BuildDllsHelperTests
    {
        [Test]
        public void YooAssetDllRegistrar_JoinsTagsWithYooAssetDelimiter()
        {
            Type registrarType = Type.GetType(
                "LFramework.Editor.Builder.BuildingResource.YooAssetDllRegistrar, LFramework.Editor");
            Assert.That(registrarType, Is.Not.Null);

            MethodInfo method = registrarType.GetMethod(
                "JoinAssetTags",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "Expected YooAssetDllRegistrar.JoinAssetTags to own YooAsset tag serialization.");

            string result = (string)method.Invoke(null, new object[]
            {
                new[] { "init_assets", "code_dll", "init_assets", " " }
            });

            Assert.That(result, Is.EqualTo("init_assets;code_dll"));
        }

        [Test]
        public void ResourceBuildPipeline_PreparesEnvironmentBeforeBuildingDlls()
        {
            var taskNames = new ResourceBuildPipeline().GetTasks()
                .Select(task => task.GetType().Name)
                .ToArray();

            Assert.That(taskNames, Is.EqualTo(new[]
            {
                "PrepareBuildEnvironmentTask",
                "BuildDllTask",
                "BuildResourcesTask"
            }));
        }

        [Test]
        public void BuildResourcesService_RejectsMissingBuildSettings()
        {
            Assert.Throws<ArgumentNullException>(() => BuildResourcesService.Build(null));
        }

        [Test]
        public void BuildEnvironmentConfigurator_RejectsMissingProjectHybridClrSetting()
        {
            var exception = Assert.Throws<InvalidOperationException>(() =>
                BuildEnvironmentConfigurator.ValidateRuntimeSettingForBuild(null));

            Assert.That(exception.Message, Does.Contain("ProjectSettingSelector"));
        }

        [Test]
        public void AddressableDllRegistrar_ReusesExistingGroupIdentity()
        {
            Type settingsType = Type.GetType(
                "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings, Unity.Addressables.Editor");
            Assert.That(settingsType, Is.Not.Null);

            MethodInfo createSettings = settingsType.GetMethod(
                "Create",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new[] { typeof(string), typeof(string), typeof(bool), typeof(bool) },
                null);
            Assert.That(createSettings, Is.Not.Null);

            object settings = createSettings.Invoke(null, new object[]
            {
                "Assets",
                $"LFrameworkAddressableRegistrarTests_{Guid.NewGuid():N}",
                false,
                false
            });
            try
            {
                Type registrarType = Type.GetType(
                    "LFramework.Editor.Builder.BuildingResource.AddressableDllRegistrar, LFramework.Editor");
                Assert.That(registrarType, Is.Not.Null);

                ConstructorInfo constructor = registrarType.GetConstructor(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { settingsType },
                    null);
                Assert.That(constructor, Is.Not.Null,
                    "Expected an injectable AddressableDllRegistrar constructor for deterministic group updates.");

                object registrar = constructor.Invoke(new object[] { settings });
                MethodInfo ensureGroup = registrarType.GetMethod(
                    "EnsureGroupExists",
                    BindingFlags.Instance | BindingFlags.Public);
                Assert.That(ensureGroup, Is.Not.Null);

                Assert.That(ensureGroup.Invoke(registrar, new object[] { "Update_Code" }), Is.True);
                MethodInfo findGroup = settingsType.GetMethod(
                    "FindGroup",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    new[] { typeof(string) },
                    null);
                Assert.That(findGroup, Is.Not.Null);

                object firstGroup = findGroup.Invoke(settings, new object[] { "Update_Code" });
                Assert.That(firstGroup, Is.Not.Null);
                string firstGuid = (string)firstGroup.GetType().GetProperty("Guid")?.GetValue(firstGroup);
                Assert.That(firstGuid, Is.Not.Empty);

                Assert.That(ensureGroup.Invoke(registrar, new object[] { "Update_Code" }), Is.True);
                object secondGroup = findGroup.Invoke(settings, new object[] { "Update_Code" });
                string secondGuid = (string)secondGroup?.GetType().GetProperty("Guid")?.GetValue(secondGroup);

                Assert.That(secondGroup, Is.SameAs(firstGroup));
                Assert.That(secondGuid, Is.EqualTo(firstGuid));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate((UnityEngine.Object)settings);
            }
        }

        [Test]
        public void AddressableBuildSystem_RejectsReportedBuildErrors()
        {
            Type buildSystemType = Type.GetType(
                "LFramework.Editor.Builder.BuildingResource.AddressableBuildSystem, LFramework.Editor");
            Assert.That(buildSystemType, Is.Not.Null);

            MethodInfo method = buildSystemType.GetMethod(
                "ThrowIfBuildFailed",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "Expected AddressableBuildSystem.ThrowIfBuildFailed to centralize error propagation.");

            var exception = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[] { "catalog failed" }));
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(exception.InnerException.Message, Does.Contain("catalog failed"));

            Assert.DoesNotThrow(() => method.Invoke(null, new object[] { string.Empty }));
        }

#if LFRAMEWORK_HYBRIDCLR_DHE
        [Test]
        public void ValidateDhePatchSet_RejectsPartialAssemblySets()
        {
            Type hotfixType = Type.GetType(
                "LFramework.Runtime.HotfixComponent, LFramework.Runtime");
            Assert.That(hotfixType, Is.Not.Null);

            MethodInfo method = hotfixType.GetMethod(
                "ValidateDhePatchSet",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "Expected HotfixComponent.ValidateDhePatchSet to enforce atomic DHE patches.");

            var configured = new[] { "Foundation.Hotfix", "Game.Hotfix" };
            var noPatch = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var partialPatch = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["Game.Hotfix"] = new byte[] { 1, 2 }
            };
            var completePatch = new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["Foundation.Hotfix"] = new byte[] { 1, 2 },
                ["Game.Hotfix"] = new byte[] { 3, 4 }
            };

            Assert.That(method.Invoke(null, new object[] { configured, noPatch }), Is.Null);
            Assert.That(
                method.Invoke(null, new object[] { configured, partialPatch }),
                Does.Contain("Foundation.Hotfix"));
            Assert.That(method.Invoke(null, new object[] { configured, completePatch }), Is.Null);
        }
#endif

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

        [TestCase(true, "8.11.1", "8.11.1", true, true)]
        [TestCase(false, "8.11.1", "8.11.1", true, false)]
        [TestCase(true, "8.11.0", "8.11.1", true, false)]
        [TestCase(true, "8.11.1", "8.11.0", true, false)]
        [TestCase(true, "8.11.1", "8.11.1", false, false)]
        public void IsDheEnvironmentAvailable_RequiresAllRuntimeAndEditorPrerequisites(
            bool hasRequiredApis,
            string packageVersion,
            string libil2cppVersion,
            bool hasDheNativeFiles,
            bool expected)
        {
            Assert.That(
                HybridCLRBuildWorkflow.IsDheEnvironmentAvailable(
                    hasRequiredApis,
                    packageVersion,
                    libil2cppVersion,
                    hasDheNativeFiles),
                Is.EqualTo(expected));
        }

        [TestCase("6000", 6000, false, true)]
        [TestCase("6000.3.x", 6000, false, true)]
        [TestCase("6000.2.x", 6000, false, false)]
        [TestCase("2022", 2022, false, true)]
        [TestCase("2022", 6000, false, false)]
        public void InstallerManifestVersionMatchesUnityMajorLine(
            string manifestVersion,
            int unityMajor,
            bool isTuanjie,
            bool expected)
        {
            MethodInfo method = typeof(InstallerController).GetMethod(
                "IsManifestVersionCompatible",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            Assert.That(
                method.Invoke(null, new object[] { manifestVersion, unityMajor, isTuanjie }),
                Is.EqualTo(expected));
        }

        [Test]
        public void GenerateStripedAOTDllsFailureMessageContainsBuildReportErrors()
        {
            MethodInfo method = typeof(HybridCLR.Editor.Commands.StripAOTDllCommand).GetMethod(
                "FormatBuildFailureMessage",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "Build failures must retain the detailed BuildReport errors.");

            string result = (string)method.Invoke(null, new object[]
            {
                "Android",
                "Failed",
                2,
                3,
                "/tmp/StrippedAOTDlls",
                new[] { "il2cpp exited with code 1", "missing libil2cpp" },
            });

            Assert.That(result, Does.Contain("target=Android"));
            Assert.That(result, Does.Contain("errors=2"));
            Assert.That(result, Does.Contain("warnings=3"));
            Assert.That(result, Does.Contain("/tmp/StrippedAOTDlls"));
            Assert.That(result, Does.Contain("il2cpp exited with code 1"));
            Assert.That(result, Does.Contain("missing libil2cpp"));
        }

#if LFRAMEWORK_HYBRIDCLR_DHE
        [Test]
        public void TryUpdateDifferentialHybridAssemblies_UpdatesCompatibleSettingsField()
        {
            var settings = ScriptableObject.CreateInstance<HybridCLRSettings>();
            try
            {
                settings.differentialHybridAssemblies = new[] { "Old.Hotfix" };

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
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
#endif

#if LFRAMEWORK_HYBRIDCLR_DHE
        [Test]
        public void HasArtifactContent_RejectsPlaceholdersAndAcceptsGeneratedData()
        {
            string directory = Path.Combine(
                Path.GetTempPath(),
                $"LFrameworkHybridClrArtifact_{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            string placeholder = Path.Combine(directory, "placeholder.bytes");
            string generated = Path.Combine(directory, "generated.bytes");
            File.WriteAllBytes(placeholder, new byte[] { 1 });
            File.WriteAllBytes(generated, new byte[] { 1, 2 });

            try
            {
                Type helperType = Type.GetType("LFramework.Editor.BuildDllsHelper, LFramework.Editor");
                MethodInfo method = helperType?.GetMethod(
                    "HasArtifactContent",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);

                Assert.That(method.Invoke(null, new object[] { placeholder }), Is.False);
                Assert.That(method.Invoke(null, new object[] { generated }), Is.True);
                Assert.That(method.Invoke(null, new object[] { Path.Combine(directory, "missing.bytes") }),
                    Is.False);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
#endif

#if LFRAMEWORK_HYBRIDCLR_DHE
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
#endif

#if LFRAMEWORK_HYBRIDCLR_DHE
        [Test]
        public void ClearDifferentialHybridAssemblies_RemovesStaleDheConfiguration()
        {
            var settings = ScriptableObject.CreateInstance<HybridCLRSettings>();
            try
            {
                settings.differentialHybridAssemblies = new[] { "Old.Hotfix" };

                bool changed = BuildEnvironmentConfigurator.ClearDifferentialHybridAssemblies(settings);

                Assert.That(changed, Is.True);
                Assert.That(settings.differentialHybridAssemblies, Is.Empty);
                Assert.That(
                    BuildEnvironmentConfigurator.ClearDifferentialHybridAssemblies(settings),
                    Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(settings);
            }
        }
#endif
    }
}
