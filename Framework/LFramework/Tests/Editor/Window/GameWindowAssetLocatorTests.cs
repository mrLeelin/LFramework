using System.IO;
using NUnit.Framework;
using LFramework.Editor.Window;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowAssetLocatorTests
    {
        private const string TempPackageFolder = "Assets/__TempGameWindowPackage";

        [SetUp]
        public void SetUp()
        {
            DeleteTempFolder(TempPackageFolder);
            EnsureFolder(SettingProjectPaths.Root);
            EnsureFolder(SettingProjectPaths.BaseFolder);
            EnsureFolder(SettingProjectPaths.ComponentFolder);
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            DeleteTempFolder(TempPackageFolder);
            DeleteAsset($"{SettingProjectPaths.BaseFolder}/GameWindowLocatorBase.asset");
            DeleteAsset($"{SettingProjectPaths.ComponentFolder}/GameWindowLocatorComponent.asset");
            AssetDatabase.Refresh();
        }

        [Test]
        public void GetPreferredAssetsAtType_PrefersProjectOwnedBaseSettings_WhenDuplicateNamesExist()
        {
            EnsureFolder(TempPackageFolder);

            var projectAsset = ScriptableObject.CreateInstance<TestLocatorBaseSetting>();
            projectAsset.name = "GameWindowLocatorBase";
            AssetDatabase.CreateAsset(projectAsset, $"{SettingProjectPaths.BaseFolder}/GameWindowLocatorBase.asset");

            var packageAsset = ScriptableObject.CreateInstance<TestLocatorBaseSetting>();
            packageAsset.name = "GameWindowLocatorBase";
            AssetDatabase.CreateAsset(packageAsset, $"{TempPackageFolder}/GameWindowLocatorBase.asset");

            var result = GameWindowAssetLocator.GetPreferredAssetsAtType<TestLocatorBaseSetting>();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.SameAs(projectAsset));
        }

        [Test]
        public void GetPreferredAssetsAtType_PrefersProjectOwnedComponentSettings_WhenDuplicateNamesExist()
        {
            EnsureFolder(TempPackageFolder);

            var projectAsset = ScriptableObject.CreateInstance<TestLocatorComponentSetting>();
            projectAsset.name = "GameWindowLocatorComponent";
            AssetDatabase.CreateAsset(projectAsset, $"{SettingProjectPaths.ComponentFolder}/GameWindowLocatorComponent.asset");

            var packageAsset = ScriptableObject.CreateInstance<TestLocatorComponentSetting>();
            packageAsset.name = "GameWindowLocatorComponent";
            AssetDatabase.CreateAsset(packageAsset, $"{TempPackageFolder}/GameWindowLocatorComponent.asset");

            var result = GameWindowAssetLocator.GetPreferredAssetsAtType<TestLocatorComponentSetting>();

            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0], Is.SameAs(projectAsset));
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (!string.IsNullOrEmpty(parent))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void DeleteTempFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.DeleteAsset(folderPath);
            }
        }

        private static void DeleteAsset(string assetPath)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        private sealed class TestLocatorBaseSetting : BaseSetting
        {
        }

        private sealed class TestLocatorComponentSetting : ComponentSetting
        {
        }
    }
}
