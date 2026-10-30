using System;
using System.Reflection;
using GameFramework.Resource;
using LFramework.Editor.Builder;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage
{
    /// <summary>
    /// 验证构建入口使用当前工程注册的资源配置，不扫描模板资产。
    /// </summary>
    public class ResourceSettingSelectionTests
    {
        private static readonly FieldInfo SelectorField = typeof(SettingManager).GetField(
            "_projectSelector", BindingFlags.Static | BindingFlags.NonPublic);

        private ProjectSettingSelector _originalSelector;
        private ProjectSettingSelector _selector;
        private ResourceComponentSetting _resourceSetting;

        [SetUp]
        public void SetUp()
        {
            Assert.That(SelectorField, Is.Not.Null);
            _originalSelector = (ProjectSettingSelector)SelectorField.GetValue(null);
            _selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            _resourceSetting = ScriptableObject.CreateInstance<ResourceComponentSetting>();
            SelectorField.SetValue(null, _selector);
        }

        [TearDown]
        public void TearDown()
        {
            SelectorField.SetValue(null, _originalSelector);
            UnityEngine.Object.DestroyImmediate(_selector);
            UnityEngine.Object.DestroyImmediate(_resourceSetting);
        }

        [TestCase(ResourceMode.Addressable)]
        [TestCase(ResourceMode.YooAsset)]
        public void Constructor_UsesResourceModeFromProjectSelector(ResourceMode mode)
        {
            var serialized = new SerializedObject(_resourceSetting);
            serialized.FindProperty("_resourceMode").intValue = (int)mode;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _selector.SetComponentSetting(_resourceSetting);
            var buildSetting = new BuildSetting();

            new BuildOrchestrator(buildSetting);

            Assert.That(buildSetting.ResourceSystem, Is.EqualTo(mode));
        }

        [Test]
        public void Constructor_WhenResourceSettingIsNotRegistered_FailsInsteadOfUsingTemplate()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => new BuildOrchestrator(new BuildSetting()));

            Assert.That(exception.Message, Does.Contain("ResourceComponentSetting"));
            Assert.That(exception.Message, Does.Contain("ProjectSettingSelector"));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ResourceBuildConversion_PreservesParentPlayerReleaseMode(bool isRelease)
        {
            var data = new BuildResourcesData { IsRelease = isRelease };
            MethodInfo convert = typeof(BuildResourcesData).GetMethod(
                "ConvertToBuildSetting", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(convert, Is.Not.Null);

            var buildSetting = (BuildSetting)convert.Invoke(null, new object[] { data });

            Assert.That(buildSetting.isRelease, Is.EqualTo(isRelease));
        }
    }
}
