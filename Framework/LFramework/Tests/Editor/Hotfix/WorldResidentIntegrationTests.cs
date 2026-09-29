using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework;
using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Hotfix;
using LFramework.Hotfix.Procedure;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Hotfix
{
    public sealed class WorldResidentIntegrationTests
    {
        private const int StateA = 7811;
        private const int StateB = 7812;
        private const int StateD = 7813;
        private const int StateFail = 7814;
        private static readonly List<string> Calls = new();
        private LSystemApplication _application;
        private WorldResidentHost _host;

        [SetUp]
        public void SetUp()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            LSystemApplication.ClearReflectionCache();
            SingletonManager.AddSingleton(new LFrameworkAspect());
            Calls.Clear();
            _host = new WorldResidentHost();
            LServices.Register(_host);
            LServices.Register(new EventComponent());
            var hotfix = new HotfixComponent();
            LServices.Register(hotfix);
            var types = new Dictionary<string, Type>();
            foreach (var type in new[]
            {
                typeof(WorldA), typeof(WorldB), typeof(WorldD), typeof(FailingWorld),
                typeof(MapResident), typeof(FailedWorldResident), typeof(DependentProvider)
            }) types.Add(type.FullName, type);
            typeof(HotfixComponent).GetMethod("ParseAttributes", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(hotfix, new object[] { types });
            _application = new LSystemApplication();
        }

        [TearDown]
        public void TearDown()
        {
            _application?.Dispose();
            _host.ShutDown();
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            LSystemApplication.ClearReflectionCache();
        }

        [Test]
        public void WorldSwitchRetainsSharedResidentUntilNonmatchingWorldEnters()
        {
            _application.TryRegisterWorld(StateA);
            var resident = LServices.Get<MapResident>();
            Assert.That(LServices.Get<WorldA>().Resident, Is.SameAs(resident));
            _application.TryUnRegisterWorld();
            Assert.That(resident.ShutdownCount, Is.Zero);
            Assert.That(LServices.Get<IMapService>(), Is.SameAs(resident));
            _application.TryRegisterWorld(StateB);
            Assert.That(LServices.Get<IMapService>(), Is.SameAs(resident));
            _application.TryUnRegisterWorld();
            _application.TryRegisterWorld(StateD);
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            Assert.That(LServices.TryGet<IMapService>(out _), Is.False);
        }

        [Test]
        public void PreparationMakesResidentAvailableToProviderBeforeWorldInitialization()
        {
            _application.PrepareWorldResidents(StateA);
            _application.TryRegisterProvider(StateA);
            var resident = LServices.Get<IMapService>();
            Assert.That(LServices.Get<DependentProvider>().Resident, Is.SameAs(resident));
            _application.TryRegisterWorld(StateA);
            Assert.That(LServices.Get<WorldA>().Resident, Is.SameAs(resident));
            _application.CompleteWorldResidentTransition();
            Assert.That(((MapResident)resident).InitializeCount, Is.EqualTo(1));
        }

        [Test]
        public void FailingWorldInitializationRollsBackNewResidentsAndRestoresOutgoingServices()
        {
            _application.TryRegisterWorld(StateA);
            var resident = LServices.Get<MapResident>();
            _application.TryUnRegisterWorld();
            Assert.Throws<InvalidOperationException>(() => _application.TryRegisterWorld(StateFail));
            Assert.That(LServices.Get<IMapService>(), Is.SameAs(resident));
            Assert.That(resident.ShutdownCount, Is.Zero);
            Assert.That(LServices.TryGet<FailedWorldResident>(out _), Is.False);
            Assert.That(LServices.TryGet<FailingWorld>(out _), Is.False);
            Assert.That(Calls, Does.Contain("failed-resident:shutdown"));
        }

        [Test]
        public void ApplicationDisposalStopsWorldAndProviderBeforeResident()
        {
            _application.PrepareWorldResidents(StateA);
            _application.TryRegisterProvider(StateA);
            _application.TryRegisterWorld(StateA);
            _application.CompleteWorldResidentTransition();
            var resident = LServices.Get<MapResident>();
            Calls.Clear();
            _application.Dispose();
            Assert.That(Calls.IndexOf("world:stop"), Is.GreaterThanOrEqualTo(0));
            Assert.That(Calls.IndexOf("provider:stop"), Is.GreaterThanOrEqualTo(0));
            Assert.That(Calls.IndexOf("resident:stop"), Is.GreaterThan(Calls.IndexOf("world:stop")));
            Assert.That(Calls.IndexOf("resident:stop"), Is.GreaterThan(Calls.IndexOf("provider:stop")));
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            Assert.That(LServices.TryGet<IMapService>(out _), Is.False);
            Assert.DoesNotThrow(() => _application.Dispose());
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
        }

        [Test]
        public void ProcedureWithoutWorldReleasesPreviousResident()
        {
            _application.TryRegisterWorld(StateA);
            var resident = LServices.Get<MapResident>();
            _application.TryUnRegisterWorld();
            Assert.That(_application.TryRegisterWorld(7899), Is.Null);
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            Assert.That(_host.ActiveCount, Is.Zero);
        }

        [Test]
        public void NewHotfixApplicationCanReuseHostAfterPreviousApplicationDisposal()
        {
            _application.TryRegisterWorld(StateA);
            var previous = LServices.Get<MapResident>();
            _application.Dispose();
            _application = new LSystemApplication();
            _application.TryRegisterWorld(StateA);
            Assert.That(LServices.Get<MapResident>(), Is.Not.SameAs(previous));
            Assert.That(_host.ActiveCount, Is.EqualTo(1));
        }

        [Test]
        public void CancelPreparationCleansResidentsAfterProviderFailure()
        {
            _application.PrepareWorldResidents(StateA);
            var resident = LServices.Get<MapResident>();
            _application.TryRegisterProvider(StateA);
            _application.TryUnRegisterProvider(StateA);
            _application.CancelWorldResidentTransition();
            Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            Assert.That(LServices.TryGet<IMapService>(out _), Is.False);
            Assert.That(_host.ActiveCount, Is.Zero);
        }

        [Test]
        public void BootstrapRegistersHostAndShutdownHookRunsBeforeRuntimeDependencies()
        {
            SingletonManager.Close();
            LServices.Reset();
            var gameObject = new UnityEngine.GameObject("Resident bootstrap test");
            try
            {
                var runtime = gameObject.AddComponent<ResidentBootstrapTestApplication>();
                runtime.RunStartApplication();
                Assert.That(LServices.TryGet<WorldResidentHost>(out var automaticHost), Is.True);
                Assert.That(automaticHost, Is.Not.Null);
                var shutdown = new RecordingApplicationShutdown();
                LServices.Register<IApplicationShutdown>(shutdown);
                Calls.Clear();
                runtime.StopApplication(ShutdownType.None);
                Assert.That(Calls, Is.EqualTo(new[] { "application:shutdown", "runtime:shutdown" }));
            }
            finally { UnityEngine.Object.DestroyImmediate(gameObject); }
        }

        [Test]
        public void BusinessEntryCanImmediatelySwitchProcedureWithSameResident()
        {
            var fsms = GameFrameworkEntry.GetModule<IFsmManager>();
            var procedures = GameFrameworkEntry.GetModule<IProcedureManager>();
            procedures.Initialize(fsms, new ImmediateProcedure(), new DestinationProcedure());
            try
            {
                procedures.StartProcedure<ImmediateProcedure>();
                Assert.That(procedures.CurrentProcedure, Is.TypeOf<DestinationProcedure>());
                Assert.That(LServices.Get<MapResident>().InitializeCount, Is.EqualTo(1));
                Assert.That(LServices.Get<MapResident>().ShutdownCount, Is.Zero);
                Assert.That(LServices.Get<WorldB>(), Is.Not.Null);
            }
            finally { fsms.DestroyFsm<IProcedureManager>(); }
        }

        [Test]
        public void ApplicationShutdownLeavesProcedureWhileResidentAndWorldAreAlive()
        {
            var gameObject = new UnityEngine.GameObject("Resident shutdown test");
            var fsmComponent = new FsmComponent();
            fsmComponent.AwakeComponent();
            LServices.Register(fsmComponent);
            var procedures = GameFrameworkEntry.GetModule<IProcedureManager>();
            procedures.Initialize(GameFrameworkEntry.GetModule<IFsmManager>(), new ShutdownProcedure());
            procedures.StartProcedure<ShutdownProcedure>();
            var resident = LServices.Get<MapResident>();
            Calls.Clear();
            try
            {
                var runtime = gameObject.AddComponent<ResidentBootstrapTestApplication>();
                runtime.StopApplication(ShutdownType.None);
                Assert.That(Calls[0], Is.EqualTo("procedure:leave"));
                Assert.That(Calls.FindAll(item => item == "procedure:leave").Count, Is.EqualTo(1));
                Assert.That(resident.ShutdownCount, Is.EqualTo(1));
                Assert.That(fsmComponent.HasFsm<IProcedureManager>(), Is.False);
            }
            finally
            {
                fsmComponent.DestroyFsm<IProcedureManager>();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void OverwrittenResidentAliasRejectsCommitAndRollbackRestoresRetainedResident()
        {
            _application.TryRegisterWorld(StateA);
            var resident = LServices.Get<IMapService>();
            _application.TryUnRegisterWorld();
            _application.PrepareWorldResidents(StateB);
            _application.TryRegisterWorld(StateB);
            LServices.Register<IMapService>(new ConflictingMapService());
            Assert.Throws<InvalidOperationException>(_application.CompleteWorldResidentTransition);
            _application.TryUnRegisterWorld();
            LServices.Unregister<IMapService>();
            _application.CancelWorldResidentTransition();
            Assert.That(LServices.Get<IMapService>(), Is.SameAs(resident));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProcedureExitRetriesFailedProviderWithoutRepeatingBusinessLeave(bool failUnsubscribe)
        {
            var fsms = GameFrameworkEntry.GetModule<IFsmManager>();
            var procedures = GameFrameworkEntry.GetModule<IProcedureManager>();
            var source = new ShutdownProcedure();
            procedures.Initialize(fsms, source, new DestinationProcedure());
            procedures.StartProcedure<ShutdownProcedure>();
            var provider = LServices.Get<DependentProvider>();
            provider.ThrowOnNextStop = !failUnsubscribe;
            provider.ThrowOnNextUnsubscribe = failUnsubscribe;
            provider.RequireLiveWorldOnStop = true;
            try
            {
                Assert.Throws<AggregateException>(() => procedures.ForceChangedProcedure<DestinationProcedure>());
                Assert.That(procedures.CurrentProcedure, Is.SameAs(source));
                Assert.That(LServices.TryGet<WorldA>(out _), Is.True, "Retry must retain the provider's World dependency.");
                Assert.DoesNotThrow(() => procedures.ForceChangedProcedure<DestinationProcedure>());
                Assert.That(source.LeaveCount, Is.EqualTo(1));
                Assert.That(provider.StopCount, Is.EqualTo(failUnsubscribe ? 1 : 2));
                Assert.That(provider.UnsubscribeCount, Is.EqualTo(failUnsubscribe ? 2 : 1));
                Assert.That(LServices.TryGet<DependentProvider>(out _), Is.False);
                _application.Update(0, 0);
                Assert.That(provider.UpdateCount, Is.Zero);
            }
            finally
            {
                provider.ThrowOnNextStop = false;
                provider.ThrowOnNextUnsubscribe = false;
                provider.RequireLiveWorldOnStop = false;
                fsms.DestroyFsm<IProcedureManager>();
                _application.DisposeAllActiveProviders();
            }
        }

        [Test]
        public void ProcedureExitRetriesFailedWorldWithoutRepeatingCompletedCleanup()
        {
            var fsms = GameFrameworkEntry.GetModule<IFsmManager>();
            var procedures = GameFrameworkEntry.GetModule<IProcedureManager>();
            var source = new ShutdownProcedure();
            procedures.Initialize(fsms, source, new DestinationProcedure());
            procedures.StartProcedure<ShutdownProcedure>();
            var world = LServices.Get<WorldA>();
            var provider = LServices.Get<DependentProvider>();
            var resident = LServices.Get<MapResident>();
            world.ThrowOnNextDeinitialize = true;
            try
            {
                Assert.Throws<AggregateException>(() => procedures.ForceChangedProcedure<DestinationProcedure>());
                Assert.That(procedures.CurrentProcedure, Is.SameAs(source));
                Assert.That(resident.IsActive, Is.True);
                Assert.DoesNotThrow(() => procedures.ForceChangedProcedure<DestinationProcedure>());
                Assert.That(world.DeinitializeCount, Is.EqualTo(2));
                Assert.That(source.LeaveCount, Is.EqualTo(1));
                Assert.That(provider.StopCount, Is.EqualTo(1));
                Assert.That(procedures.CurrentProcedure, Is.TypeOf<DestinationProcedure>());
            }
            finally
            {
                world.ThrowOnNextDeinitialize = false;
                fsms.DestroyFsm<IProcedureManager>();
            }
        }

        [Test]
        public void ApplicationDisposeRetriesFailedProviderBeforeReleasingItsDependencies()
        {
            _application.PrepareWorldResidents(StateA);
            _application.TryRegisterProvider(StateA);
            _application.TryRegisterWorld(StateA);
            _application.CompleteWorldResidentTransition();
            var resident = LServices.Get<MapResident>();
            var provider = LServices.Get<DependentProvider>();
            provider.ThrowOnNextStop = true;
            provider.RequireLiveWorldOnStop = true;
            try
            {
                Assert.Throws<AggregateException>(_application.Dispose);
                Assert.That(LServices.Get<IApplicationShutdown>(), Is.SameAs(_application));
                Assert.That(resident.IsActive, Is.True);
                _application.Update(0, 0);
                Assert.That(provider.UpdateCount, Is.Zero, "An application awaiting cleanup retry must stop ticking.");
                Assert.DoesNotThrow(_application.Dispose);
                Assert.That(provider.StopCount, Is.EqualTo(2));
                Assert.That(resident.ShutdownCount, Is.EqualTo(1));
                Assert.That(LServices.TryGet<IApplicationShutdown>(out _), Is.False);
                Assert.DoesNotThrow(_application.Dispose);
                Assert.That(provider.StopCount, Is.EqualTo(2));
            }
            finally
            {
                provider.ThrowOnNextStop = false;
                provider.RequireLiveWorldOnStop = false;
                // 断言失败时也允许旧实现清理测试对象，不让 finally 遮蔽原始失败。
                if (!LServices.TryGet<IMapService>(out _)) LServices.Register<IMapService>(resident);
                _application.DisposeAllActiveProviders();
            }
        }

        [Test]
        public void RuntimeShutdownRetainsDependenciesUntilApplicationCleanupRetrySucceeds()
        {
            _application.PrepareWorldResidents(StateA);
            _application.TryRegisterProvider(StateA);
            _application.TryRegisterWorld(StateA);
            _application.CompleteWorldResidentTransition();
            var resident = LServices.Get<MapResident>();
            var provider = LServices.Get<DependentProvider>();
            provider.ThrowOnNextStop = true;
            provider.RequireLiveWorldOnStop = true;
            var gameObject = new UnityEngine.GameObject("Resident shutdown retry test");
            try
            {
                var runtime = gameObject.AddComponent<ResidentBootstrapTestApplication>();
                Assert.Throws<AggregateException>(() => runtime.StopApplication(ShutdownType.None));
                Assert.That(resident.IsActive, Is.True);
                Assert.That(_host.ActiveCount, Is.EqualTo(1));
                Assert.That(LServices.Get<IApplicationShutdown>(), Is.SameAs(_application));
                Assert.DoesNotThrow(() => runtime.StopApplication(ShutdownType.None));
                Assert.That(provider.StopCount, Is.EqualTo(2));
                Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            }
            finally
            {
                provider.ThrowOnNextStop = false;
                provider.RequireLiveWorldOnStop = false;
                if (!LServices.TryGet<IMapService>(out _)) LServices.Register<IMapService>(resident);
                _application.DisposeAllActiveProviders();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RuntimeShutdownReentryKeepsDependenciesAliveForTheOuterCleanup()
        {
            VerifyNestedRuntimeShutdown(true);
        }

        [Test]
        public void RuntimeShutdownRejectsAnApplicationDisposeAlreadyInProgress()
        {
            VerifyNestedRuntimeShutdown(false);
        }

        private void VerifyNestedRuntimeShutdown(bool runtimeOwnsShutdown)
        {
            _application.PrepareWorldResidents(StateA);
            _application.TryRegisterProvider(StateA);
            _application.TryRegisterWorld(StateA);
            _application.CompleteWorldResidentTransition();
            var resident = LServices.Get<MapResident>();
            var provider = LServices.Get<DependentProvider>();
            var gameObject = new UnityEngine.GameObject("Resident nested shutdown test");
            try
            {
                var runtime = gameObject.AddComponent<ResidentBootstrapTestApplication>();
                provider.Stopping = () =>
                {
                    if (runtimeOwnsShutdown)
                        Assert.DoesNotThrow(() => runtime.StopApplication(ShutdownType.None));
                    else
                        Assert.Throws<AggregateException>(() => runtime.StopApplication(ShutdownType.None));
                    Assert.That(resident.IsActive, Is.True, "Nested shutdown must preserve outer cleanup dependencies.");
                    Assert.That(LServices.TryGet<WorldA>(out _), Is.True);
                };
                if (runtimeOwnsShutdown)
                    Assert.DoesNotThrow(() => runtime.StopApplication(ShutdownType.None));
                else
                    Assert.DoesNotThrow(_application.Dispose);
                Assert.That(provider.StopCount, Is.EqualTo(1));
                Assert.That(resident.ShutdownCount, Is.EqualTo(1));
            }
            finally
            {
                provider.Stopping = null;
                if (!LServices.TryGet<IMapService>(out _)) LServices.Register<IMapService>(resident);
                _application.DisposeAllActiveProviders();
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RuntimeEntryContinuesAfterCommittedResidentShutdownFailure()
        {
            var fsms = GameFrameworkEntry.GetModule<IFsmManager>();
            var procedures = GameFrameworkEntry.GetModule<IProcedureManager>();
            var destination = new RuntimeDestinationProcedure();
            procedures.Initialize(fsms, new ShutdownProcedure(), destination);
            procedures.StartProcedure<ShutdownProcedure>();
            var resident = LServices.Get<MapResident>();
            resident.ThrowOnShutdown = true;
            try
            {
                Assert.DoesNotThrow(() => procedures.ForceChangedProcedure<RuntimeDestinationProcedure>());
                Assert.That(procedures.CurrentProcedure, Is.SameAs(destination));
                Assert.That(destination.Injected, Is.True);
                Assert.That(destination.Entered, Is.True);
                Assert.That(resident.IsActive, Is.False);
                Assert.That(resident.ShutdownCount, Is.EqualTo(1));
                Assert.That(_host.ActiveCount, Is.Zero);
            }
            finally { fsms.DestroyFsm<IProcedureManager>(); }
        }

        public sealed class ConflictingMapService : IMapService { }

        public sealed class ImmediateProcedure : HotfixProcedureBase
        {
            protected override int ProcedureState => StateA;
            protected override void OnEnterProcedure(IFsm<IProcedureManager> owner)
            {
                ChangeState<DestinationProcedure>(owner);
            }
        }

        public sealed class DestinationProcedure : HotfixProcedureBase
        {
            protected override int ProcedureState => StateB;
        }

        public sealed class ShutdownProcedure : HotfixProcedureBase
        {
            public int LeaveCount;
            protected override int ProcedureState => StateA;
            protected override void OnLeaveProcedure(IFsm<IProcedureManager> owner, bool shutdown)
            {
                LeaveCount++;
                Assert.That(LServices.Get<MapResident>().IsActive, Is.True);
                Assert.That(GetLinkWorld<WorldA>(), Is.SameAs(LServices.Get<WorldA>()));
                Calls.Add("procedure:leave");
            }
        }

        public sealed class RuntimeDestinationProcedure : LFramework.Runtime.Procedure.RuntimeBaseProcedure, IInjectable
        {
            public bool Injected;
            public bool Entered;
            public void Inject(IServiceResolver resolver) { Injected = true; }
            protected override void OnEnter(IFsm<IProcedureManager> owner)
            {
                base.OnEnter(owner);
                Entered = true;
            }
        }

        public sealed class RecordingApplicationShutdown : IApplicationShutdown
        {
            public void ShutdownApplication() { Calls.Add("application:shutdown"); }
        }

        public sealed class RuntimeDependency : GameFrameworkComponent
        {
            public override void ShutDown() { Calls.Add("runtime:shutdown"); }
        }

        public interface IMapService { }

        [WorldResident(typeof(WorldA), typeof(WorldB))]
        public sealed class MapResident : WorldResidentBase, IMapService
        {
            public int InitializeCount;
            public int ShutdownCount;
            public bool ThrowOnShutdown;
            protected override void OnInitialize() { InitializeCount++; }
            protected override void OnShutdown()
            {
                ShutdownCount++;
                Calls.Add("resident:stop");
                if (ThrowOnShutdown) throw new InvalidOperationException("resident shutdown test failure");
            }
        }

        [WorldResident(typeof(FailingWorld))]
        public sealed class FailedWorldResident : WorldResidentBase, IMapService
        {
            protected override void OnShutdown() { Calls.Add("failed-resident:shutdown"); }
        }

        [BelongTo(StateA)]
        public sealed class WorldA : WorldBase, IInjectable
        {
            public IMapService Resident;
            public bool ThrowOnNextDeinitialize;
            public int DeinitializeCount;
            public void Inject(IServiceResolver resolver) { Resident = resolver.Get<IMapService>(); }
            protected override void OnInitialized()
            {
                ThrowOnNextDeinitialize = false;
                DeinitializeCount = 0;
                Assert.That(Resident, Is.Not.Null);
            }
            protected override void OnDeInitialized()
            {
                DeinitializeCount++;
                if (ThrowOnNextDeinitialize)
                {
                    ThrowOnNextDeinitialize = false;
                    throw new InvalidOperationException("world deinitialize test failure");
                }
                Assert.That(LServices.Get<IMapService>(), Is.SameAs(Resident));
                Calls.Add("world:stop");
            }
        }

        [BelongTo(StateB)]
        public sealed class WorldB : WorldBase { }
        [BelongTo(StateD)]
        public sealed class WorldD : WorldBase { }
        [BelongTo(StateFail)]
        public sealed class FailingWorld : WorldBase
        {
            protected override void OnInitialized() { throw new InvalidOperationException("world initialization failure"); }
        }

        [BelongTo(StateA)]
        public sealed class DependentProvider : SystemProviderBase, IInjectable
        {
            public IMapService Resident;
            public bool ThrowOnNextStop;
            public bool ThrowOnNextUnsubscribe;
            public bool RequireLiveWorldOnStop;
            public int StopCount;
            public int UnsubscribeCount;
            public int UpdateCount;
            public Action Stopping;
            public void Inject(IServiceResolver resolver) { Resident = resolver.Get<IMapService>(); }
            public override void AwakeComponent()
            {
                ThrowOnNextStop = ThrowOnNextUnsubscribe = RequireLiveWorldOnStop = false;
                StopCount = UnsubscribeCount = UpdateCount = 0;
                Stopping = null;
            }
            public override void UpdateComponent(float elapsed, float realElapsed) { UpdateCount++; }
            protected override void UnSubscribe(EventComponent events)
            {
                UnsubscribeCount++;
                if (ThrowOnNextUnsubscribe)
                {
                    ThrowOnNextUnsubscribe = false;
                    throw new InvalidOperationException("provider unsubscribe test failure");
                }
            }
            public override void OnStop()
            {
                StopCount++;
                Stopping?.Invoke();
                if (RequireLiveWorldOnStop)
                {
                    Assert.That(LServices.TryGet<WorldA>(out _), Is.True);
                    Assert.That(((MapResident)Resident).IsActive, Is.True);
                }
                if (ThrowOnNextStop)
                {
                    ThrowOnNextStop = false;
                    throw new InvalidOperationException("provider stop test failure");
                }
                Assert.That(LServices.Get<IMapService>(), Is.SameAs(Resident));
                Calls.Add("provider:stop");
            }
        }
    }

    public sealed class ResidentBootstrapTestApplication : UnitySystemApplicationBehaviour
    {
        public void RunStartApplication() { StartApplication(); }
        protected override void RegisterComponents()
        {
            RegisterComponent(new WorldResidentIntegrationTests.RuntimeDependency());
            base.RegisterComponents();
        }
        protected override void ApplicationStarted() { }
    }
}
