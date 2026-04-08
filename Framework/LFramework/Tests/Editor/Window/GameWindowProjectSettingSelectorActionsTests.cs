using LFramework.Editor.Settings;
using LFramework.Editor.Window;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowProjectSettingSelectorActionsTests
    {
        private const string TempRoot = "Assets/__TempGameWindowProjectSettingSelectorActionsTests";

        [SetUp]
        public void SetUp()
        {
            DeleteProjectSettingsRoot();
            DeleteTempRoot();
            AssetDatabase.Refresh();
            SettingManager.ClearCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            SettingManager.ClearCacheForTests();
            DeleteProjectSettingsRoot();
            DeleteTempRoot();
            AssetDatabase.Refresh();
        }

        [Test]
        public void CollectAllSettings_PopulatesSelectedProjectSettingSelector()
        {
            EnsureTempRoot();
            ProjectSettingSelector selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            AssetDatabase.CreateAsset(selector, $"{TempRoot}/ProjectSettingSelector.asset");

            GameWindowProjectSettingSelectorActions.CollectAllSettings(selector);

            Assert.That(selector.GetAllSettings().Count, Is.GreaterThan(0));
            Assert.That(selector.GetAllComponentSettings().Count, Is.GreaterThan(0));
            Assert.That(AssetDatabase.GetAssetPath(selector.GetSetting<GameSetting>()), Does.StartWith(SettingProjectPaths.BaseFolder + "/"));
        }

        [Test]
        public void ValidateAllSettings_ReturnsReportWithoutErrors_AfterCollectAllSettings()
        {
            EnsureTempRoot();
            ProjectSettingSelector selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            AssetDatabase.CreateAsset(selector, $"{TempRoot}/ProjectSettingSelector.asset");

            GameWindowProjectSettingSelectorActions.CollectAllSettings(selector);

            SettingValidationReport report = GameWindowProjectSettingSelectorActions.ValidateAllSettings(selector);

            Assert.That(report, Is.Not.Null);
            Assert.That(report.HasErrors, Is.False);
        }

        private static void EnsureTempRoot()
        {
            if (!AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.CreateFolder("Assets", "__TempGameWindowProjectSettingSelectorActionsTests");
            }
        }

        private static void DeleteTempRoot()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }
        }

        private static void DeleteProjectSettingsRoot()
        {
            if (AssetDatabase.IsValidFolder("Assets/Game"))
            {
                AssetDatabase.DeleteAsset("Assets/Game");
            }
        }
    }
}
