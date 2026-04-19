#if YOOASSET_SUPPORT
using System.Reflection;
using LFramework.Editor.Builder.BuildingResource;
using NUnit.Framework;
using YooAsset.Editor;

namespace LFramework.Editor.Tests.Settings
{
    public class YooAssetsBuildSystemTests
    {
        [TestCase(EBuildinFileCopyOption.None, 0, EBuildinFileCopyOption.None)]
        [TestCase(EBuildinFileCopyOption.None, 2, EBuildinFileCopyOption.None)]
        [TestCase(EBuildinFileCopyOption.ClearAndCopyAll, 0, EBuildinFileCopyOption.ClearAndCopyAll)]
        [TestCase(EBuildinFileCopyOption.ClearAndCopyAll, 1, EBuildinFileCopyOption.OnlyCopyAll)]
        [TestCase(EBuildinFileCopyOption.ClearAndCopyByTags, 0, EBuildinFileCopyOption.ClearAndCopyByTags)]
        [TestCase(EBuildinFileCopyOption.ClearAndCopyByTags, 3, EBuildinFileCopyOption.OnlyCopyByTags)]
        [TestCase(EBuildinFileCopyOption.OnlyCopyAll, 2, EBuildinFileCopyOption.OnlyCopyAll)]
        [TestCase(EBuildinFileCopyOption.OnlyCopyByTags, 2, EBuildinFileCopyOption.OnlyCopyByTags)]
        public void ResolvePackageBuildinCopyOption_UsesNonClearingCopyModesAfterFirstPackage(
            EBuildinFileCopyOption sourceOption,
            int packageIndex,
            EBuildinFileCopyOption expectedOption)
        {
            MethodInfo method = typeof(YooAssetsBuildSystem).GetMethod(
                "ResolvePackageBuildinCopyOption",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null, "Expected multi-package buildin copy option resolver.");

            object result = method.Invoke(null, new object[] { sourceOption, packageIndex });

            Assert.That(result, Is.AssignableTo<EBuildinFileCopyOption>());
            Assert.That((EBuildinFileCopyOption)result, Is.EqualTo(expectedOption));
        }
    }
}
#endif
