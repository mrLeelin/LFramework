using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LFramework.Editor.Builder;
using LFramework.Editor.Builder.PlatformConfig;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage.PlatformConfig
{
    /// <summary>
    /// 验证各平台从工程配置读取完整构建选项，不执行实际打包。
    /// </summary>
    public class PlayerBuildSettingTests
    {
        private static readonly FieldInfo SelectorField = typeof(SettingManager).GetField(
            "_projectSelector", BindingFlags.Static | BindingFlags.NonPublic);

        private ProjectSettingSelector _originalSelector;
        private ProjectSettingSelector _selector;
        private PlayerBuildSetting _setting;
        private AndroidSetting _androidSetting;
        private iOSSetting _iosSetting;

        [SetUp]
        public void SetUp()
        {
            Assert.That(SelectorField, Is.Not.Null);
            _originalSelector = (ProjectSettingSelector)SelectorField.GetValue(null);
            _selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            _setting = ScriptableObject.CreateInstance<PlayerBuildSetting>();
            _androidSetting = ScriptableObject.CreateInstance<AndroidSetting>();
            _iosSetting = ScriptableObject.CreateInstance<iOSSetting>();
            _selector.SetSetting(_setting);
            _selector.SetSetting(_androidSetting);
            _selector.SetSetting(_iosSetting);
            SelectorField.SetValue(null, _selector);
            foreach (BuilderTarget target in Enum.GetValues(typeof(BuilderTarget)))
                SetOptions(target, BuildOptions.None);
        }

        [TearDown]
        public void TearDown()
        {
            SelectorField.SetValue(null, _originalSelector);
            UnityEngine.Object.DestroyImmediate(_selector);
            UnityEngine.Object.DestroyImmediate(_setting);
            UnityEngine.Object.DestroyImmediate(_androidSetting);
            UnityEngine.Object.DestroyImmediate(_iosSetting);
        }

        [TestCase(BuilderTarget.Android, false)]
        [TestCase(BuilderTarget.Android, true)]
        [TestCase(BuilderTarget.iOS, false)]
        [TestCase(BuilderTarget.iOS, true)]
        [TestCase(BuilderTarget.Windows, false)]
        [TestCase(BuilderTarget.Windows, true)]
        public void NewConfiguration_DefaultsToDevelopmentAndDebuggingOnlyForDev(BuilderTarget target, bool isRelease)
        {
            var fresh = ScriptableObject.CreateInstance<PlayerBuildSetting>();
            try
            {
                _selector.SetSetting(fresh);
                var expected = isRelease ? BuildOptions.None : BuildOptions.Development | BuildOptions.AllowDebugging;
                Assert.That(GetOptions(target, isRelease), Is.EqualTo(expected));
            }
            finally
            {
                _selector.SetSetting(_setting);
                UnityEngine.Object.DestroyImmediate(fresh);
            }
        }

        [TestCase(BuilderTarget.Android, false)]
        [TestCase(BuilderTarget.Android, true)]
        [TestCase(BuilderTarget.iOS, false)]
        [TestCase(BuilderTarget.iOS, true)]
        [TestCase(BuilderTarget.Windows, false)]
        [TestCase(BuilderTarget.Windows, true)]
        public void MultipleOptions_AffectOnlySelectedPlatformAndMode(BuilderTarget target, bool isRelease)
        {
            const BuildOptions selected = BuildOptions.StrictMode | BuildOptions.CompressWithLz4 |
                                          BuildOptions.Development | BuildOptions.ConnectWithProfiler;
            SetOptions(target, selected, isRelease);

            foreach (BuilderTarget candidate in Enum.GetValues(typeof(BuilderTarget)))
            {
                foreach (bool candidateRelease in new[] { false, true })
                {
                    BuildOptions expected = candidate == target && candidateRelease == isRelease
                        ? selected : BuildOptions.None;
                    Assert.That(GetOptions(candidate, candidateRelease), Is.EqualTo(expected),
                        $"{candidate}, Release={candidateRelease}");
                }
            }
        }

        [TestCase(BuilderTarget.Android, BuildOptions.EnableDeepProfilingSupport)]
        [TestCase(BuilderTarget.Android, BuildOptions.AllowDebugging)]
        [TestCase(BuilderTarget.Android, BuildOptions.ConnectWithProfiler)]
        [TestCase(BuilderTarget.Android, BuildOptions.WaitForPlayerConnection)]
        [TestCase(BuilderTarget.iOS, BuildOptions.EnableDeepProfilingSupport)]
        [TestCase(BuilderTarget.iOS, BuildOptions.AllowDebugging)]
        [TestCase(BuilderTarget.iOS, BuildOptions.ConnectWithProfiler)]
        [TestCase(BuilderTarget.iOS, BuildOptions.WaitForPlayerConnection)]
        [TestCase(BuilderTarget.Windows, BuildOptions.EnableDeepProfilingSupport)]
        [TestCase(BuilderTarget.Windows, BuildOptions.AllowDebugging)]
        [TestCase(BuilderTarget.Windows, BuildOptions.ConnectWithProfiler)]
        [TestCase(BuilderTarget.Windows, BuildOptions.WaitForPlayerConnection)]
        public void SelectedOption_DoesNotImplicitlyEnableDevelopment(BuilderTarget target, BuildOptions option)
        {
            foreach (bool isRelease in new[] { false, true })
            {
                SetOptions(target, option, isRelease);
                Assert.That(GetOptions(target, isRelease), Is.EqualTo(option));
                Assert.That(GetOptions(target, !isRelease), Is.EqualTo(BuildOptions.None));
                PlayerBuildSetting.BuildOptionsConfig config = isRelease
                    ? _setting.GetReleaseConfig(ToRuntimePlatform(target))
                    : _setting.GetConfig(ToRuntimePlatform(target));
                Assert.That(config.OptionsValue, Is.EqualTo((int)option),
                    "解析构建选项不应修改保存的配置。");
                SetOptions(target, BuildOptions.None, isRelease);
            }
        }

        [TestCase(BuilderTarget.Android)]
        [TestCase(BuilderTarget.iOS)]
        [TestCase(BuilderTarget.Windows)]
        public void None_RemovesPreviouslyEnabledAndLegacyDebugOptions(BuilderTarget target)
        {
            foreach (bool isRelease in new[] { false, true })
            {
                SetOptions(target, BuildOptions.Development | BuildOptions.EnableDeepProfilingSupport |
                                   BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler |
                                   BuildOptions.WaitForPlayerConnection, isRelease);
                Assert.That(GetOptions(target, isRelease), Is.Not.EqualTo(BuildOptions.None));
                SetOptions(target, BuildOptions.None, isRelease);
                Assert.That(GetOptions(target, isRelease), Is.EqualTo(BuildOptions.None));
            }
        }

        [TestCase(BuilderTarget.Android, false)]
        [TestCase(BuilderTarget.Android, true)]
        [TestCase(BuilderTarget.iOS, false)]
        [TestCase(BuilderTarget.iOS, true)]
        [TestCase(BuilderTarget.Windows, false)]
        [TestCase(BuilderTarget.Windows, true)]
        public void ConflictingCompressionOptions_FailExplicitly(BuilderTarget target, bool isRelease)
        {
            SetOptions(target, BuildOptions.CompressWithLz4 | BuildOptions.CompressWithLz4HC, isRelease);

            Assert.Throws<InvalidOperationException>(() => GetOptions(target, isRelease));
            Assert.That(GetOptions(target, !isRelease), Is.EqualTo(BuildOptions.None));
        }

        [TestCase(BuildAndroidAppType.APK, false)]
        [TestCase(BuildAndroidAppType.APK, true)]
        [TestCase(BuildAndroidAppType.AppBundle, false)]
        [TestCase(BuildAndroidAppType.AppBundle, true)]
        public void AndroidNonExport_RejectsExternalModificationOption(BuildAndroidAppType appType, bool isRelease)
        {
            SetOptions(BuilderTarget.Android, BuildOptions.AcceptExternalModificationsToPlayer, isRelease);

            Assert.Throws<InvalidOperationException>(() => GetOptions(BuilderTarget.Android, isRelease, appType));
            Assert.That(GetOptions(BuilderTarget.Android, !isRelease, appType), Is.EqualTo(BuildOptions.None));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void AndroidExport_AlwaysPreservesExportOption(bool isRelease)
        {
            Assert.That(GetOptions(BuilderTarget.Android, isRelease, BuildAndroidAppType.ExportAndroidProject),
                Is.EqualTo(BuildOptions.AcceptExternalModificationsToPlayer));
            SetOptions(BuilderTarget.Android, BuildOptions.EnableDeepProfilingSupport | BuildOptions.StrictMode, isRelease);

            BuildOptions expected = BuildOptions.AcceptExternalModificationsToPlayer |
                                    BuildOptions.EnableDeepProfilingSupport | BuildOptions.StrictMode;
            Assert.That(GetOptions(BuilderTarget.Android, isRelease, BuildAndroidAppType.ExportAndroidProject),
                Is.EqualTo(expected));
        }

        [TestCase(BuilderTarget.Android, false)]
        [TestCase(BuilderTarget.Android, true)]
        [TestCase(BuilderTarget.iOS, false)]
        [TestCase(BuilderTarget.iOS, true)]
        [TestCase(BuilderTarget.Windows, false)]
        [TestCase(BuilderTarget.Windows, true)]
        public void MissingConfiguration_FailsInBothModes(BuilderTarget target, bool isRelease)
        {
            var serializedSelector = new SerializedObject(_selector);
            serializedSelector.FindProperty("selectedSettings").ClearArray();
            serializedSelector.ApplyModifiedPropertiesWithoutUndo();
            _selector.SetSetting(_androidSetting);
            _selector.SetSetting(_iosSetting);

            var exception = Assert.Throws<InvalidOperationException>(() => GetOptions(target, isRelease));

            Assert.That(exception.Message, Does.Contain("PlayerBuildSetting"));
            Assert.That(exception.Message, Does.Contain("ProjectSettingSelector"));
            if (target == BuilderTarget.Android)
            {
                Assert.Throws<InvalidOperationException>(() =>
                    GetOptions(target, isRelease, BuildAndroidAppType.ExportAndroidProject));
            }
        }

        [Test]
        public void UnknownRuntimePlatform_FailsExplicitly()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _setting.GetConfig(RuntimePlatform.LinuxPlayer));
            Assert.Throws<ArgumentOutOfRangeException>(() => _setting.GetReleaseConfig(RuntimePlatform.LinuxPlayer));
        }

        [Test]
        public void SerializedConfiguration_PreservesAllSixIndependentOptionSets()
        {
            var expected = new Dictionary<BuilderTarget, BuildOptions[]>
            {
                { BuilderTarget.Android, new[]
                    {
                        BuildOptions.StrictMode | BuildOptions.CompressWithLz4,
                        BuildOptions.StrictMode | BuildOptions.CompressWithLz4HC
                    }
                },
                { BuilderTarget.iOS, new[]
                    {
                        BuildOptions.AllowDebugging | BuildOptions.ConnectWithProfiler,
                        BuildOptions.EnableDeepProfilingSupport | BuildOptions.Development
                    }
                },
                { BuilderTarget.Windows, new[]
                    {
                        BuildOptions.WaitForPlayerConnection | BuildOptions.Development,
                        BuildOptions.EnableDeepProfilingSupport | BuildOptions.AllowDebugging
                    }
                }
            };
            foreach (var pair in expected)
            {
                SetOptions(pair.Key, pair.Value[0]);
                SetOptions(pair.Key, pair.Value[1], true);
            }
            var restored = ScriptableObject.CreateInstance<PlayerBuildSetting>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(_setting), restored);
                _selector.SetSetting(restored);

                foreach (var pair in expected)
                {
                    Assert.That(GetOptions(pair.Key, false), Is.EqualTo(pair.Value[0]), $"{pair.Key}, Dev");
                    Assert.That(GetOptions(pair.Key, true), Is.EqualTo(pair.Value[1]), $"{pair.Key}, Release");
                }
            }
            finally
            {
                _selector.SetSetting(_setting);
                UnityEngine.Object.DestroyImmediate(restored);
            }
        }

        private void SetOptions(BuilderTarget target, BuildOptions options, bool isRelease = false)
        {
            string platform = target switch
            {
                BuilderTarget.Android => "android",
                BuilderTarget.iOS => "ios",
                BuilderTarget.Windows => "windows",
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
            if (isRelease)
            {
                platform += "Release";
            }
            var serializedSetting = new SerializedObject(_setting);
            serializedSetting.FindProperty($"{platform}.buildOptions").intValue = (int)options;
            serializedSetting.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RuntimePlatform ToRuntimePlatform(BuilderTarget target)
        {
            return target switch
            {
                BuilderTarget.Android => RuntimePlatform.Android,
                BuilderTarget.iOS => RuntimePlatform.IPhonePlayer,
                BuilderTarget.Windows => RuntimePlatform.WindowsPlayer,
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
        }

        private static BuildOptions GetOptions(BuilderTarget target, bool isRelease,
            BuildAndroidAppType androidAppType = BuildAndroidAppType.APK)
        {
            var buildSetting = new BuildSetting
            {
                builderTarget = target,
                isRelease = isRelease,
                buildAndroidAppType = androidAppType,
                appVersion = "1.0.0",
                versionCode = 1
            };
            IPlatformConfig config = target switch
            {
                BuilderTarget.Android => new AndroidPlatformConfig(buildSetting),
                BuilderTarget.iOS => new iOSPlatformConfig(buildSetting),
                BuilderTarget.Windows => new WindowsPlatformConfig(buildSetting),
                _ => throw new ArgumentOutOfRangeException(nameof(target))
            };
            return config.GetBuildPlayerOptions(buildSetting).options;
        }
    }

    /// <summary>
    /// 验证构建作用域同步 Development 状态，并在退出后恢复用户原设置。
    /// </summary>
    public class PlayerDevelopmentBuildScopeTests
    {
        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Dispose_RestoresPreviousStateAfterSuccess(bool previous, bool requested)
        {
            VerifyScope(previous, requested, false);
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Dispose_RestoresPreviousStateAfterException(bool previous, bool requested)
        {
            VerifyScope(previous, requested, true);
        }

        private static void VerifyScope(bool previous, bool requested, bool throwException)
        {
            bool originalUserSetting = EditorUserBuildSettings.development;
            try
            {
                EditorUserBuildSettings.development = previous;
                BuildOptions options = requested ? BuildOptions.Development : BuildOptions.None;
                if (throwException)
                {
                    Assert.Throws<InvalidOperationException>(() =>
                    {
                        using (new PlayerDevelopmentBuildScope(options))
                        {
                            Assert.That(EditorUserBuildSettings.development, Is.EqualTo(requested));
                            throw new InvalidOperationException("Simulated build failure.");
                        }
                    });
                }
                else
                {
                    using (new PlayerDevelopmentBuildScope(options))
                    {
                        Assert.That(EditorUserBuildSettings.development, Is.EqualTo(requested));
                    }
                }

                Assert.That(EditorUserBuildSettings.development, Is.EqualTo(previous));
            }
            finally
            {
                EditorUserBuildSettings.development = originalUserSetting;
            }
        }
    }

    /// <summary>
    /// 只读验证当前工程配置与模板注册表、同步记录之间的完整性。
    /// 不初始化、不删除、不保存任何工程资产。
    /// </summary>
    public class ProjectSettingCompletenessTests
    {
        [Test]
        public void MissingRequiredTemplate_FailsWithSettingId()
        {
            var registry = ScriptableObject.CreateInstance<SettingTemplateRegistry>();
            try
            {
                registry.SetEntries(new[]
                {
                    new SettingTemplateEntry
                    {
                        settingId = "Missing.Required.Setting",
                        isRequired = true,
                        templateAsset = null
                    }
                });

                var exception = Assert.Throws<InvalidOperationException>(() =>
                    SettingProjectInitializer.LoadTemplateAssets(registry));

                Assert.That(exception.Message, Does.Contain("Missing.Required.Setting"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(registry);
            }
        }

        [Test]
        public void MissingOptionalTemplate_IsIgnored()
        {
            var registry = ScriptableObject.CreateInstance<SettingTemplateRegistry>();
            var validTemplate = ScriptableObject.CreateInstance<PlayerBuildSetting>();
            try
            {
                registry.SetEntries(new[]
                {
                    new SettingTemplateEntry
                    {
                        settingId = "Missing.Optional.Setting",
                        isRequired = false,
                        templateAsset = null
                    },
                    new SettingTemplateEntry
                    {
                        settingId = validTemplate.SettingId,
                        isRequired = true,
                        templateAsset = validTemplate
                    }
                });

                var templates = SettingProjectInitializer.LoadTemplateAssets(registry);

                Assert.That(templates.Count, Is.EqualTo(1));
                Assert.That(templates[0], Is.SameAs(validTemplate));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(registry);
                UnityEngine.Object.DestroyImmediate(validTemplate);
            }
        }

        [Test]
        public void RegisteredTemplates_HaveMatchingIndependentProjectAssetsAndSyncRecords()
        {
            var registry = SettingProjectInitializer.FindTemplateRegistry();
            var selector = AssetDatabase.LoadAssetAtPath<ProjectSettingSelector>(SettingProjectPaths.SelectorAssetPath);
            var sync = AssetDatabase.LoadAssetAtPath<SettingSyncState>(SettingProjectPaths.SyncStateAssetPath);
            Assert.That(registry, Is.Not.Null);
            Assert.That(selector, Is.Not.Null);
            Assert.That(sync, Is.Not.Null);
            Assert.That(registry.Entries.Count, Is.GreaterThan(0));

            var seenIds = new HashSet<string>();
            var localAssets = selector.GetAllSettings().Cast<ScriptableObject>()
                .Concat(selector.GetAllComponentSettings()).ToList();
            foreach (SettingTemplateEntry entry in registry.Entries)
            {
                Assert.That(entry, Is.Not.Null);
                Assert.That(entry.templateAsset, Is.Not.Null, entry.settingId);
                Assert.That(seenIds.Add(entry.settingId), Is.True, $"重复模板标识：{entry.settingId}");
                Assert.That(GetSettingId(entry.templateAsset), Is.EqualTo(entry.settingId));
                Assert.That(Type.GetType(entry.settingTypeName), Is.EqualTo(entry.templateAsset.GetType()), entry.settingId);
                var matchingAssets = localAssets.Where(asset => GetSettingId(asset) == entry.settingId).ToList();
                Assert.That(matchingAssets.Count, Is.EqualTo(1), entry.settingId);
                ScriptableObject local = matchingAssets[0];
                Assert.That(local.GetType(), Is.EqualTo(entry.templateAsset.GetType()), entry.settingId);
                Assert.That(local, Is.Not.SameAs(entry.templateAsset), entry.settingId);
                Assert.That(AssetDatabase.GetAssetPath(local), Does.StartWith(SettingProjectPaths.Root + "/"));
                Assert.That(sync.Records.Count(record => record != null && record.settingId == entry.settingId),
                    Is.EqualTo(1), entry.settingId);
                SettingSyncRecord record = sync.GetRecord(entry.settingId);
                Assert.That(record.localAsset, Is.SameAs(local), entry.settingId);
                Assert.That(Type.GetType(record.settingTypeName), Is.EqualTo(local.GetType()), entry.settingId);
                Assert.That(record.lastTemplateVersion, Is.EqualTo(entry.templateVersion), entry.settingId);
            }
        }

        private static string GetSettingId(ScriptableObject asset)
        {
            return asset switch
            {
                BaseSetting setting => setting.SettingId,
                ComponentSetting component => component.SettingId,
                _ => throw new InvalidOperationException($"Unsupported setting type: {asset.GetType().FullName}")
            };
        }
    }
}
