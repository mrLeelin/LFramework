using NUnit.Framework;
using LFramework.Runtime;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class SettingValidationServiceTests
    {
        private const string TempRoot = "Assets/__TempSettingValidationTests";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }

            AssetDatabase.CreateFolder("Assets", "__TempSettingValidationTests");
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void Validate_ReturnsError_WhenProjectSelectorMissing()
        {
            SettingValidationReport report = SettingValidationService.Validate(null, null);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues, Has.Count.EqualTo(2));
            Assert.That(report.Issues[0].severity, Is.EqualTo(SettingValidationSeverity.Error));
        }

        [Test]
        public void MissingProjectSelectorGuidance_IncludesGenerationPath()
        {
            string message = LSystemApplicationBehaviour.GetMissingProjectSelectorGuidanceMessage();

            Assert.That(message, Does.Contain("ProjectSettingSelector"));
            Assert.That(message, Does.Contain("LFramework/GameSetting"));
            Assert.That(message, Does.Contain("Framework Setting"));
            Assert.That(message, Does.Contain("初始化 Project Settings"));
        }

        [Test]
        public void Validate_DetectsDuplicateSettingIds()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var first = ScriptableObject.CreateInstance<TestBaseSettingA>();
            var second = ScriptableObject.CreateInstance<TestBaseSettingB>();
            first.EditorSetSettingId("duplicate.id");
            second.EditorSetSettingId("duplicate.id");
            selector.SetSetting(first);
            selector.SetSetting(second);

            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();

            SettingValidationReport report = SettingValidationService.Validate(selector, syncState);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Exists(issue => issue.code == SettingValidationCode.DuplicateSettingId), Is.True);
        }

        [Test]
        public void Validate_DetectsInvalidComponentBindType()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var component = ScriptableObject.CreateInstance<TestComponentSetting>();
            component.EditorSetSettingId("component.invalid");
            component.bindTypeName = "Definitely.Not.A.Real.Component";
            selector.SetComponentSetting(component);

            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();

            SettingValidationReport report = SettingValidationService.Validate(selector, syncState);

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Exists(issue => issue.code == SettingValidationCode.InvalidBindType), Is.True);
        }

        [Test]
        public void Validate_DetectsMissingRequiredSettingFromTemplates()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var requiredTemplate = ScriptableObject.CreateInstance<TestBaseSettingA>();
            requiredTemplate.EditorSetSettingId("required.setting");
            AssetDatabase.CreateAsset(requiredTemplate, $"{TempRoot}/RequiredTemplate.asset");

            SettingValidationReport report = SettingValidationService.Validate(
                selector,
                syncState,
                new ScriptableObject[] { requiredTemplate });

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Exists(issue => issue.code == SettingValidationCode.MissingRequiredSetting), Is.True);
        }

        [Test]
        public void Validate_DetectsDirectPackageTemplateReference()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var templateAsset = ScriptableObject.CreateInstance<TestBaseSettingA>();
            templateAsset.EditorSetSettingId("template.setting");
            AssetDatabase.CreateAsset(templateAsset, $"{TempRoot}/Template.asset");
            selector.SetSetting(templateAsset);

            SettingValidationReport report = SettingValidationService.Validate(
                selector,
                syncState,
                new ScriptableObject[] { templateAsset });

            Assert.That(report.HasErrors, Is.True);
            Assert.That(report.Issues.Exists(issue => issue.code == SettingValidationCode.DirectTemplateReference), Is.True);
        }

        [Test]
        public void ResolveComponentSettings_UsesProjectSelectorOnly()
        {
            var projectSelector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var projectComponent = ScriptableObject.CreateInstance<TestComponentSetting>();
            projectComponent.EditorSetSettingId("component.project");
            projectComponent.bindTypeName = "Project.Component";
            projectSelector.SetComponentSetting(projectComponent);

            var resolved = LSystemApplicationBehaviour.ResolveComponentSettingsForRegistration(
                projectSelector);

            Assert.That(resolved, Has.Count.EqualTo(1));
            Assert.That(resolved[0], Is.SameAs(projectComponent));
        }

        [Test]
        public void OpenProjectSettings_SelectsProjectSelectorAsset()
        {
            ProjectSettingSelector selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            AssetDatabase.CreateAsset(selector, $"{TempRoot}/ProjectSettingSelector.asset");
            try
            {
                Selection.activeObject = null;

                SettingMenuCommands.OpenProjectSettings(selector);

                Assert.That(Selection.activeObject, Is.SameAs(selector));
            }
            finally
            {
                AssetDatabase.DeleteAsset($"{TempRoot}/ProjectSettingSelector.asset");
            }
        }

        private sealed class TestBaseSettingA : BaseSetting
        {
        }

        private sealed class TestBaseSettingB : BaseSetting
        {
        }

        private sealed class TestComponentSetting : ComponentSetting
        {
        }
    }
}
