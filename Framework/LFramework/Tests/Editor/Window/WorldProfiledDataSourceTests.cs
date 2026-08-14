using System;
using LFramework.Editor.Window;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Window
{
    public sealed class WorldProfiledDataSourceTests
    {
        [SetUp]
        public void SetUp()
        {
            LServices.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            LServices.Reset();
        }

        [Test]
        public void CreateSnapshotDeduplicatesWorldAliasesAndPreservesRegistrations()
        {
            var world = new DataSourceTestWorld();
            var registrations = new[]
            {
                new WorldServiceRegistration(typeof(DataSourceTestWorld), null, world, "Root", false),
                new WorldServiceRegistration(typeof(IDataSourceTestWorld), "active", world, "Root", false),
            };

            WorldProfiledSnapshot snapshot = WorldProfiledDataSource.CreateSnapshot(registrations);

            Assert.That(snapshot.Worlds, Has.Count.EqualTo(1));
            Assert.That(snapshot.Worlds[0].Diagnostics.Instance, Is.SameAs(world));
            Assert.That(snapshot.Worlds[0].ServiceRegistrations, Has.Count.EqualTo(2));
            Assert.That(snapshot.Worlds[0].ServiceRegistrations[0].ServiceType, Is.EqualTo(typeof(DataSourceTestWorld)));
            Assert.That(snapshot.Worlds[0].ServiceRegistrations[1].ServiceType, Is.EqualTo(typeof(IDataSourceTestWorld)));
            Assert.That(snapshot.Worlds[0].ServiceRegistrations[1].Identifier, Is.EqualTo("active"));
        }

        [Test]
        public void RefreshGateThrottlesAgainAfterFailedAttempt()
        {
            var gate = new WorldProfiledRefreshGate(0.5d);

            Assert.Throws<InvalidOperationException>(() => BeginFailingRefresh(gate, 10d));

            Assert.That(gate.TryBeginRefresh(10.49d), Is.False);
            Assert.That(gate.TryBeginRefresh(10.5d), Is.True);
        }

        private static void BeginFailingRefresh(WorldProfiledRefreshGate gate, double currentTime)
        {
            if (gate.TryBeginRefresh(currentTime))
            {
                throw new InvalidOperationException("Capture failed");
            }
        }

        [Test]
        public void CaptureReportsCurrentHelperBindingFromChildScope()
        {
            var world = new DataSourceTestWorld();
            DataSourceTestHelper helper = world.RegisterHelper();
            LServices.Register(world);

            try
            {
                using (LServiceScope childScope = LServices.CreateScope())
                {
                    childScope.Register(typeof(IDataSourceTestHelper), "active", helper);

                    WorldProfiledSnapshot snapshot = WorldProfiledDataSource.Capture();

                    Assert.That(snapshot.ScopeCount, Is.EqualTo(2));
                    Assert.That(snapshot.Worlds, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Worlds[0].Helpers, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Worlds[0].Helpers[0].Diagnostics.Instance, Is.SameAs(helper));
                    Assert.That(snapshot.Worlds[0].Helpers[0].IsRegisteredAsService, Is.True);
                    Assert.That(snapshot.Worlds[0].Helpers[0].ServiceRegistrations, Has.Count.EqualTo(1));
                    Assert.That(snapshot.Worlds[0].Helpers[0].ServiceRegistrations[0].ScopeName, Is.EqualTo("Root/Child 1"));
                }
            }
            finally
            {
                world.Clear();
            }
        }

        private interface IDataSourceTestWorld : IWorld
        {
        }

        private interface IDataSourceTestHelper : IWorldHelper
        {
        }

        private sealed class DataSourceTestWorld : WorldBase, IDataSourceTestWorld
        {
            public DataSourceTestHelper RegisterHelper()
            {
                return BuildWorldHelpers<DataSourceTestHelper>();
            }

            protected override void OnInitialized()
            {
            }

            protected override void OnDeInitialized()
            {
            }
        }

        private sealed class DataSourceTestHelper : WorldHelperBase, IDataSourceTestHelper
        {
            public override void Clear()
            {
            }

            public override void Initialize()
            {
            }

            public override void StartGame()
            {
            }

            public override void StopGame()
            {
            }
        }
    }
}
