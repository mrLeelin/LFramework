using NUnit.Framework;
using UnityEditor;
using UnityEngine;

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
            LubanExportConfig.ResetCacheForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LubanExportConfig.SetAssetLookupForTests(null);
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

        private static void DeleteTempAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TempAssetPath) != null)
            {
                AssetDatabase.DeleteAsset(TempAssetPath);
            }
        }
    }
}
