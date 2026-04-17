using System;
using System.Collections.Generic;
using System.Reflection;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class ResourceComponentInspectorTests
    {
        [Test]
        public void BuildActivePackagePreview_ReturnsFilteredPackagesForPlatformAndChannel()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SetPrivateField(setting, "_yooAssetPackageName", string.Empty);
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
        public void BuildActivePackagePreview_UsesLegacyPackageFallback_WhenNoPackageDefinitionsExist()
        {
            var setting = ScriptableObject.CreateInstance<ResourceComponentSetting>();

            List<string> lines = InvokePreviewBuilder(setting, RuntimePlatform.WindowsEditor, "Google");

            Assert.That(lines, Has.Count.EqualTo(1));
            Assert.That(lines[0], Does.Contain("DefaultPackage"));
            Assert.That(lines[0], Does.Contain("Legacy"));
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

        private static void SetPrivateField(object instance, string name, object value)
        {
            FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {name}.");
            field.SetValue(instance, value);
        }
    }
}
