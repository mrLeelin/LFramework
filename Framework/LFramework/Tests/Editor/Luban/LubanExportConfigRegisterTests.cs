using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.IO;

namespace Luban.Editor.Tests
{
    public class LubanExportConfigRegisterTests
    {
        private const string TempAssetPath = "Assets/__TempLubanExportConfig.asset";

        [SetUp]
        public void SetUp()
        {
            DeleteTempAsset();
            LubanExportConfig.SetAssetLookupForTests(null);
            LubanExportConfig.SetPackageRootResolverForTests(null);
            LubanExportConfig.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LubanExportConfig.SetAssetLookupForTests(null);
            LubanExportConfig.SetPackageRootResolverForTests(null);
            LubanExportConfig.ResetCacheForTests();
            DeleteTempAsset();
        }

        [Test]
        public void GetOrCreate_ShouldCacheAssetLookupResult_UntilCacheIsReset()
        {
            var asset = ScriptableObject.CreateInstance<LubanExportConfig>();
            AssetDatabase.CreateAsset(asset, TempAssetPath);
            string guid = AssetDatabase.AssetPathToGUID(TempAssetPath);
            int lookupCount = 0;

            LubanExportConfig.SetAssetLookupForTests(() =>
            {
                lookupCount++;
                return new[] { guid };
            });

            var first = LubanExportConfig.GetOrCreate();
            var second = LubanExportConfig.GetOrCreate();

            Assert.That(first, Is.SameAs(asset));
            Assert.That(second, Is.SameAs(asset));
            Assert.That(lookupCount, Is.EqualTo(1));
        }

        [Test]
        public void ResolveExternalToolPath_ShouldConvertPackagesAssetPath_ToResolvedDiskPath()
        {
            LubanExportConfig.SetPackageRootResolverForTests(assetPath =>
            {
                if (assetPath == "Packages/com.lframework.core/Framework/LFramework/Assets/Template/Luban/Templates")
                {
                    return @"D:\UnityCache\com.lframework.core";
                }

                return null;
            });

            string resolved = LubanExportConfig.ResolveExternalToolPath(
                "Packages/com.lframework.core/Framework/LFramework/Assets/Template/Luban/Templates");

            Assert.That(
                resolved,
                Is.EqualTo("D:/UnityCache/com.lframework.core/Framework/LFramework/Assets/Template/Luban/Templates"));
        }

        [Test]
        public void ResolveExternalToolPath_ShouldConvertAssetsPath_ToProjectAbsolutePath()
        {
            string resolved = LubanExportConfig.ResolveExternalToolPath("Assets/Framework");

            Assert.That(resolved, Is.EqualTo(Path.GetFullPath("Assets/Framework").Replace("\\", "/")));
        }

        [Test]
        public void ResolveExternalToolPath_ShouldKeepAbsolutePath_Normalized()
        {
            string input = Path.Combine(Path.GetTempPath(), "Luban", "Templates");

            string resolved = LubanExportConfig.ResolveExternalToolPath(input);

            Assert.That(resolved, Is.EqualTo(Path.GetFullPath(input).Replace("\\", "/")));
        }

        private static void DeleteTempAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TempAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(TempAssetPath);
            }
        }
    }
}
