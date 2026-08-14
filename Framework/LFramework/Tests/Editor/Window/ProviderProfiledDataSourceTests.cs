using System;
using LFramework.Editor.Window;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Window
{
    public sealed class ProviderProfiledDataSourceTests
    {
        [Test]
        public void CreateSnapshotDeduplicatesProviderAliasesAndPreservesBindings()
        {
            var provider = new DiagnosticsTestProvider();
            provider.SetDiagnosticsRegistration(7, ProviderLifeCycle.Forever, 42);
            provider.SubscribeEvent();
            provider.SetDiagnosticsStage(ProviderDiagnosticsStage.Ready);
            var registrations = new[]
            {
                new ProviderServiceRegistration(typeof(DiagnosticsTestProvider), null, provider, "Root", false),
                new ProviderServiceRegistration(typeof(IDiagnosticsTestProvider), "active", provider, "Root/Child 1", true),
            };

            ProviderProfiledSnapshot snapshot = ProviderProfiledDataSource.CreateSnapshot(registrations, 2);

            Assert.That(snapshot.ScopeCount, Is.EqualTo(2));
            Assert.That(snapshot.Providers, Has.Count.EqualTo(1));
            Assert.That(snapshot.Providers[0].ServiceRegistrations, Has.Count.EqualTo(2));
            Assert.That(snapshot.Providers[0].Diagnostics.Instance, Is.SameAs(provider));
            Assert.That(snapshot.Providers[0].Diagnostics.ProcedureState, Is.EqualTo(7));
            Assert.That(snapshot.Providers[0].Diagnostics.LifeCycle, Is.EqualTo(ProviderLifeCycle.Forever));
            Assert.That(snapshot.Providers[0].Diagnostics.Sort, Is.EqualTo(42));
            Assert.That(snapshot.Providers[0].Diagnostics.Stage, Is.EqualTo(ProviderDiagnosticsStage.Ready));
            Assert.That(snapshot.Providers[0].Diagnostics.IsSubscribed, Is.True);
            Assert.That(snapshot.Providers[0].Diagnostics.OverridesUpdate, Is.True);
            Assert.That(snapshot.Providers[0].Diagnostics.SupportsLateUpdate, Is.False);
        }

        private interface IDiagnosticsTestProvider : ISystemProvider
        {
        }

        private sealed class DiagnosticsTestProvider : SystemProviderBase, IDiagnosticsTestProvider
        {
            protected override void Subscribe(EventComponent eventComponent)
            {
            }

            public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
            {
            }
        }
    }
}
