using System.IO;
using NUnit.Framework;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class ProjectOwnedSettingsTests
    {
        private const string TempRoot = "Assets/__TempProjectOwnedSettingsTests";

        [SetUp]
        public void SetUp()
        {
            DeleteTempAssets();
            AssetDatabase.Refresh();
            SettingManager.ClearCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            SettingManager.ClearCacheForTests();
            DeleteTempAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void BaseSetting_SettingId_UsesExplicitValueOrFallsBackToTypeFullName()
        {
            var setting = ScriptableObject.CreateInstance<TestBaseSetting>();

            Assert.That(setting.SettingId, Is.EqualTo(typeof(TestBaseSetting).FullName));

            setting.EditorSetSettingId("custom.base.setting");

            Assert.That(setting.SettingId, Is.EqualTo("custom.base.setting"));
        }

        [Test]
        public void ComponentSetting_SettingId_UsesExplicitValueOrFallsBackToTypeFullName()
        {
            var setting = ScriptableObject.CreateInstance<TestComponentSetting>();

            Assert.That(setting.SettingId, Is.EqualTo(typeof(TestComponentSetting).FullName));

            setting.EditorSetSettingId("custom.component.setting");

            Assert.That(setting.SettingId, Is.EqualTo("custom.component.setting"));
        }

        [Test]
        public void ProjectSettingSelector_CanStoreAndResolveBaseAndComponentSettings()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var gameSetting = ScriptableObject.CreateInstance<GameSetting>();
            var componentSetting = ScriptableObject.CreateInstance<TestComponentSetting>();
            componentSetting.bindTypeName = "Test.Component";

            selector.SetSetting(gameSetting);
            selector.SetComponentSetting(componentSetting);

            Assert.That(selector.GetSetting<GameSetting>(), Is.SameAs(gameSetting));
            Assert.That(selector.GetAllSettings(), Has.Count.EqualTo(1));
            Assert.That(selector.GetComponentSetting<TestComponentSetting>(), Is.SameAs(componentSetting));
            Assert.That(selector.GetComponentSettingByBindTypeName("Test.Component"), Is.SameAs(componentSetting));
            Assert.That(selector.GetAllComponentSettings(), Has.Count.EqualTo(1));
        }

        [Test]
        public void ProjectSettingSelector_SetSetting_ReplacesExistingConcreteSetting_WhenCalledViaBaseTypeReference()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            BaseSetting first = ScriptableObject.CreateInstance<TestBaseSetting>();
            BaseSetting second = ScriptableObject.CreateInstance<TestBaseSetting>();
            first.name = "First";
            second.name = "Second";

            selector.SetSetting(first);
            selector.SetSetting(second);

            Assert.That(selector.GetAllSettings(), Has.Count.EqualTo(1));
            Assert.That(selector.GetSetting<TestBaseSetting>().name, Is.EqualTo("Second"));
        }

        [Test]
        public void ProjectSettingSelector_SetSetting_CleansExistingDuplicatesForSameConcreteType()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            BaseSetting first = ScriptableObject.CreateInstance<TestBaseSetting>();
            BaseSetting second = ScriptableObject.CreateInstance<TestBaseSetting>();
            BaseSetting third = ScriptableObject.CreateInstance<TestBaseSetting>();
            first.name = "First";
            second.name = "Second";
            third.name = "Third";

            selector.SetSetting(first);
            selector.SetSetting(second);
            selector.SetSetting(third);

            Assert.That(selector.GetAllSettings(), Has.Count.EqualTo(1));
            Assert.That(selector.GetSetting<TestBaseSetting>().name, Is.EqualTo("Third"));
        }

        [Test]
        public void ProjectSettingSelector_SetComponentSetting_ReplacesExistingConcreteSetting()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var first = ScriptableObject.CreateInstance<TestComponentSetting>();
            var second = ScriptableObject.CreateInstance<TestComponentSetting>();
            first.name = "FirstComponent";
            first.bindTypeName = "Test.Component";
            second.name = "SecondComponent";
            second.bindTypeName = "Test.Component";

            selector.SetComponentSetting(first);
            selector.SetComponentSetting(second);

            Assert.That(selector.GetAllComponentSettings(), Has.Count.EqualTo(1));
            Assert.That(selector.GetComponentSetting<TestComponentSetting>().name, Is.EqualTo("SecondComponent"));
        }

        [Test]
        public void SettingManager_LoadsProjectSettingSelector_FromProjectAsset()
        {
            CreateFolder("Assets/__TempProjectOwnedSettingsTests");

            var projectSelector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var projectGameSetting = ScriptableObject.CreateInstance<GameSetting>();
            projectGameSetting.name = "ProjectGameSetting";
            projectSelector.SetSetting(projectGameSetting);

            AssetDatabase.CreateAsset(projectSelector, $"{TempRoot}/ProjectSettingSelector.asset");
            AssetDatabase.AddObjectToAsset(projectGameSetting, projectSelector);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SettingManager.ClearCacheForTests();

            var loadedSelector = SettingManager.GetProjectSelector();
            var resolved = SettingManager.GetSetting<GameSetting>();

            Assert.That(loadedSelector, Is.SameAs(projectSelector));
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.name, Is.EqualTo("ProjectGameSetting"));
        }

        private static void DeleteTempAssets()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }
        }

        private static void CreateFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                CreateFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private sealed class TestBaseSetting : BaseSetting
        {
        }

        private sealed class TestComponentSetting : ComponentSetting
        {
        }
    }
}
