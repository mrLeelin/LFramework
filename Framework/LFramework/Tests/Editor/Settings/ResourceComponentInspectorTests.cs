using System;
using System.Collections.Generic;
using System.Reflection;
using LFramework.Editor;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class ResourceComponentInspectorTests
    {
        [Test]
        public void BuildActivePackagePreview_ReturnsFilteredPackagesForPlatformAndChannel()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage_Windows",
                platformFilter = new List<string> { RuntimePlatform.WindowsEditor.ToString() },
                channelFilter = new List<string> { "Google" }
            });
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "ui",
                yooPackageName = "UIPackage_Android",
                platformFilter = new List<string> { RuntimePlatform.Android.ToString() },
                channelFilter = new List<string> { "Google" }
            });
            setting.YooAssetPackages.Add(new PackageDefinition
            {
                packageId = "scene",
                yooPackageName = "ScenePackage_Default"
            });

            List<string> lines = InvokePreviewBuilder(setting, RuntimePlatform.WindowsEditor, "Google");

            Assert.That(lines, Has.Count.EqualTo(2));
            Assert.That(lines.Exists(line => line.Contains("ui") && line.Contains("UIPackage_Windows")), Is.True);
            Assert.That(lines.Exists(line => line.Contains("scene") && line.Contains("ScenePackage_Default")), Is.True);
            Assert.That(lines.Exists(line => line.Contains("UIPackage_Android")), Is.False);
        }

        [Test]
        public void BuildActivePackagePreview_ReturnsEmpty_WhenNoPackageDefinitionsExist()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            List<string> lines = InvokePreviewBuilder(setting, RuntimePlatform.WindowsEditor, "Google");

            Assert.That(lines, Is.Empty);
        }

        [Test]
        public void Inspector_ExposesRouteIndexGenerationAction()
        {
            Type inspectorType = typeof(SettingProjectPaths).Assembly.GetType("LFramework.Editor.Inspector.ResourceComponentInspector");
            Assert.That(inspectorType, Is.Not.Null, "Expected ResourceComponentInspector type.");

            MethodInfo method = inspectorType.GetMethod(
                "ExecuteRouteIndexGeneration",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected route index generation action on ResourceComponentInspector.");
        }

        [Test]
        public void RouteIndexGenerator_DoesNotExposeLegacyMenuEntry()
        {
            MethodInfo method = typeof(RouteIndexGenerator).GetMethod(
                "GenerateFromMenu",
                BindingFlags.Static | BindingFlags.Public);

            Assert.That(method, Is.Null, "Expected route index generation to be launched from ResourceComponentInspector instead of a menu item.");
        }

        [Test]
        public void Inspector_DoesNotExposeRouteIndexBuildSettingFactory()
        {
            Type inspectorType = typeof(SettingProjectPaths).Assembly.GetType("LFramework.Editor.Inspector.ResourceComponentInspector");
            Assert.That(inspectorType, Is.Not.Null, "Expected ResourceComponentInspector type.");

            MethodInfo method = inspectorType.GetMethod(
                "CreateRouteIndexBuildSetting",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Null, "Expected route index action to stay generation-only in the inspector.");
        }

        [Test]
        public void RouteIndexGenerator_UsesHybridClrInitLabel_ForCollectorTags()
        {
            var hybridClrSetting = ScriptableObject.CreateInstance<HybridCLRSetting>();
            hybridClrSetting.defaultInitLabel = "init_assets_custom";

            string result = InvokeRouteIndexCollectorTagResolver(hybridClrSetting);

            Assert.That(result, Is.EqualTo("init_assets_custom"));
        }

        [Test]
        public void ResolveRouteIndexAssetPath_UsesRoutingConfiguration()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetRouting.routeIndexAssetPath = "Assets/Custom/RouteIndex.asset";

            string result = InvokeRouteIndexAssetPathResolver(setting);

            Assert.That(result, Is.EqualTo("Assets/Custom/RouteIndex.asset"));
        }

        [Test]
        public void CollectRouteIndexPackageIds_ReturnsDistinctConfiguredPackageIds()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui" });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = string.Empty });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "scene" });
            setting.YooAssetPackages.Add(new PackageDefinition { packageId = "ui" });

            var serializedObject = new SerializedObject(setting);
            SerializedProperty packagesProperty = serializedObject.FindProperty("_yooAssetPackages");

            List<string> result = InvokeRouteIndexPackageIdCollector(packagesProperty);

            Assert.That(result, Is.EqualTo(new[] { "ui", "scene" }));
        }

        private static List<string> InvokePreviewBuilder(ResourceComponentSetting setting, RuntimePlatform platform, string channel)
        {
            Type inspectorType = typeof(SettingProjectPaths).Assembly.GetType("LFramework.Editor.Inspector.ResourceComponentInspector");
            Assert.That(inspectorType, Is.Not.Null, "Expected ResourceComponentInspector type.");
            MethodInfo method = inspectorType.GetMethod(
                "BuildActivePackagePreview",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected BuildActivePackagePreview helper on ResourceComponentInspector.");

            object result = method.Invoke(null, new object[] { setting, platform, channel });
            Assert.That(result, Is.AssignableTo<List<string>>());
            return (List<string>)result;
        }

        private static string InvokeRouteIndexCollectorTagResolver(HybridCLRSetting hybridClrSetting)
        {
            MethodInfo method = typeof(RouteIndexGenerator).GetMethod(
                "ResolveRouteIndexAssetTags",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected route index collector tag resolver on RouteIndexGenerator.");

            object result = method.Invoke(null, new object[] { hybridClrSetting });
            Assert.That(result, Is.AssignableTo<string>());
            return (string)result;
        }

        private static string InvokeRouteIndexAssetPathResolver(ResourceComponentSetting setting)
        {
            Type inspectorType = typeof(SettingProjectPaths).Assembly.GetType("LFramework.Editor.Inspector.ResourceComponentInspector");
            Assert.That(inspectorType, Is.Not.Null, "Expected ResourceComponentInspector type.");
            MethodInfo method = inspectorType.GetMethod(
                "ResolveRouteIndexAssetPath",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected route index asset path resolver on ResourceComponentInspector.");

            object result = method.Invoke(null, new object[] { setting });
            Assert.That(result, Is.AssignableTo<string>());
            return (string)result;
        }

        private static List<string> InvokeRouteIndexPackageIdCollector(SerializedProperty packagesProperty)
        {
            Type inspectorType = typeof(SettingProjectPaths).Assembly.GetType("LFramework.Editor.Inspector.ResourceComponentInspector");
            Assert.That(inspectorType, Is.Not.Null, "Expected ResourceComponentInspector type.");
            MethodInfo method = inspectorType.GetMethod(
                "CollectRouteIndexPackageIds",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected route index package collector on ResourceComponentInspector.");

            object result = method.Invoke(null, new object[] { packagesProperty });
            Assert.That(result, Is.AssignableTo<List<string>>());
            return (List<string>)result;
        }

    }
}
