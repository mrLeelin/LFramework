using System;
using System.Collections.Generic;
using System.Threading;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.World
{
    public sealed class WorldResidentHostTests
    {
        private WorldResidentHost _host;
        private static readonly List<string> Calls = new();

        [SetUp]
        public void SetUp()
        {
            LServices.Reset();
            Injection.ClearAll();
            Calls.Clear();
            CountingResident.Created = 0;
            _host = new WorldResidentHost();
        }

        [TearDown]
        public void TearDown()
        {
            try { _host.ShutDown(); }
            finally { LServices.Reset(); Injection.ClearAll(); }
        }

        private void Enter(Type world)
        {
            using var transition = _host.BeginTransition(world);
            transition.Commit();
        }

        [Test]
        public void MatchingWorldsRetainInstanceAndDataLeavingRangeReleasesAndReentryCreatesFreshInstance()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Enter(typeof(WorldA));
            var first = LServices.Get<CountingResident>();
            first.Value = 42;
            Enter(typeof(WorldB));
            Enter(typeof(WorldC));
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(first));
            Assert.That(first.Value, Is.EqualTo(42));
            Assert.That(first.InitializeCount, Is.EqualTo(1));
            Enter(typeof(WorldD));
            Assert.That(first.ShutdownCount, Is.EqualTo(1));
            Assert.That(first.IsActive, Is.False);
            Assert.That(LServices.TryGet<ICounter>(out _), Is.False);
            Enter(typeof(WorldA));
            Assert.That(LServices.Get<CountingResident>(), Is.Not.SameAs(first));
            Assert.That(LServices.Get<CountingResident>().Value, Is.Zero);
        }

        [Test]
        public void ExactWorldTypeMatchDoesNotActivateForDerivedWorld()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Enter(typeof(DerivedWorldA));
            Assert.That(_host.ActiveCount, Is.Zero);
            Assert.That(CountingResident.Created, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void NullTargetAndShutdownCancelLifetimeAndCleanExactlyOnce(bool shutdown)
        {
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Enter(typeof(WorldA));
            var resident = LServices.Get<CountingResident>();
            var token = resident.Token;
            if (shutdown) _host.ShutDown();
            else Enter(null);
            _host.ShutDown();
            Assert.That(token.IsCancellationRequested, Is.True);
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            Assert.That(LServices.TryGet<CountingResident>(out _), Is.False);
            Assert.That(LServices.TryGet<ICounter>(out _), Is.False);
            Assert.That(_host.ActiveCount, Is.Zero);
        }

        [Test]
        public void DependenciesResolveStagedInstanceBeforeRootPublication()
        {
            _host.RegisterTypes(new[] { typeof(DependentResident), typeof(CountingResident) });
            Enter(typeof(WorldA));
            var dependent = LServices.Get<DependentResident>();
            Assert.That(dependent.Counter, Is.SameAs(LServices.Get<ICounter>()));
            Assert.That(dependent.RootPublishedDuringInjection, Is.False);
            Assert.That(Calls, Is.EqualTo(new[] { "counter:init", "dependent:init" }));
        }

        [Test]
        public void DependencyWithNarrowerWorldRangeIsRejectedBeforeItCanBecomeStale()
        {
            _host.RegisterTypes(new[] { typeof(ShortLivedResident), typeof(LongLivedDependent) });
            Assert.Catch(() => _host.BeginTransition(typeof(WorldA)));
            Assert.That(LServices.TryGet<ShortLivedResident>(out _), Is.False);
            Assert.That(LServices.TryGet<LongLivedDependent>(out _), Is.False);
            Assert.That(_host.ActiveCount, Is.Zero);
        }

        [Test]
        public void FailedInitializationCancelsAndCleansEveryCreatedResident()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident), typeof(FailingAfterCounterResident) });
            FailingAfterCounterResident.Counter = null;
            Assert.Catch(() => _host.BeginTransition(typeof(WorldA)));
            var counter = FailingAfterCounterResident.Counter;
            Assert.That(counter, Is.Not.Null);
            Assert.That(counter.InitializeCount, Is.EqualTo(1));
            Assert.That(counter.ShutdownCount, Is.EqualTo(1));
            Assert.That(counter.IsActive, Is.False);
            Assert.That(LServices.TryGet<ICounter>(out _), Is.False);
        }

        [TestCase(typeof(FailingInitializeResident))]
        [TestCase(typeof(FailingConstructorResident))]
        [TestCase(typeof(FailingInjectionResident))]
        public void FailedPreparationLeavesNoPartialServicesAndPreservesExistingResident(Type failingType)
        {
            _host.RegisterTypes(new[] { typeof(CountingResident), failingType });
            Enter(typeof(WorldA));
            var original = LServices.Get<CountingResident>();
            Assert.Catch(() => _host.BeginTransition(typeof(WorldD)));
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(original));
            Assert.That(original.ShutdownCount, Is.Zero);
            Assert.That(original.Token.IsCancellationRequested, Is.False);
            Assert.That(LServices.TryGet(failingType, null, out _), Is.False);
            Assert.That(_host.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void ConflictingTargetAliasesFailBeforeConstructingAnyResident()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident), typeof(ConflictingResident) });
            Assert.Catch(() => _host.BeginTransition(typeof(WorldA)));
            Assert.That(CountingResident.Created, Is.Zero);
            Assert.That(_host.ActiveCount, Is.Zero);
            Assert.That(LServices.TryGet<ICounter>(out _), Is.False);
        }

        [Test]
        public void ExternalAliasConflictFailsBeforeConstructionAndPreservesExternalService()
        {
            var external = new ExternalCounter();
            LServices.Register<ICounter>(external);
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Assert.Catch(() => _host.BeginTransition(typeof(WorldA)));
            Assert.That(CountingResident.Created, Is.Zero);
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(external));
        }

        [Test]
        public void ShutdownDoesNotRemoveExternallyReplacedAlias()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Enter(typeof(WorldA));
            var resident = LServices.Get<CountingResident>();
            var external = new ExternalCounter();
            LServices.Register<ICounter>(external);
            _host.ShutDown();
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(external));
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void RolledBackAliasReplacementRestoresOutgoingInstance()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident), typeof(ReplacementResident) });
            Enter(typeof(WorldA));
            var original = LServices.Get<ICounter>();
            ReplacementResident replacement;
            using (_host.BeginTransition(typeof(WorldD)))
            {
                replacement = LServices.Get<ReplacementResident>();
                Assert.That(LServices.Get<ICounter>(), Is.SameAs(replacement));
            }
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(original));
            Assert.That(replacement.ShutdownCount, Is.EqualTo(1));
            Assert.That(((CountingResident)original).ShutdownCount, Is.Zero);
        }

        [Test]
        public void RollbackDoesNotOverwriteExternalReplacement()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident), typeof(ReplacementResident) });
            Enter(typeof(WorldA));
            var external = new ExternalCounter();
            using (_host.BeginTransition(typeof(WorldD))) LServices.Register<ICounter>(external);
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(external));
        }

        [Test]
        public void UpdatesAreSuspendedWhileTransitionPendingAndResumeOnRollback()
        {
            _host.RegisterTypes(new[] { typeof(UpdatingResident) });
            Enter(typeof(WorldA));
            var resident = LServices.Get<UpdatingResident>();
            _host.UpdateComponent(0.25f, 0.5f);
            Assert.That(resident.UpdateCount, Is.EqualTo(1));
            Assert.That(resident.Elapsed, Is.EqualTo(0.25f));
            Assert.That(resident.RealElapsed, Is.EqualTo(0.5f));
            using (_host.BeginTransition(typeof(WorldD))) _host.UpdateComponent(1, 2);
            Assert.That(resident.UpdateCount, Is.EqualTo(1));
            _host.UpdateComponent(1, 2);
            Assert.That(resident.UpdateCount, Is.EqualTo(2));
            Enter(typeof(WorldD));
            _host.UpdateComponent(1, 2);
            Assert.That(resident.UpdateCount, Is.EqualTo(2));
        }

        [Test]
        public void ShutdownFailureStillCleansOtherResidentsAndIsNotRetried()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident), typeof(FailingShutdownResident) });
            Enter(typeof(WorldA));
            var healthy = LServices.Get<CountingResident>();
            var failing = LServices.Get<FailingShutdownResident>();
            Assert.Catch(() => _host.ShutDown());
            Assert.That(healthy.ShutdownCount, Is.EqualTo(1));
            Assert.That(failing.ShutdownCount, Is.EqualTo(1));
            Assert.That(LServices.TryGet<ICounter>(out _), Is.False);
            Assert.That(LServices.TryGet<FailingShutdownResident>(out _), Is.False);
            Assert.That(_host.ActiveCount, Is.Zero);
            Assert.DoesNotThrow(() => _host.ShutDown());
        }

        [Test]
        public void OutgoingServicesAreHiddenUntilRollback()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Enter(typeof(WorldA));
            var original = LServices.Get<ICounter>();
            using (_host.BeginTransition(typeof(WorldD)))
            {
                Assert.That(LServices.TryGet<ICounter>(out _), Is.False);
                Assert.That(((CountingResident)original).IsActive, Is.True);
            }
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(original));
        }

        [Test]
        public void CommitRejectsOverwrittenRetainedServiceAndRollbackRestoresMissingAlias()
        {
            _host.RegisterTypes(new[] { typeof(CountingResident) });
            Enter(typeof(WorldA));
            var original = LServices.Get<ICounter>();
            using (var transition = _host.BeginTransition(typeof(WorldB)))
            {
                LServices.Register<ICounter>(new ExternalCounter());
                Assert.Throws<InvalidOperationException>(transition.Commit);
                LServices.Unregister<ICounter>();
            }
            Assert.That(LServices.Get<ICounter>(), Is.SameAs(original));
        }

        [Test]
        public void ResidentUpdateCanSwitchWorldAndSkipsReleasedResidents()
        {
            _host.RegisterTypes(new[] { typeof(SwitchingResident), typeof(UpdatingResident) });
            Enter(typeof(WorldA));
            var updating = LServices.Get<UpdatingResident>();
            LServices.Get<SwitchingResident>().Switch = () => Enter(typeof(WorldD));
            Assert.DoesNotThrow(() => _host.UpdateComponent(1, 1));
            Assert.That(updating.UpdateCount, Is.Zero);
            Assert.That(updating.IsActive, Is.False);
            Assert.That(_host.ActiveCount, Is.Zero);
        }

        [WorldResident(typeof(WorldA), Sort = 1000)]
        public sealed class SwitchingResident : WorldResidentBase, IWorldResidentUpdate
        {
            public Action Switch;
            public void Update(float elapsed, float realElapsed) { Switch(); }
        }

        [Test]
        public void WorldCallbacksUseSortOrderAndEachWorldInstance()
        {
            _host.RegisterTypes(new[] { typeof(EarlyWorldResident), typeof(LateWorldResident) });
            var firstWorld = new WorldA();
            using (var transition = _host.BeginTransition(typeof(WorldA)))
            {
                transition.EnterWorld(firstWorld);
                transition.Commit();
            }
            var early = LServices.Get<EarlyWorldResident>();
            Assert.That(early.EnteredWorld, Is.SameAs(firstWorld));
            Assert.Throws<InvalidOperationException>(() => _host.BeginTransition(typeof(WorldA)));
            _host.ExitWorld();
            _host.ExitWorld();
            var secondWorld = new WorldA();
            using (var transition = _host.BeginTransition(typeof(WorldA)))
            {
                transition.EnterWorld(secondWorld);
                transition.Commit();
            }
            Assert.That(LServices.Get<EarlyWorldResident>(), Is.SameAs(early));
            Assert.That(early.EnteredWorld, Is.SameAs(secondWorld));
            _host.ShutDown();
            Assert.That(Calls, Is.EqualTo(new[]
            {
                "early:enter", "late:enter", "late:exit", "early:exit",
                "early:enter", "late:enter", "late:exit", "early:exit",
                "late:shutdown", "early:shutdown"
            }));
        }

        [Test]
        public void FailedExitRetainsResidentsAndOnlyFailedCallbackIsRetried()
        {
            _host.RegisterTypes(new[] { typeof(EarlyWorldResident), typeof(LateWorldResident) });
            using (var transition = _host.BeginTransition(typeof(WorldA)))
            {
                transition.EnterWorld(new WorldA());
                transition.Commit();
            }
            LServices.Get<LateWorldResident>().FailNextExit = true;
            Assert.Throws<AggregateException>(_host.ShutDown);
            Assert.That(_host.ActiveCount, Is.EqualTo(2));
            Assert.That(LServices.Get<EarlyWorldResident>().IsActive, Is.True);
            Assert.That(Calls, Is.EqualTo(new[] { "early:enter", "late:enter", "late:exit", "early:exit" }));
            Assert.DoesNotThrow(_host.ShutDown);
            Assert.That(Calls.FindAll(call => call == "early:exit").Count, Is.EqualTo(1));
            Assert.That(Calls.FindAll(call => call == "late:exit").Count, Is.EqualTo(2));
            Assert.That(_host.ActiveCount, Is.Zero);
        }

        [Test]
        public void FailedEnterIsCompensatedInReverseOrderBeforeShutdown()
        {
            _host.RegisterTypes(new[] { typeof(EarlyWorldResident), typeof(LateWorldResident) });
            var transition = _host.BeginTransition(typeof(WorldA));
            LServices.Get<LateWorldResident>().FailEnter = true;
            Assert.Throws<InvalidOperationException>(() => transition.EnterWorld(new WorldA()));
            transition.Dispose();
            Assert.That(Calls, Is.EqualTo(new[]
            {
                "early:enter", "late:enter", "late:exit", "early:exit", "late:shutdown", "early:shutdown"
            }));
            Assert.That(LServices.TryGet<EarlyWorldResident>(out _), Is.False);
        }

        [Test]
        public void FailedEntryCompensationKeepsTransitionAvailableForRetry()
        {
            _host.RegisterTypes(new[] { typeof(EarlyWorldResident), typeof(LateWorldResident) });
            var transition = _host.BeginTransition(typeof(WorldA));
            var late = LServices.Get<LateWorldResident>();
            late.FailEnter = late.FailNextExit = true;
            Assert.Throws<InvalidOperationException>(() => transition.EnterWorld(new WorldA()));
            Assert.Throws<AggregateException>(transition.Dispose);
            Assert.That(late.IsActive, Is.True);
            Assert.That(Calls, Does.Not.Contain("late:shutdown"));
            Assert.DoesNotThrow(transition.Dispose);
            Assert.That(Calls.FindAll(call => call == "early:exit").Count, Is.EqualTo(1));
            Assert.That(Calls.FindAll(call => call == "late:exit").Count, Is.EqualTo(2));
            Assert.That(late.IsActive, Is.False);
        }

        [Test]
        public void WrongWorldInstanceTypeIsRejectedBeforeEntryCallbacks()
        {
            _host.RegisterTypes(new[] { typeof(EarlyWorldResident) });
            using (var transition = _host.BeginTransition(typeof(WorldA)))
                Assert.Throws<InvalidOperationException>(() => transition.EnterWorld(new WorldB()));
            Assert.That(Calls, Is.EqualTo(new[] { "early:shutdown" }));
        }

        [WorldResident(typeof(WorldA), Sort = 100)]
        public sealed class EarlyWorldResident : WorldResidentBase
        {
            public WorldBase EnteredWorld;
            protected override void OnEnterWorld(WorldBase world)
            {
                EnteredWorld = world;
                Calls.Add("early:enter");
            }
            protected override void OnExitWorld(WorldBase world) { Calls.Add("early:exit"); }
            protected override void OnShutdown() { Calls.Add("early:shutdown"); }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class LateWorldResident : WorldResidentBase
        {
            public bool FailEnter;
            public bool FailNextExit;
            protected override void OnEnterWorld(WorldBase world)
            {
                Calls.Add("late:enter");
                if (FailEnter) throw new InvalidOperationException("enter failure");
            }
            protected override void OnExitWorld(WorldBase world)
            {
                Calls.Add("late:exit");
                if (FailNextExit)
                {
                    FailNextExit = false;
                    throw new InvalidOperationException("exit failure");
                }
            }
            protected override void OnShutdown() { Calls.Add("late:shutdown"); }
        }

        public interface ICounter { }
        public sealed class ExternalCounter : ICounter { }
        public class WorldA : WorldBase { }
        public sealed class DerivedWorldA : WorldA { }
        public sealed class WorldB : WorldBase { }
        public sealed class WorldC : WorldBase { }
        public sealed class WorldD : WorldBase { }

        [WorldResident(typeof(WorldA), typeof(WorldB), typeof(WorldC), Sort = 100)]
        public sealed class CountingResident : WorldResidentBase, ICounter
        {
            public static int Created;
            public int Value;
            public int InitializeCount;
            public int ShutdownCount;
            public CancellationToken Token => LifetimeToken;
            public CountingResident() { Created++; }
            protected override void OnInitialize() { InitializeCount++; Calls.Add("counter:init"); }
            protected override void OnShutdown() { ShutdownCount++; }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class DependentResident : WorldResidentBase, IInjectable
        {
            public ICounter Counter;
            public bool RootPublishedDuringInjection;
            public void Inject(IServiceResolver resolver)
            {
                Counter = resolver.Get<ICounter>();
                RootPublishedDuringInjection = LServices.TryGet<ICounter>(out _);
            }
            protected override void OnInitialize() { Calls.Add("dependent:init"); }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class ConflictingResident : WorldResidentBase, ICounter { }

        [WorldResident(typeof(WorldD))]
        public sealed class ReplacementResident : WorldResidentBase, ICounter
        {
            public int ShutdownCount;
            protected override void OnShutdown() { ShutdownCount++; }
        }

        [WorldResident(typeof(WorldD))]
        public sealed class FailingInitializeResident : WorldResidentBase
        {
            protected override void OnInitialize() { throw new InvalidOperationException("initialize failure"); }
        }

        [WorldResident(typeof(WorldD))]
        public sealed class FailingConstructorResident : WorldResidentBase
        {
            public FailingConstructorResident() { throw new InvalidOperationException("constructor failure"); }
        }

        [WorldResident(typeof(WorldD))]
        public sealed class FailingInjectionResident : WorldResidentBase, IInjectable
        {
            public void Inject(IServiceResolver resolver) { resolver.Get<IDisposable>(); }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class UpdatingResident : WorldResidentBase, IWorldResidentUpdate
        {
            public int UpdateCount;
            public float Elapsed;
            public float RealElapsed;
            public void Update(float elapsed, float realElapsed)
            {
                UpdateCount++;
                Elapsed = elapsed;
                RealElapsed = realElapsed;
            }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class FailingShutdownResident : WorldResidentBase
        {
            public int ShutdownCount;
            protected override void OnShutdown()
            {
                ShutdownCount++;
                throw new InvalidOperationException("shutdown failure");
            }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class ShortLivedResident : WorldResidentBase { }

        [WorldResident(typeof(WorldA), typeof(WorldB))]
        public sealed class LongLivedDependent : WorldResidentBase, IInjectable
        {
            public void Inject(IServiceResolver resolver) { resolver.Get<ShortLivedResident>(); }
        }

        [WorldResident(typeof(WorldA))]
        public sealed class FailingAfterCounterResident : WorldResidentBase, IInjectable
        {
            public static CountingResident Counter;
            public void Inject(IServiceResolver resolver) { Counter = resolver.Get<CountingResident>(); }
            protected override void OnInitialize() { throw new InvalidOperationException("failure after dependency initialization"); }
        }
    }
}
