using System;
using System.Reflection;
using LFramework.Runtime.Settings;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.BuildPackage
{
    public class AddressableBuildSystemTests
    {
        [Test]
        public void ValidateNoAotDllChangedForContentUpdate_ThrowsWhenAotAssetChanged()
        {
            Type helperType = Type.GetType(
                "LFramework.Editor.Builder.BuildingResource.AddressableBuildHelper, LFramework.Editor");
            Assert.That(helperType, Is.Not.Null, "AddressableBuildHelper type is missing.");

            MethodInfo method = helperType.GetMethod(
                "ValidateNoAotDllChangedForContentUpdate",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected AddressableBuildHelper to protect content updates from AOT changes.");

            var setting = ScriptableObject.CreateInstance<HybridCLRSetting>();
            setting.aotDllFolderPath = "MagicWarrior/_Resources/Dll/Aot";

            var ex = Assert.Throws<TargetInvocationException>(() =>
                method.Invoke(null, new object[]
                {
                    new[] { "Assets/MagicWarrior/_Resources/Dll/Aot/System.Core.dll.bytes" },
                    setting
                }));

            Assert.That(ex.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(ex.InnerException.Message, Does.Contain("本次包含 AOT 变化，需要重新出包或恢复首包 AOT"));
        }
    }
}
