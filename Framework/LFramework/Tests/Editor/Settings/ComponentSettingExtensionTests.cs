using System.Reflection;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Settings
{
    /// <summary>
    /// 验证组件设置元数据与运行参数的映射边界。
    /// </summary>
    public sealed class ComponentSettingExtensionTests
    {
        /// <summary>
        /// 空标识和自定义标识均不应作为组件字段映射。
        /// </summary>
        [TestCase("")]
        [TestCase("tests.setting")]
        public void CovertToComponent_IgnoresMetadataAndCopiesComponentFields(string settingId)
        {
            var setting = ScriptableObject.CreateInstance<SettingComponentSetting>();
            try
            {
                setting.bindTypeName = typeof(SettingComponent).FullName;
                setting.EditorSetSettingId(settingId);
                const string helperTypeName = "Tests.CustomSettingHelper";
                typeof(SettingComponentSetting).GetField("m_SettingHelperTypeName",
                    BindingFlags.Instance | BindingFlags.NonPublic).SetValue(setting, helperTypeName);

                var convert = typeof(ComponentSettingExtension).GetMethod("CovertToComponent",
                    BindingFlags.Static | BindingFlags.NonPublic);
                var component = convert.Invoke(null, new object[] { setting });

                Assert.That(component, Is.TypeOf<SettingComponent>());
                Assert.That(typeof(SettingComponent).GetField("m_SettingHelperTypeName",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component), Is.EqualTo(helperTypeName));
                LogAssert.NoUnexpectedReceived();
            }
            finally
            {
                Object.DestroyImmediate(setting);
            }
        }
    }
}
