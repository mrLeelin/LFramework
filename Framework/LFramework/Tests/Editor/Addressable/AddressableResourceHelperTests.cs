using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework.Resource;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using YooAsset;

namespace LFramework.Editor.Tests.ResourceComponent
{
    public class YooAssetResourceHelperTests
    {
        [Test]
        public void AcquireHandles_ReleasesPreviouslyAcquiredHandles_WhenLaterLoadThrows()
        {
            Type helperType = Type.GetType(
                "LFramework.Runtime.YooAssetResourceHelper, LFramework.Runtime");
            Assert.That(helperType, Is.Not.Null);

            MethodInfo method = helperType.GetMethod(
                "AcquireHandles",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);

            method = method.MakeGenericMethod(typeof(int), typeof(TrackedHandle));
            var acquired = new TrackedHandle();
            var releaseCount = 0;
            Func<int, TrackedHandle> acquire = index => index == 1
                ? acquired
                : throw new InvalidOperationException("second load failed");
            Action<TrackedHandle> release = handle =>
            {
                Assert.That(handle, Is.SameAs(acquired));
                releaseCount++;
            };

            var exception = Assert.Throws<TargetInvocationException>(() => method.Invoke(
                null,
                new object[] { new[] { 1, 2 }, acquire, release }));

            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(releaseCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolveTaggedAssetInfos_UsesTagLookupAndNormalizesNullResults()
        {
            Type helperType = Type.GetType(
                "LFramework.Runtime.YooAssetResourceHelper, LFramework.Runtime");
            Assert.That(helperType, Is.Not.Null);

            MethodInfo method = helperType.GetMethod(
                "ResolveTaggedAssetInfos",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null,
                "Expected YooAssetResourceHelper.ResolveTaggedAssetInfos to own tag lookup.");

            string resolvedTag = null;
            Func<string, AssetInfo[]> resolver = tag =>
            {
                resolvedTag = tag;
                return null;
            };

            var result = (AssetInfo[])method.Invoke(null, new object[] { "code_dll", resolver });

            Assert.That(resolvedTag, Is.EqualTo("code_dll"));
            Assert.That(result, Is.Empty);
        }

        private sealed class TrackedHandle
        {
        }
    }

    public class AddressableResourceHelperTests
    {
        [Test]
        public void ContainsPrimaryKeyMatch_ReturnsTrue_OnlyForExactPrimaryKey()
        {
            MethodInfo method = GetAddressableHelperMethod("ContainsPrimaryKeyMatch");
            var locations = new List<IResourceLocation>
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

        private sealed class TestResourceLocation : IResourceLocation
        {
            public TestResourceLocation(string primaryKey)
            {
                PrimaryKey = primaryKey;
            }

            public string PrimaryKey { get; }
            public string InternalId => PrimaryKey;
            public string ProviderId => "test";
            public IList<IResourceLocation> Dependencies => Array.Empty<IResourceLocation>();
            public int DependencyHashCode => 0;
            public bool HasDependencies => false;
            public object Data => null;
            public Type ResourceType => typeof(UnityEngine.Object);

            public int Hash(Type resultType)
            {
                unchecked
                {
                    return (PrimaryKey?.GetHashCode() ?? 0) * 397 ^ (resultType?.GetHashCode() ?? 0);
                }
            }
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
