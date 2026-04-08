using NUnit.Framework;
using LFramework.Editor.Window;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowSettingPageSupportTests
    {
        [Test]
        public void TryCreate_ReturnsPageModel_ForComponentSetting()
        {
            var setting = ScriptableObject.CreateInstance<TestComponentSetting>();

            bool success = GameWindowSettingPageSupport.TryCreate(setting, out GameWindowSettingPageModel model);

            Assert.That(success, Is.True);
            Assert.That(model.Target, Is.SameAs(setting));
        }

        [Test]
        public void TryCreate_ReturnsPageModel_ForBaseSetting()
        {
            var setting = ScriptableObject.CreateInstance<TestBaseSetting>();

            bool success = GameWindowSettingPageSupport.TryCreate(setting, out GameWindowSettingPageModel model);

            Assert.That(success, Is.True);
            Assert.That(model.Target, Is.SameAs(setting));
        }

        [Test]
        public void TryCreate_ReturnsPageModel_ForProjectSettingSelector()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();

            bool success = GameWindowSettingPageSupport.TryCreate(selector, out GameWindowSettingPageModel model);

            Assert.That(success, Is.True);
            Assert.That(model.Target, Is.SameAs(selector));
        }

        [Test]
        public void TryCreate_ReturnsFalse_ForUnsupportedObject()
        {
            var unsupported = ScriptableObject.CreateInstance<TestUnsupportedObject>();

            bool success = GameWindowSettingPageSupport.TryCreate(unsupported, out _);

            Assert.That(success, Is.False);
        }

        private sealed class TestBaseSetting : BaseSetting
        {
        }

        private sealed class TestComponentSetting : ComponentSetting
        {
        }

        private sealed class TestUnsupportedObject : ScriptableObject
        {
        }
    }
}
