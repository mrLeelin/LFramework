using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework.Resource;
using NUnit.Framework;
using UnityEngine;

namespace LFramework.Editor.Tests.ResourceComponent
{
    public class AddressableResourceHelperTests
    {
        [Test]
        public void ContainsPrimaryKeyMatch_ReturnsTrue_OnlyForExactPrimaryKey()
        {
            MethodInfo method = GetAddressableHelperMethod("ContainsPrimaryKeyMatch");
            var locations = new List<object>
            {
                new TestResourceLocation("shared_label"),
                new TestResourceLocation("ui/home")
            };

            bool exactMatch = (bool)method.Invoke(null, new object[] { locations, "ui/home" });
            bool missingMatch = (bool)method.Invoke(null, new object[] { locations, "ui/missing" });
            bool labelOnlyMatch = (bool)method.Invoke(null, new object[] { locations, "shared_label_2" });

            Assert.That(exactMatch, Is.True);
            Assert.That(missingMatch, Is.False);
            Assert.That(labelOnlyMatch, Is.False);
        }

        [Test]
        public void ResolveCatalogAssetResult_ReturnsExist_WhenCatalogLocationExists()
        {
            MethodInfo method = GetAddressableHelperMethod("ResolveCatalogAssetResult");

            object result = method.Invoke(null, new object[] { true, false });

            Assert.That(result, Is.EqualTo(HasAssetResult.Exist));
        }

        [Test]
        public void ResolveCatalogAssetResult_DistinguishesMissingCatalogFromUninitializedCatalog()
        {
            MethodInfo method = GetAddressableHelperMethod("ResolveCatalogAssetResult");

            object missingAfterCatalogReady = method.Invoke(null, new object[] { false, true });
            object catalogNotReady = method.Invoke(null, new object[] { false, false });

            Assert.That(missingAfterCatalogReady, Is.EqualTo(HasAssetResult.NotExist));
            Assert.That(catalogNotReady, Is.EqualTo(HasAssetResult.NotReady));
        }

        private static MethodInfo GetAddressableHelperMethod(string methodName)
        {
            Type helperType = Type.GetType(
                "LFramework.Runtime.AddressableResourceHelper, LFramework.Runtime");

            Assert.That(helperType, Is.Not.Null, "Expected AddressableResourceHelper type to exist in LFramework.Runtime.");

            MethodInfo method = helperType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, $"Expected AddressableResourceHelper.{methodName} to exist.");
            return method;
        }

        private sealed class TestResourceLocation
        {
            public TestResourceLocation(string primaryKey)
            {
                PrimaryKey = primaryKey;
            }

            public string PrimaryKey { get; }
        }
    }

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
