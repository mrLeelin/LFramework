using System.Collections.Generic;
using System.Linq;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class YooAssetMultiPackageUtilityTests
    {
        [Test]
        public void CollectBuildPackages_ReturnsActivePackagesWithDefaultPackageFirst()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "base");
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage" });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "base", yooPackageName = "BasePackage" });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "scene", yooPackageName = "ScenePackage" });

            List<PackageDefinition> packages = YooAssetMultiPackageUtility.CollectBuildPackages(
                setting,
                RuntimePlatform.WindowsEditor,
                "Google");

            Assert.That(packages.Select(item => item.packageId).ToArray(), Is.EqualTo(new[] { "base", "scene", "ui" }));
        }

        [Test]
        public void ResolveDefaultPackageName_UsesFallbackChain_WhenConfiguredDefaultPackageIsInactive()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "premium-ui");
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "premium-ui",
                yooPackageName = "PremiumUIPackage",
                fallbackPackageId = "shared-ui",
                platformFilter = new List<string> { RuntimePlatform.Android.ToString() }
            });
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "shared-ui",
                yooPackageName = "SharedUIPackage"
            });

            string packageName = YooAssetMultiPackageUtility.ResolveDefaultPackageName(
                setting,
                RuntimePlatform.WindowsEditor,
                "Google");

            Assert.That(packageName, Is.EqualTo("SharedUIPackage"));
        }

        [Test]
        public void CollectManifestUpdatePackages_IncludesRouteIndexPackage_EvenWhenItsFlagIsDisabled()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "base");
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "base", yooPackageName = "BasePackage", updateManifestOnLaunch = false });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "routes", yooPackageName = "RoutePackage", updateManifestOnLaunch = false });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui", yooPackageName = "UIPackage", updateManifestOnLaunch = true });
            setting.YooAssetRouting.routeIndexPackageId = "routes";

            List<PackageDefinition> packages = YooAssetMultiPackageUtility.CollectManifestUpdatePackages(
                setting,
                RuntimePlatform.WindowsEditor,
                "Google");

            Assert.That(packages.Select(item => item.packageId).ToArray(), Is.EqualTo(new[] { "routes", "ui" }));
        }

        [Test]
        public void CollectDownloadPlans_MergesGlobalInitAndPackageLabels_PerPackage()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "base");
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "base",
                yooPackageName = "BasePackage",
                downloadOnLaunch = false
            });
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "routes",
                yooPackageName = "RoutePackage",
                downloadOnLaunch = false
            });
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage",
                downloadOnLaunch = true
            });
            setting.YooAssetRouting.routeIndexPackageId = "routes";

            List<YooAssetPackageDownloadPlan> plans = YooAssetMultiPackageUtility.CollectDownloadPlans(
                setting,
                RuntimePlatform.WindowsEditor,
                "Google",
                new[] { "hotfix" });

            Assert.That(plans.Select(item => item.PackageId).ToArray(), Is.EqualTo(new[] { "routes", "ui" }));
            Assert.That(plans[0].PackageName, Is.EqualTo("RoutePackage"));
            Assert.That(plans[0].Labels, Is.EquivalentTo(new[] { "hotfix", "init_assets" }));
            Assert.That(plans[1].PackageName, Is.EqualTo("UIPackage"));
            Assert.That(plans[1].Labels, Is.EquivalentTo(new[] { "ui_group", "hotfix", "init_assets" }));
        }

        [Test]
        public void ResolveRouteIndexPackageName_UsesDefaultPackage_WhenRouteIndexPackageIdIsEmpty()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_defaultPackageId", "base");
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "base", yooPackageName = "BasePackage" });

            string packageName = YooAssetMultiPackageUtility.ResolveRouteIndexPackageName(
                setting,
                RuntimePlatform.WindowsEditor,
                "Google");

            Assert.That(packageName, Is.EqualTo("BasePackage"));
        }

        private static void SetPrivateField(object instance, string name, object value)
        {
            var field = instance.GetType().GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {name}.");
            field.SetValue(instance, value);
        }
    }
}
