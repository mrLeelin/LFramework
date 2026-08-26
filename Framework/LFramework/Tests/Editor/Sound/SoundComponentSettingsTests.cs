using System;
using System.Reflection;
using GameFramework;
using GameFramework.Sound;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Sound
{
    public sealed class SoundComponentSettingsTests
    {
        private const string TemplateAssetPath =
            "Assets/Frame/Framework/LFramework/Assets/Settings/ComponentSettings/SoundComponentSetting.asset";

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
        public void CovertToComponent_AppliesSoundCacheSettings()
        {
            SoundComponentSetting setting = CreateSetting();
            try
            {
                SetPublicField(setting, "m_SoundAssetIdleReleaseSeconds", 3.5f);
                SetPublicField(setting, "m_MaxIdleSoundAssetCount", 17);
                SetPublicField(setting, "m_SoundQueueWarningThreshold", 23);
                SetPublicField(setting, "m_SoundQueueWarningResetThreshold", 11);

                SoundComponent component = InvokeConvertToComponent(setting);

                Assert.That(GetPrivateField<float>(component, "m_SoundAssetIdleReleaseSeconds"), Is.EqualTo(3.5f));
                Assert.That(GetPrivateField<int>(component, "m_MaxIdleSoundAssetCount"), Is.EqualTo(17));
                Assert.That(GetPrivateField<int>(component, "m_SoundQueueWarningThreshold"), Is.EqualTo(23));
                Assert.That(GetPrivateField<int>(component, "m_SoundQueueWarningResetThreshold"), Is.EqualTo(11));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void AwakeComponent_ConfiguresSoundManagerFromSetting()
        {
            SoundComponentSetting setting = CreateSetting();
            GameObject parent = new GameObject("SoundComponentSettingsTests");
            try
            {
                SetPublicField(setting, "m_SoundAssetIdleReleaseSeconds", 4.5f);
                SetPublicField(setting, "m_MaxIdleSoundAssetCount", 19);
                SetPublicField(setting, "m_SoundQueueWarningThreshold", 29);
                SetPublicField(setting, "m_SoundQueueWarningResetThreshold", 13);
                SoundComponent component = InvokeConvertToComponent(setting);
                component.Parent = parent;

                component.AwakeComponent();

                object manager = GetPrivateField<object>(component, "m_SoundManager");
                Assert.That(GetPrivateField<float>(manager, "m_IdleReleaseSeconds"), Is.EqualTo(4.5f));
                Assert.That(GetPrivateField<int>(manager, "m_MaxIdleEntryCount"), Is.EqualTo(19));
                Assert.That(GetPrivateField<int>(manager, "m_QueueWarningThreshold"), Is.EqualTo(29));
                Assert.That(GetPrivateField<int>(manager, "m_QueueWarningResetThreshold"), Is.EqualTo(13));

                component.ShutDown();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void Template_QueuesOnlyUISoundGroup()
        {
            SoundComponentSetting setting = AssetDatabase.LoadAssetAtPath<SoundComponentSetting>(TemplateAssetPath);

            Assert.That(setting, Is.Not.Null, $"Expected sound component template at '{TemplateAssetPath}'.");
            Assert.That(GetOverflowPolicy(GetGroup(setting, "Music")),
                Is.EqualTo(SoundOverflowPolicy.ReplaceByPriority));
            Assert.That(GetOverflowPolicy(GetGroup(setting, "Sound")),
                Is.EqualTo(SoundOverflowPolicy.ReplaceByPriority));
            Assert.That(GetOverflowPolicy(GetGroup(setting, "UISound")),
                Is.EqualTo(SoundOverflowPolicy.QueueNonLooping));
        }

        [Test]
        public void Inspector_BindsAllSoundCacheSettings()
        {
            SoundComponentSetting setting = CreateSetting();
            Type inspectorType = typeof(SettingProjectPaths).Assembly.GetType(
                "LFramework.Editor.Inspector.SoundComponentInspector");
            UnityEditor.Editor editor = UnityEditor.Editor.CreateEditor(setting, inspectorType);
            try
            {
                Assert.That(GetPrivateField<SerializedProperty>(editor, "m_SoundAssetIdleReleaseSeconds"), Is.Not.Null);
                Assert.That(GetPrivateField<SerializedProperty>(editor, "m_MaxIdleSoundAssetCount"), Is.Not.Null);
                Assert.That(GetPrivateField<SerializedProperty>(editor, "m_SoundQueueWarningThreshold"), Is.Not.Null);
                Assert.That(GetPrivateField<SerializedProperty>(editor, "m_SoundQueueWarningResetThreshold"), Is.Not.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(editor);
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        [Test]
        public void ShutDown_CancelsPendingPlaybackBeforeUnsubscribing()
        {
            SoundComponentSetting setting = CreateSetting();
            GameObject parent = new GameObject("SoundComponentShutdownTests");
            try
            {
                SoundComponent component = InvokeConvertToComponent(setting);
                component.Parent = parent;
                component.AwakeComponent();

                ISoundManager manager = GetPrivateField<ISoundManager>(component, "m_SoundManager");
                manager.SetResourceManager(new FakeResourceManager());
                manager.SetSoundHelper(new FakeSoundHelper());
                Assert.That(manager.AddSoundGroup("UISound", new FakeSoundGroupHelper()), Is.True);
                Assert.That(manager.AddSoundAgentHelper("UISound", new FakeSoundAgentHelper()), Is.True);
                int baselineReferences = GetUsingReferenceCount("PlaySoundInfo");
                int serialId = component.PlaySound("ui/pending", "UISound");
                Assert.That(manager.IsLoadingSound(serialId), Is.True);
                Assert.That(GetUsingReferenceCount("PlaySoundInfo"), Is.EqualTo(baselineReferences + 1));

                component.ShutDown();

                Assert.That(manager.IsLoadingSound(serialId), Is.False);
                Assert.That(GetUsingReferenceCount("PlaySoundInfo"), Is.EqualTo(baselineReferences));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(parent);
                UnityEngine.Object.DestroyImmediate(setting);
            }
        }

        private static SoundComponentSetting CreateSetting()
        {
            SoundComponentSetting setting = ScriptableObject.CreateInstance<SoundComponentSetting>();
            setting.bindTypeName = typeof(SoundComponent).FullName;
            setting.m_SoundGroups = Array.Empty<SoundComponent.SoundGroup>();
            return setting;
        }

        private static SoundComponent InvokeConvertToComponent(ComponentSetting setting)
        {
            MethodInfo method = typeof(ComponentSettingExtension).GetMethod(
                "CovertToComponent", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (SoundComponent)method.Invoke(null, new object[] { setting });
        }

        private static SoundComponent.SoundGroup GetGroup(SoundComponentSetting setting, string groupName)
        {
            Assert.That(setting.m_SoundGroups, Is.Not.Null);
            foreach (SoundComponent.SoundGroup group in setting.m_SoundGroups)
            {
                if (group.Name == groupName)
                {
                    return group;
                }
            }

            Assert.Fail($"Expected sound group '{groupName}'.");
            return null;
        }

        private static SoundOverflowPolicy GetOverflowPolicy(SoundComponent.SoundGroup group)
        {
            PropertyInfo property = typeof(SoundComponent.SoundGroup).GetProperty("OverflowPolicy");
            Assert.That(property, Is.Not.Null, "Expected SoundGroup.OverflowPolicy to be serialized and exposed.");
            return (SoundOverflowPolicy)property.GetValue(group);
        }

        private static int GetUsingReferenceCount(string typeName)
        {
            foreach (ReferencePoolInfo info in ReferencePool.GetAllReferencePoolInfos())
            {
                if (info.Type.Name == typeName)
                {
                    return info.UsingReferenceCount;
                }
            }

            return 0;
        }

        private static void SetPublicField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
            Assert.That(field, Is.Not.Null, $"Expected public field '{fieldName}' on '{target.GetType().Name}'.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field '{fieldName}' on '{target.GetType().Name}'.");
            return (T)field.GetValue(target);
        }
    }
}
