using System;
using System.Reflection;
using LFramework.Editor;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Settings
{
    public class AssetBundleCollectorRouteIndexSaveSyncTests
    {
        [Test]
        public void ShouldQueueRouteIndexGeneration_ReturnsTrue_WhenSavedPathsContainCollectorSetting()
        {
            bool result = InvokeShouldQueueRouteIndexGeneration(
                new[] { @"Assets\Framework\AssetBundleCollectorSetting.asset", "Assets/Other.asset" },
                "Assets/Framework/AssetBundleCollectorSetting.asset",
                false);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldQueueRouteIndexGeneration_ReturnsFalse_WhenSavedPathsDoNotContainCollectorSetting()
        {
            bool result = InvokeShouldQueueRouteIndexGeneration(
                new[] { "Assets/Other.asset", "Assets/Another.asset" },
                "Assets/Framework/AssetBundleCollectorSetting.asset",
                false);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldQueueRouteIndexGeneration_ReturnsFalse_WhenSuppressed()
        {
            bool result = InvokeShouldQueueRouteIndexGeneration(
                new[] { "Assets/Framework/AssetBundleCollectorSetting.asset" },
                "Assets/Framework/AssetBundleCollectorSetting.asset",
                true);

            Assert.That(result, Is.False);
        }

        [Test]
        public void CollectorImportSync_ExposesPostprocessHook()
        {
            Type syncType = typeof(RouteIndexGenerator).Assembly.GetType("LFramework.Editor.AssetBundleCollectorRouteIndexImportSync");
            Assert.That(syncType, Is.Not.Null, "Expected collector import sync type.");

            MethodInfo method = syncType.GetMethod(
                "OnPostprocessAllAssets",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected import-time postprocess hook for collector route-index sync.");
        }

        private static bool InvokeShouldQueueRouteIndexGeneration(
            string[] paths,
            string collectorSettingPath,
            bool suppressEnqueue)
        {
            Type syncType = typeof(RouteIndexGenerator).Assembly.GetType("LFramework.Editor.AssetBundleCollectorRouteIndexSaveSync");
            Assert.That(syncType, Is.Not.Null, "Expected collector save sync type.");

            MethodInfo method = syncType.GetMethod(
                "ShouldQueueRouteIndexGeneration",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected save-path filter helper on AssetBundleCollectorRouteIndexSaveSync.");

            object result = method.Invoke(null, new object[] { paths, collectorSettingPath, suppressEnqueue });
            Assert.That(result, Is.AssignableTo<bool>());
            return (bool)result;
        }
    }
}
