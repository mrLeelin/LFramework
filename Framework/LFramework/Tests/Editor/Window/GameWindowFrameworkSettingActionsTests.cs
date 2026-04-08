using NUnit.Framework;
using LFramework.Editor.Window;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Window
{
    public class GameWindowFrameworkSettingActionsTests
    {
        [Test]
        public void HandleAssetBadgeClick_SelectsAsset_WhenBadgeIndexIsAsset()
        {
            var asset = ScriptableObject.CreateInstance<TestActionAsset>();
            Selection.activeObject = null;

            bool handled = GameWindowFrameworkSettingActions.HandleAssetBadgeClick(asset, 0);

            Assert.That(handled, Is.True);
            Assert.That(Selection.activeObject, Is.SameAs(asset));
        }

        [Test]
        public void HandleAssetBadgeClick_IgnoresNonAssetBadges()
        {
            var asset = ScriptableObject.CreateInstance<TestActionAsset>();
            Selection.activeObject = null;

            bool handled = GameWindowFrameworkSettingActions.HandleAssetBadgeClick(asset, 1);

            Assert.That(handled, Is.False);
            Assert.That(Selection.activeObject, Is.Null);
        }

        private sealed class TestActionAsset : ScriptableObject
        {
        }
    }
}
