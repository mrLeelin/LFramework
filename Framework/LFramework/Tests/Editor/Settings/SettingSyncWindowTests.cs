using NUnit.Framework;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class SettingSyncWindowTests
    {
        [TearDown]
        public void TearDown()
        {
            SettingSyncWindow existingWindow = EditorWindow.HasOpenInstances<SettingSyncWindow>()
                ? EditorWindow.GetWindow<SettingSyncWindow>()
                : null;
            if (existingWindow != null)
            {
                existingWindow.Close();
            }
        }

        [Test]
        public void OpenWindow_CreatesWindowWithExpectedTitle()
        {
            SettingSyncWindow window = SettingSyncWindow.OpenWindow();

            Assert.That(window, Is.Not.Null);
            Assert.That(window.titleContent.text, Is.EqualTo("Setting Sync"));
        }

        [Test]
        public void RefreshReport_StoresLatestReport()
        {
            SettingSyncWindow window = SettingSyncWindow.OpenWindow();
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var template = ScriptableObject.CreateInstance<TestWindowSetting>();
            template.EditorSetSettingId("window.test");

            window.RefreshReport(selector, syncState, new ScriptableObject[] { template });

            Assert.That(window.CurrentReport, Is.Not.Null);
            Assert.That(window.CurrentReport.Items, Has.Count.EqualTo(1));
            Assert.That(window.CurrentReport.Items[0].status, Is.EqualTo(SettingSyncStatus.New));
        }

        private sealed class TestWindowSetting : BaseSetting
        {
        }
    }
}
