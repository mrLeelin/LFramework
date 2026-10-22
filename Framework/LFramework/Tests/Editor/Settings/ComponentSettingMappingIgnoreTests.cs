using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using GameFramework;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;
// 注意：LFramework.Editor.Tests 下存在同级命名空间 LFramework.Editor.Tests.ResourceComponent，
// 裸写 ResourceComponent 会被解析为该命名空间；因此本文件统一使用完整限定名 UnityGameFramework.Runtime.ResourceComponent。

namespace LFramework.Editor.Tests.Settings
{
    /// <summary>
    /// 验证 <see cref="ComponentSettingMappingIgnoreAttribute"/> 的映射语义，以及未映射字段的编辑器诊断。
    /// </summary>
    public sealed class ComponentSettingMappingIgnoreTests
    {
        private const string ComponentSettingsFolder =
            "Assets/Frame/Framework/LFramework/Assets/Settings/ComponentSettings";

        private const string ResourceComponentSettingAssetPath =
            ComponentSettingsFolder + "/ResourceComponentSetting.asset";

        private const string LocalizationComponentSettingAssetPath =
            ComponentSettingsFolder + "/LocalizationComponentSetting.asset";

        private static readonly MethodInfo ConvertToComponentMethod =
            typeof(ComponentSettingExtension).GetMethod("CovertToComponent",
                BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// 已确认「仅由 Helper 读取、不参与组件字段映射」的 11 个设置字段。
        /// </summary>
        private static readonly (Type SettingType, string FieldName)[] KnownUnmappedSettingFields =
        {
            (typeof(ResourceComponentSetting), "_defaultPackageId"),
            (typeof(ResourceComponentSetting), "_yooAssetPackages"),
            (typeof(ResourceComponentSetting), "_routing"),
            (typeof(ResourceComponentSetting), "_hotfixProfileName"),
            (typeof(ResourceComponentSetting), "_forceSingleSlashUrls"),
            (typeof(ResourceComponentSetting), "_logLoadUrls"),
            (typeof(LocalizationComponentSetting), "m_EnableLoadDictionaryUpdateEvent"),
            (typeof(LocalizationComponentSetting), "m_EnableLoadDictionaryDependencyAssetEvent"),
            (typeof(LocalizationComponentSetting), "m_LocalizationHelperTypeName"),
            (typeof(LocalizationComponentSetting), "m_CustomLocalizationHelper"),
            (typeof(LocalizationComponentSetting), "m_CachedBytesSize"),
        };

        /// <summary>
        /// 已确认「设置字段 - 组件字段」同名字段对，映射时要求双方字段类型完全一致。
        /// </summary>
        private static readonly (Type SettingType, Type ComponentType, string FieldName)[] KnownMappedFieldPairs =
        {
            (typeof(ResourceComponentSetting), typeof(UnityGameFramework.Runtime.ResourceComponent), "_resourceMode"),
            (typeof(ResourceComponentSetting), typeof(UnityGameFramework.Runtime.ResourceComponent), "m_ResourceHelperTypeName"),
            (typeof(ResourceComponentSetting), typeof(UnityGameFramework.Runtime.ResourceComponent), "m_CustomResourceHelper"),
            (typeof(ResourceComponentSetting), typeof(UnityGameFramework.Runtime.ResourceComponent), "_minUnloadInterval"),
            (typeof(ResourceComponentSetting), typeof(UnityGameFramework.Runtime.ResourceComponent), "_maxUnloadInterval"),
        };

        [SetUp]
        public void SetUp()
        {
            GameFrameworkEntry.Shutdown();
        }

        [TearDown]
        public void TearDown()
        {
            GameFrameworkEntry.Shutdown();
        }

        [Test]
        public void CovertToComponent_IgnoredFieldAbsentOnComponent_CopiesMappedFieldsWithoutDiagnostics()
        {
            var setting = ScriptableObject.CreateInstance<IgnoredFieldAbsentSetting>();
            try
            {
                setting.bindTypeName = typeof(IgnoredFieldAbsentComponent).FullName;
                setting.mappedValue = "mapped-value";
                setting.ignoredValue = "ignored-value";

                var component = (IgnoredFieldAbsentComponent)ConvertToComponent(setting);

                Assert.That(component.mappedValue, Is.EqualTo("mapped-value"));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void CovertToComponent_IgnoredFieldPresentOnComponent_DoesNotCopyIt()
        {
            var setting = ScriptableObject.CreateInstance<IgnoredFieldPresentSetting>();
            try
            {
                setting.bindTypeName = typeof(IgnoredFieldPresentComponent).FullName;
                setting.mappedValue = "mapped-value";
                setting.ignoredValue = "ignored-value";

                var component = (IgnoredFieldPresentComponent)ConvertToComponent(setting);

                Assert.That(component.ignoredValue, Is.EqualTo(IgnoredFieldPresentComponent.DefaultIgnoredValue));
                Assert.That(component.mappedValue, Is.EqualTo("mapped-value"));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void CovertToComponent_UnmarkedFieldMissingOnComponent_LogsErrorAndStillMapsRemainingFields()
        {
            var setting = ScriptableObject.CreateInstance<UnmarkedFieldAbsentSetting>();
            try
            {
                setting.bindTypeName = typeof(UnmarkedFieldAbsentComponent).FullName;
                setting.mappedValue = "mapped-value";
                setting.unmappedValue = "unmapped-value";

                LogAssert.Expect(LogType.Error,
                    CreateMissingFieldDiagnostic(typeof(UnmarkedFieldAbsentSetting), "unmappedValue",
                        typeof(UnmarkedFieldAbsentComponent)));

                var component = (UnmarkedFieldAbsentComponent)ConvertToComponent(setting);

                Assert.That(component.mappedValue, Is.EqualTo("mapped-value"));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void CovertToComponent_ShippedResourceComponentSettingTemplate_CreatesResourceComponentWithoutDiagnostics()
        {
            var setting = AssetDatabase.LoadAssetAtPath<ResourceComponentSetting>(ResourceComponentSettingAssetPath);
            Assert.That(setting, Is.Not.Null, $"未找到模板资产：{ResourceComponentSettingAssetPath}");
            Assert.That(setting.bindTypeName, Is.EqualTo(typeof(UnityGameFramework.Runtime.ResourceComponent).FullName));

            var component = ConvertToComponent(setting);

            Assert.That(component, Is.TypeOf<UnityGameFramework.Runtime.ResourceComponent>());
            Assert.That(GetInstanceFieldValue(component, "_resourceMode"),
                Is.EqualTo(GetInstanceFieldValue(setting, "_resourceMode")));
            Assert.That(GetInstanceFieldValue(component, "_minUnloadInterval"), Is.EqualTo(60f));
            Assert.That(GetInstanceFieldValue(component, "_maxUnloadInterval"), Is.EqualTo(300f));
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void CovertToComponent_ShippedLocalizationComponentSettingTemplate_CreatesLanguageComponentWithoutDiagnostics()
        {
            var setting = AssetDatabase.LoadAssetAtPath<LocalizationComponentSetting>(
                LocalizationComponentSettingAssetPath);
            Assert.That(setting, Is.Not.Null, $"未找到模板资产：{LocalizationComponentSettingAssetPath}");
            Assert.That(setting.bindTypeName, Is.EqualTo(typeof(LanguageComponent).FullName));

            var component = ConvertToComponent(setting);

            Assert.That(component, Is.TypeOf<LanguageComponent>());
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void AllKnownUnmappedSettingFields_AreMarkedWithComponentSettingMappingIgnore()
        {
            Assert.That(KnownUnmappedSettingFields, Has.Length.EqualTo(11));

            var missingFields = new List<string>();
            foreach (var (settingType, fieldName) in KnownUnmappedSettingFields)
            {
                var field = FindInstanceField(settingType, fieldName);
                if (field == null)
                {
                    missingFields.Add($"{settingType.FullName}.{fieldName} (field not found)");
                    continue;
                }

                if (field.GetCustomAttribute<ComponentSettingMappingIgnoreAttribute>(true) == null)
                {
                    missingFields.Add($"{settingType.FullName}.{fieldName}");
                }
            }

            Assert.That(missingFields, Is.Empty,
                "以下字段缺少 [ComponentSettingMappingIgnore]：" + string.Join(", ", missingFields));
        }

        [Test]
        public void ResourceComponentSetting_CustomResourceHelperFieldTypeMatchesComponent()
        {
            var settingField = FindInstanceField(typeof(ResourceComponentSetting), "m_CustomResourceHelper");
            var componentField = FindInstanceField(typeof(UnityGameFramework.Runtime.ResourceComponent), "m_CustomResourceHelper");

            Assert.That(settingField, Is.Not.Null, "ResourceComponentSetting.m_CustomResourceHelper 字段不存在。");
            Assert.That(componentField, Is.Not.Null, "ResourceComponent.m_CustomResourceHelper 字段不存在。");

            Assert.That(settingField.FieldType, Is.EqualTo(typeof(ResourceHelperBase)),
                "ResourceComponentSetting.m_CustomResourceHelper 应为 UnityGameFramework.Runtime.ResourceHelperBase。");
            Assert.That(settingField.FieldType, Is.EqualTo(componentField.FieldType),
                "设置字段与组件字段类型不一致时，映射会被静默跳过。");

            var asymmetries = new List<string>();
            foreach (var (settingType, componentType, fieldName) in KnownMappedFieldPairs)
            {
                var settingFieldInfo = FindInstanceField(settingType, fieldName);
                var componentFieldInfo = FindInstanceField(componentType, fieldName);
                if (settingFieldInfo == null || componentFieldInfo == null)
                {
                    asymmetries.Add(
                        $"{settingType.FullName}.{fieldName} 或 {componentType.FullName}.{fieldName} 不存在");
                    continue;
                }

                if (settingFieldInfo.FieldType != componentFieldInfo.FieldType)
                {
                    asymmetries.Add(
                        $"{settingType.FullName}.{fieldName} ({settingFieldInfo.FieldType.Name}) != " +
                        $"{componentType.FullName}.{fieldName} ({componentFieldInfo.FieldType.Name})");
                }
            }

            Assert.That(asymmetries, Is.Empty, "以下映射字段对的类型不一致：" + string.Join("; ", asymmetries));
        }

        [Test]
        public void ComponentSettingMappingIgnoreAttribute_IsFieldOnlySealedAndInherited()
        {
            var attributeType = typeof(ComponentSettingMappingIgnoreAttribute);

            Assert.That(attributeType.IsSealed, Is.True);
            Assert.That(attributeType.BaseType, Is.EqualTo(typeof(Attribute)));

            var usage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();
            Assert.That(usage, Is.Not.Null);
            Assert.That(usage.ValidOn, Is.EqualTo(AttributeTargets.Field));
            Assert.That(usage.AllowMultiple, Is.False);
            Assert.That(usage.Inherited, Is.True);
        }

        [Test]
        public void CovertToComponent_WidenedLookup_MapsPublicAndBaseClassPrivateComponentFields()
        {
            var setting = ScriptableObject.CreateInstance<WidenedLookupSetting>();
            try
            {
                setting.bindTypeName = typeof(WidenedLookupComponent).FullName;
                setting.derivedPublicValue = "public-value";
                setting.basePrivateValue = "base-private-value";

                var component = (WidenedLookupComponent)ConvertToComponent(setting);

                Assert.That(component.DerivedPublicValue, Is.EqualTo("public-value"));
                Assert.That(component.BasePrivateValue, Is.EqualTo("base-private-value"));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(setting);
            }
        }

        /// <summary>
        /// 构造「未映射字段」诊断消息的正则（未锚定，允许日志前缀与调用点后缀）。
        /// </summary>
        private static Regex CreateMissingFieldDiagnostic(Type settingType, string fieldName, Type componentType)
        {
            return new Regex(
                "\\[ComponentSetting\\] Field '" + Regex.Escape(settingType.FullName) + "\\." +
                Regex.Escape(fieldName) + "' is not mapped: component '" + Regex.Escape(componentType.FullName) +
                "' has no field with that name\\.");
        }

        private static GameFrameworkComponent ConvertToComponent(ComponentSetting setting)
        {
            Assert.That(ConvertToComponentMethod, Is.Not.Null,
                "通过反射未找到 ComponentSettingExtension.CovertToComponent。");

            var component = ConvertToComponentMethod.Invoke(null, new object[] { setting });

            Assert.That(component, Is.Not.Null, $"设置类型 '{setting.GetType().FullName}' 未能转换出组件。");
            return (GameFrameworkComponent)component;
        }

        private static object GetInstanceFieldValue(object target, string fieldName)
        {
            var field = FindInstanceField(target.GetType(), fieldName);
            Assert.That(field, Is.Not.Null, $"字段 '{fieldName}' 不存在于类型 '{target.GetType().FullName}'。");
            return field.GetValue(target);
        }

        /// <summary>
        /// 沿类型继承链查找实例字段（包含 public 与基类私有字段）。
        /// </summary>
        private static FieldInfo FindInstanceField(Type type, string fieldName)
        {
            for (var current = type; current != null && current != typeof(object); current = current.BaseType)
            {
                var field = current.GetField(fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                {
                    return field;
                }
            }

            return null;
        }

        private sealed class IgnoredFieldAbsentSetting : ComponentSetting
        {
            [ComponentSettingMappingIgnore]
            public string ignoredValue;

            public string mappedValue;
        }

        public sealed class IgnoredFieldAbsentComponent : GameFrameworkComponent
        {
            public string mappedValue;
        }

        private sealed class IgnoredFieldPresentSetting : ComponentSetting
        {
            [ComponentSettingMappingIgnore]
            public string ignoredValue;

            public string mappedValue;
        }

        public sealed class IgnoredFieldPresentComponent : GameFrameworkComponent
        {
            public const string DefaultIgnoredValue = "component-default";

            public string ignoredValue = DefaultIgnoredValue;

            public string mappedValue;
        }

        private sealed class UnmarkedFieldAbsentSetting : ComponentSetting
        {
            public string mappedValue;

            public string unmappedValue;
        }

        public sealed class UnmarkedFieldAbsentComponent : GameFrameworkComponent
        {
            public string mappedValue;
        }

        private sealed class WidenedLookupSetting : ComponentSetting
        {
            public string derivedPublicValue;

            public string basePrivateValue;
        }

        public class WidenedLookupComponentBase : GameFrameworkComponent
        {
            private string basePrivateValue = string.Empty;

            public string BasePrivateValue => basePrivateValue;
        }

        public sealed class WidenedLookupComponent : WidenedLookupComponentBase
        {
            public string derivedPublicValue;

            public string DerivedPublicValue => derivedPublicValue;
        }
    }
}
