using System.Collections.Generic;
using System.Reflection;
using LFramework.Editor.Settings;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace LFramework.Tests.Editor.Settings
{
    public sealed class NetworkComponentSettingTests
    {
        [Test]
        public void CovertToComponent_AppliesNetworkTimeouts()
        {
            var setting = ScriptableObject.CreateInstance<NetworkComponentSetting>();
            setting.bindTypeName = typeof(NetworkComponent).FullName;
            SetPrivateField(setting, "_loginTimeOutSecond", 7);
            SetPrivateField(setting, "_timeOutSecond", 13);

            var component = InvokeConvertToComponent(setting);

            Assert.That(GetPrivateField<int>(component, "_loginTimeOutSecond"), Is.EqualTo(7));
            Assert.That(GetPrivateField<int>(component, "_timeOutSecond"), Is.EqualTo(13));
        }

        [Test]
        public void AwakeComponent_CreatesConfiguredNetworkChannel()
        {
            var setting = ScriptableObject.CreateInstance<NetworkComponentSetting>();
            setting.bindTypeName = typeof(NetworkComponent).FullName;
            SetPrivateField(setting, "_networkChannelTypeName", typeof(TestNetworkChannel).FullName);

            var parent = new GameObject("NetworkComponentSettingTests");
            try
            {
                var component = InvokeConvertToComponent(setting);
                component.Parent = parent;
                component.AwakeComponent();

                Assert.That(GetPrivateField<INetworkChannel>(component, "_networkChannel"), Is.TypeOf<TestNetworkChannel>());
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void AwakeComponent_IgnoresInvalidConfiguredNetworkChannel()
        {
            var setting = ScriptableObject.CreateInstance<NetworkComponentSetting>();
            setting.bindTypeName = typeof(NetworkComponent).FullName;
            SetPrivateField(setting, "_networkChannelTypeName", typeof(AbstractTestNetworkChannel).FullName);

            var parent = new GameObject("NetworkComponentSettingTests");
            try
            {
                var component = InvokeConvertToComponent(setting);
                component.Parent = parent;

                Assert.DoesNotThrow(() => component.AwakeComponent());
                Assert.That(GetPrivateField<INetworkChannel>(component, "_networkChannel"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ShutDown_DestroysConfiguredNetworkChannel()
        {
            var setting = ScriptableObject.CreateInstance<NetworkComponentSetting>();
            setting.bindTypeName = typeof(NetworkComponent).FullName;
            SetPrivateField(setting, "_networkChannelTypeName", typeof(TestNetworkChannel).FullName);

            var parent = new GameObject("NetworkComponentSettingTests");
            try
            {
                var component = InvokeConvertToComponent(setting);
                component.Parent = parent;
                component.AwakeComponent();
                var channel = GetPrivateField<INetworkChannel>(component, "_networkChannel") as NetworkChannelBase;

                component.ShutDown();

                Assert.That(channel == null, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void Inspector_DoesNotOfferMissingConfiguredNetworkChannel()
        {
            var setting = ScriptableObject.CreateInstance<NetworkComponentSetting>();
            SetPrivateField(setting, "_networkChannelTypeName", "MagicWarrior.Runtime.HttpChannel");

            var inspectorType = typeof(SettingProjectPaths).Assembly.GetType(
                "LFramework.Editor.Inspector.NetworkComponentInspector");
            var editor = UnityEditor.Editor.CreateEditor(setting, inspectorType);
            try
            {
                InvokePrivate(editor, "RefreshTypeNames");

                Assert.That(GetPrivateField<List<string>>(editor, "_networkChannelTypeNames"),
                    Does.Not.Contain("MagicWarrior.Runtime.HttpChannel"));
                Assert.That(GetPrivateField<int>(editor, "_networkChannelTypeNameIndex"), Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(setting);
            }
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            return (T)target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
        }

        private static NetworkComponent InvokeConvertToComponent(ComponentSetting setting)
        {
            var method = typeof(ComponentSettingExtension).GetMethod(
                "CovertToComponent",
                BindingFlags.Static | BindingFlags.NonPublic);

            return (NetworkComponent)method.Invoke(null, new object[] { setting });
        }

        private sealed class TestNetworkChannel : NetworkChannelBase
        {
            public override void Initialize()
            {
            }

            public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            public override void Connect(string url)
            {
            }

            public override void SetTimeOut(int loginTimeOut, int timeOut)
            {
            }

            public override void Clear()
            {
            }

            public override void Post(
                NetworkRequestParam requestParam,
                NetworkMessageCallBack callBack,
                NetworkMessageSuccessfulCallBack successfulCallBack,
                NetworkMessageErrorCallBack errorCallBack,
                NetworkRequestFailedCallBack requestFailedCallBack)
            {
            }
        }

        private abstract class AbstractTestNetworkChannel : NetworkChannelBase
        {
        }
    }
}
