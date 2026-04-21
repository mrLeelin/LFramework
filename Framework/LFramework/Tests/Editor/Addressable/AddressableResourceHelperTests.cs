using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.ResourceComponent
{
    public class ResourceAssetTypeUtilityTests
    {
        [Test]
        public void TryConvertLoadedObject_ReturnsTypedAsset_WhenObjectMatchesRequestedType()
        {
            var asset = ScriptableObject.CreateInstance<TestScriptableObject>();

            object[] arguments =
            {
                asset,
                typeof(TestScriptableObject),
                "Assets/Test.asset",
                null,
                null
            };

            bool result = (bool)GetHelperMethod("TryConvertLoadedObject").Invoke(null, arguments);

            Assert.That(result, Is.True);
            Assert.That(arguments[3], Is.SameAs(asset));
            Assert.That(arguments[4], Is.Null);
        }

        [Test]
        public void TryConvertLoadedObject_ReturnsFalse_WhenObjectCannotBeAssigned()
        {
            var asset = ScriptableObject.CreateInstance<AnotherScriptableObject>();

            object[] arguments =
            {
                asset,
                typeof(TestScriptableObject),
                "Assets/Test.asset",
                null,
                null
            };

            bool result = (bool)GetHelperMethod("TryConvertLoadedObject").Invoke(null, arguments);

            Assert.That(result, Is.False);
            Assert.That(arguments[3], Is.Null);
            Assert.That(arguments[4], Is.EqualTo(
                "Load asset 'Assets/Test.asset' returned type 'LFramework.Editor.Tests.ResourceComponent.ResourceAssetTypeUtilityTests+AnotherScriptableObject', which cannot be assigned to 'LFramework.Editor.Tests.ResourceComponent.ResourceAssetTypeUtilityTests+TestScriptableObject'."));
        }

        [Test]
        public void GetLoadTypeFallbackChain_ReturnsRequestedTypeThenObject_WhenSpecificTypeRequested()
        {
            object[] arguments = { typeof(TestScriptableObject) };

            var result = (Type[])GetHelperMethod("GetLoadTypeFallbackChain").Invoke(null, arguments);

            Assert.That(result, Is.EqualTo(new[] { typeof(TestScriptableObject), typeof(object) }));
        }

        [Test]
        public void GetLoadTypeFallbackChain_ReturnsOnlyObject_WhenRequestedTypeIsNull()
        {
            object[] arguments = { null };

            var result = (Type[])GetHelperMethod("GetLoadTypeFallbackChain").Invoke(null, arguments);

            Assert.That(result, Is.EqualTo(new[] { typeof(object) }));
        }

        [Test]
        public void GetLoadTypeFallbackChain_ReturnsOnlyObject_WhenRequestedTypeIsObject()
        {
            object[] arguments = { typeof(object) };

            var result = (Type[])GetHelperMethod("GetLoadTypeFallbackChain").Invoke(null, arguments);

            Assert.That(result, Is.EqualTo(new[] { typeof(object) }));
        }

        private static MethodInfo GetHelperMethod(string methodName)
        {
            Type helperType = Type.GetType(
                "LFramework.Runtime.ResourceAssetTypeUtility, LFramework.Runtime");

            Assert.That(helperType, Is.Not.Null, "Expected ResourceAssetTypeUtility type to exist in LFramework.Runtime.");

            MethodInfo method = helperType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Expected ResourceAssetTypeUtility.{methodName} to exist.");
            return method;
        }

        private sealed class TestScriptableObject : ScriptableObject
        {
        }

        private sealed class AnotherScriptableObject : ScriptableObject
        {
        }
    }
}
