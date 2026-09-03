using System.Collections.Generic;
using LFramework.Editor.Builder.HybridCLR;
using NUnit.Framework;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class HybridClrEditionDefineSynchronizerTests
    {
        [Test]
        public void HasDheSources_RecognizesThePaidEditorSource()
        {
            bool result = HybridClrEditionDefineSynchronizer.HasDheSources(
                "HybridCLR.Editor",
                new[] { "Packages/com.focus-creative-games.hybridclr/Editor/DHE/MetaVersionWorkflow.cs" });

            Assert.That(result, Is.True);
        }

        [TestCase("HybridCLR.Runtime", "Packages/com.focus-creative-games.hybridclr/Editor/DHE/MetaVersionWorkflow.cs")]
        [TestCase("HybridCLR.Editor", "Packages/com.focus-creative-games.hybridclr/Editor/MetaVersionWorkflow.cs")]
        [TestCase("HybridCLR.Editor", "Packages/com.other/Editor/DHE/MetaVersionWorkflow.cs")]
        public void HasDheSources_RejectsNearMatches(string assemblyName, string sourceFile)
        {
            bool result = HybridClrEditionDefineSynchronizer.HasDheSources(
                assemblyName,
                new[] { sourceFile });

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasDheSources_HandlesMissingInputs()
        {
            Assert.That(
                HybridClrEditionDefineSynchronizer.HasDheSources(null, null),
                Is.False);
            Assert.That(
                HybridClrEditionDefineSynchronizer.HasDheSources(
                    "HybridCLR.Editor",
                    new List<string>()),
                Is.False);
        }

        [Test]
        public void HasRelevantChanges_IncludesDeletedAndMovedFiles()
        {
            Assert.That(
                HybridClrEditionDefineSynchronizer.HasRelevantChanges(
                    null,
                    new[] { "Packages/com.focus-creative-games.hybridclr/Editor/DHE/MetaVersionWorkflow.cs" },
                    null,
                    null),
                Is.True);
            Assert.That(
                HybridClrEditionDefineSynchronizer.HasRelevantChanges(
                    new[] { "Assets/Scenes/Main.unity" },
                    null,
                    null,
                    null),
                Is.False);
        }

        [TestCase(null, true, "LFRAMEWORK_HYBRIDCLR_DHE")]
        [TestCase("UNITY_2022_1_OR_NEWER;LFRAMEWORK_HYBRIDCLR_DHE", true,
            "UNITY_2022_1_OR_NEWER;LFRAMEWORK_HYBRIDCLR_DHE")]
        [TestCase("UNITY_2022_1_OR_NEWER;LFRAMEWORK_HYBRIDCLR_DHE", false,
            "UNITY_2022_1_OR_NEWER")]
        [TestCase("UNITY_2022_1_OR_NEWER", false, "UNITY_2022_1_OR_NEWER")]
        [TestCase(";UNITY_2022_1_OR_NEWER;;", false, "UNITY_2022_1_OR_NEWER")]
        public void UpdateDheDefine_IsIdempotentAndPreservesOtherDefines(
            string currentDefines,
            bool enabled,
            string expectedDefines)
        {
            string result = HybridClrEditionDefineSynchronizer.UpdateDheDefine(
                currentDefines,
                enabled);

            Assert.That(result, Is.EqualTo(expectedDefines));
        }

        [TestCase(null, true, true, "HybridCLR_SUPPORT;LFRAMEWORK_HYBRIDCLR_DHE")]
        [TestCase(null, false, true, "HybridCLR_SUPPORT;LFRAMEWORK_HYBRIDCLR_DHE")]
        [TestCase("UNITY_2022_1_OR_NEWER", true, false,
            "UNITY_2022_1_OR_NEWER;HybridCLR_SUPPORT")]
        [TestCase("HybridCLR_SUPPORT;LFRAMEWORK_HYBRIDCLR_DHE", false, false,
            "")]
        [TestCase("UNITY_2022_1_OR_NEWER;HybridCLR_SUPPORT", true, true,
            "UNITY_2022_1_OR_NEWER;HybridCLR_SUPPORT;LFRAMEWORK_HYBRIDCLR_DHE")]
        public void UpdateHybridClrDefines_UpdatesSupportAndDheSymbols(
            string currentDefines,
            bool hybridClrEnabled,
            bool dheEnabled,
            string expectedDefines)
        {
            string result = HybridClrEditionDefineSynchronizer.UpdateHybridClrDefines(
                currentDefines,
                hybridClrEnabled,
                dheEnabled);

            Assert.That(result, Is.EqualTo(expectedDefines));
        }
    }
}
