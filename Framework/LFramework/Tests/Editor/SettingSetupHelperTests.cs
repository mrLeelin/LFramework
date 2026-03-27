using System.IO;
using NUnit.Framework;
using LFramework.Editor.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests
{
    public class SettingSetupHelperTests
    {
        private const string SettingSetupHelperScriptGuid = "ba7c8ec35e9a0cc4fbcd7a7b13ddd441";

        [Test]
        public void GetSettingsPath_ResolvesSettingsFolderRelativeToHelperScript()
        {
            string scriptAssetPath = AssetDatabase.GUIDToAssetPath(SettingSetupHelperScriptGuid);
            Assert.That(scriptAssetPath, Is.Not.Empty, "SettingSetupHelper.cs path could not be resolved from its GUID.");

            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string scriptFullPath = Path.GetFullPath(Path.Combine(projectRoot, scriptAssetPath));
            string scriptDirectory = Path.GetDirectoryName(scriptFullPath);

            Assert.That(scriptDirectory, Is.Not.Null.And.Not.Empty, "SettingSetupHelper.cs directory is missing.");

            string expectedSettingsFullPath = Path.GetFullPath(Path.Combine(scriptDirectory, "../../../Assets/Settings"));
            string expectedSettingsAssetPath = NormalizeAssetPath(Path.GetRelativePath(projectRoot, expectedSettingsFullPath));

            string actualSettingsAssetPath = SettingSetupHelper.GetSettingsPath();

            Assert.That(actualSettingsAssetPath, Is.EqualTo(expectedSettingsAssetPath));
            Assert.That(AssetDatabase.IsValidFolder(actualSettingsAssetPath), Is.True);
        }

        private static string NormalizeAssetPath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
