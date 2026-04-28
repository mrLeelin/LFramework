using System;
using System.Linq;
using LFramework.Editor;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Settings
{
    public class RouteIndexBuilderTests
    {
        [Test]
        public void BuildEntries_UsesCollectedAddress_FallsBackToAssetPath_AndSkipsDisabledSources()
        {
            var entries = RouteIndexBuilder.BuildEntries(new[]
            {
                new RouteIndexSource
                {
                    PackageId = "ui",
                    AssetPath = "Assets/UI/Home.prefab",
                    Address = "ui/home",
                    IncludeInRouteIndex = true
                },
                new RouteIndexSource
                {
                    PackageId = "scene",
                    AssetPath = "Assets/Scenes/Home.unity",
                    Address = string.Empty,
                    IncludeInRouteIndex = true
                },
                new RouteIndexSource
                {
                    PackageId = "fx",
                    AssetPath = "Assets/Vfx/Explosion.prefab",
                    Address = "fx/explosion",
                    IncludeInRouteIndex = false
                }
            }, true);

            Assert.That(entries.Select(entry => $"{entry.address}:{entry.packageId}").ToArray(), Is.EqualTo(new[]
            {
                "Assets/Scenes/Home.unity:scene",
                "ui/home:ui"
            }));
        }

        [Test]
        public void BuildEntries_Throws_WhenDuplicateAddressDetected()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => RouteIndexBuilder.BuildEntries(new[]
            {
                new RouteIndexSource
                {
                    PackageId = "ui",
                    AssetPath = "Assets/UI/Home.prefab",
                    Address = "shared/home",
                    IncludeInRouteIndex = true
                },
                new RouteIndexSource
                {
                    PackageId = "scene",
                    AssetPath = "Assets/Scenes/Home.unity",
                    Address = "shared/home",
                    IncludeInRouteIndex = true
                }
            }, true));

            Assert.That(exception.Message, Does.Contain("shared/home"));
        }

        [Test]
        public void BuildEntries_LastWriterWins_WhenDuplicateDetectionIsDisabled()
        {
            var entries = RouteIndexBuilder.BuildEntries(new[]
            {
                new RouteIndexSource
                {
                    PackageId = "ui",
                    AssetPath = "Assets/UI/Home.prefab",
                    Address = "shared/home",
                    IncludeInRouteIndex = true
                },
                new RouteIndexSource
                {
                    PackageId = "scene",
                    AssetPath = "Assets/Scenes/Home.unity",
                    Address = "shared/home",
                    IncludeInRouteIndex = true
                }
            }, false);

            Assert.That(entries, Has.Count.EqualTo(1));
            Assert.That(entries[0].address, Is.EqualTo("shared/home"));
            Assert.That(entries[0].packageId, Is.EqualTo("scene"));
        }

        [Test]
        public void ExactAddressUserDataUtility_RoundTripsAddress()
        {
            string userData = ExactAddressUserDataUtility.Serialize("route-index");

            bool ok = ExactAddressUserDataUtility.TryDeserialize(userData, out ExactAddressUserData payload);

            Assert.That(ok, Is.True);
            Assert.That(payload, Is.Not.Null);
            Assert.That(payload.ExactAddress, Is.EqualTo("route-index"));
        }
    }
}
