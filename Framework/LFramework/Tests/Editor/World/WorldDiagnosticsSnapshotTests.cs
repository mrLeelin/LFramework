using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.World
{
    public sealed class WorldDiagnosticsSnapshotTests
    {
        [Test]
        public void SnapshotDescribesUniqueHelpersInterfacesAndFrameCallbacks()
        {
            var world = new DiagnosticsTestWorld();
            DiagnosticsTestHelper helper = world.RegisterHelper();

            try
            {
                WorldDiagnosticsSnapshot snapshot = world.GetDiagnosticsSnapshot();

                Assert.That(snapshot.WorldType, Is.EqualTo(typeof(DiagnosticsTestWorld)));
                Assert.That(snapshot.Helpers, Has.Count.EqualTo(1));
                Assert.That(snapshot.Helpers, Is.Not.InstanceOf<List<WorldHelperDiagnostics>>());

                WorldHelperDiagnostics helperDiagnostics = snapshot.Helpers.Single();
                Assert.That(helperDiagnostics.Instance, Is.SameAs(helper));
                Assert.That(helperDiagnostics.HelperType, Is.EqualTo(typeof(DiagnosticsTestHelper)));
                Assert.That(helperDiagnostics.ServiceInterface, Is.EqualTo(typeof(IDiagnosticsTestHelper)));
                Assert.That(helperDiagnostics.RegistrationTypes, Does.Contain(typeof(DiagnosticsTestHelper)));
                Assert.That(helperDiagnostics.RegistrationTypes, Is.Not.InstanceOf<System.Type[]>());
                Assert.That(helperDiagnostics.ImplementedInterfaces, Does.Contain(typeof(IDiagnosticsTestHelper)));
                Assert.That(helperDiagnostics.ImplementedInterfaces, Does.Contain(typeof(IWorldUpdate)));
                Assert.That(helperDiagnostics.ImplementedInterfaces, Does.Contain(typeof(IWorldLateUpdate)));
                Assert.That(helperDiagnostics.ImplementedInterfaces, Is.Not.InstanceOf<System.Type[]>());
                Assert.That(helperDiagnostics.ReceivesUpdate, Is.True);
                Assert.That(helperDiagnostics.ReceivesLateUpdate, Is.True);
                Assert.That(helperDiagnostics.IsAutoRegistered, Is.False);
            }
            finally
            {
                world.Clear();
            }
        }

        [Test]
        public void SnapshotUsesReferenceIdentityForValueEqualHelpers()
        {
            var world = new DiagnosticsTestWorld();
            ValueEqualFirstHelper first = world.RegisterHelper<ValueEqualFirstHelper>();
            ValueEqualSecondHelper second = world.RegisterHelper<ValueEqualSecondHelper>();

            try
            {
                WorldDiagnosticsSnapshot snapshot = world.GetDiagnosticsSnapshot();

                Assert.That(snapshot.Helpers, Has.Count.EqualTo(2));
                Assert.That(snapshot.Helpers.Any(item => ReferenceEquals(item.Instance, first)), Is.True);
                Assert.That(snapshot.Helpers.Any(item => ReferenceEquals(item.Instance, second)), Is.True);
            }
            finally
            {
                world.Clear();
            }
        }

        private interface IDiagnosticsTestHelper : IWorldHelper
        {
        }

        private sealed class DiagnosticsTestWorld : WorldBase
        {
            public DiagnosticsTestHelper RegisterHelper()
            {
                return BuildWorldHelpers<DiagnosticsTestHelper>();
            }

            public T RegisterHelper<T>() where T : class, IWorldHelper, new()
            {
                return BuildWorldHelpers<T>();
            }

            protected override void OnInitialized()
            {
            }

            protected override void OnDeInitialized()
            {
            }
        }

        private abstract class ValueEqualHelperBase : WorldHelperBase
        {
            public override bool Equals(object obj)
            {
                return obj is ValueEqualHelperBase;
            }

            public override int GetHashCode()
            {
                return 1;
            }

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

        private sealed class ValueEqualFirstHelper : ValueEqualHelperBase
        {
        }

        private sealed class ValueEqualSecondHelper : ValueEqualHelperBase
        {
        }

        private sealed class DiagnosticsTestHelper : WorldHelperBase, IDiagnosticsTestHelper, IWorldUpdate, IWorldLateUpdate
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

            public override UniTask InstantiateWorld()
            {
                return UniTask.CompletedTask;
            }

            public override void StopGame()
            {
            }

            public void OnUpdate(float elapseSeconds, float realElapseSeconds)
            {
            }

            public void LateUpdate()
            {
            }
        }
    }
}
