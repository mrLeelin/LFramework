using NUnit.Framework;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using LFramework.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class SettingProjectInitializerTests
    {
        [SetUp]
        public void SetUp()
        {
            DeleteProjectSettingsRoot();
            AssetDatabase.Refresh();
            SettingManager.ClearCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            SettingManager.ClearCacheForTests();
            DeleteProjectSettingsRoot();
            AssetDatabase.Refresh();
        }

        [Test]
        public void SettingProjectPaths_UsesCanonicalProjectOwnedLayout()
        {
            Assert.That(SettingProjectPaths.Root, Is.EqualTo("Assets/Game/Settings"));
            Assert.That(SettingProjectPaths.SelectorAssetPath, Is.EqualTo("Assets/Game/Settings/Selector/ProjectSettingSelector.asset"));
            Assert.That(SettingProjectPaths.SyncStateAssetPath, Is.EqualTo("Assets/Game/Settings/Sync/SettingSyncState.asset"));
            Assert.That(SettingProjectPaths.BaseFolder, Is.EqualTo("Assets/Game/Settings/Base"));
            Assert.That(SettingProjectPaths.ComponentFolder, Is.EqualTo("Assets/Game/Settings/Components"));
            Assert.That(SettingProjectPaths.SnapshotFolder, Is.EqualTo("Assets/Game/Settings/Sync/Snapshots"));
        }

        [Test]
        public void InitializeProjectSettings_CreatesProjectSelectorSyncStateAndProjectOwnedAssets()
        {
            ProjectSettingSelector selector = SettingProjectInitializer.InitializeProjectSettings();
            SettingSyncState syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);

            Assert.That(selector, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(selector), Is.EqualTo(SettingProjectPaths.SelectorAssetPath));
            Assert.That(syncState, Is.Not.Null);
            Assert.That(selector.GetSetting<GameSetting>(), Is.Not.Null);
            Assert.That(selector.GetAllSettings().Count, Is.GreaterThan(0));
            Assert.That(selector.GetAllComponentSettings().Count, Is.GreaterThan(0));
            Assert.That(syncState.Records.Count, Is.EqualTo(selector.GetAllSettings().Count + selector.GetAllComponentSettings().Count));
            Assert.That(AssetDatabase.GetAssetPath(selector.GetSetting<GameSetting>()), Does.StartWith(SettingProjectPaths.BaseFolder + "/"));
        }

        [Test]
        public void InitializeProjectSettings_IsIdempotentAndDoesNotDuplicateRecords()
        {
            ProjectSettingSelector firstSelector = SettingProjectInitializer.InitializeProjectSettings();
            var firstGameSetting = firstSelector.GetSetting<GameSetting>();
            int firstBaseCount = firstSelector.GetAllSettings().Count;
            int firstComponentCount = firstSelector.GetAllComponentSettings().Count;

            ProjectSettingSelector secondSelector = SettingProjectInitializer.InitializeProjectSettings();
            SettingSyncState syncState = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);

            Assert.That(secondSelector, Is.SameAs(firstSelector));
            Assert.That(secondSelector.GetSetting<GameSetting>(), Is.SameAs(firstGameSetting));
            Assert.That(secondSelector.GetAllSettings().Count, Is.EqualTo(firstBaseCount));
            Assert.That(secondSelector.GetAllComponentSettings().Count, Is.EqualTo(firstComponentCount));
            Assert.That(syncState.Records.Count, Is.EqualTo(firstBaseCount + firstComponentCount));
        }

        [Test]
        public void LoadTemplateAssets_UsesRegistryEntriesWhenProvided()
        {
            var templateA = ScriptableObject.CreateInstance<TestRegistryBaseSetting>();
            var templateB = ScriptableObject.CreateInstance<TestRegistryComponentSetting>();
            var registry = ScriptableObject.CreateInstance<SettingTemplateRegistry>();
            registry.SetEntries(new[]
            {
                new SettingTemplateEntry { settingId = "test.base", templateAsset = templateA },
                new SettingTemplateEntry { settingId = "test.component", templateAsset = templateB }
            });

            var templates = SettingProjectInitializer.LoadTemplateAssets(registry);

            Assert.That(templates, Has.Count.EqualTo(2));
            Assert.That(templates, Does.Contain(templateA));
            Assert.That(templates, Does.Contain(templateB));
        }

        private static void DeleteProjectSettingsRoot()
        {
            if (AssetDatabase.IsValidFolder("Assets/Game"))
            {
                AssetDatabase.DeleteAsset("Assets/Game");
            }
        }

        private sealed class TestRegistryBaseSetting : BaseSetting
        {
        }

        private sealed class TestRegistryComponentSetting : ComponentSetting
        {
        }
    }
}
