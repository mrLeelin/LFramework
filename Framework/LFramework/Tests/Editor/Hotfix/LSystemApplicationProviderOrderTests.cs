using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework;
using LFramework.Hotfix;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Hotfix
{
    public sealed class LSystemApplicationProviderOrderTests
    {
        private const int InitialState = 7201;
        private const int NextState = 7202;
        private const int FailingState = 7203;
        private const int EqualPriorityState = 7204;
        private const int StopRetryState = 7205;
        private static readonly List<string> Calls = new();
        private LSystemApplication _application;

        [SetUp]
        public void SetUp()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            LSystemApplication.ClearReflectionCache();
            Calls.Clear();
            SingletonManager.AddSingleton(new LFrameworkAspect());
            LServices.Register(new EventComponent());
        }

        [TearDown]
        public void TearDown()
        {
            _application?.Dispose();
            _application = null;
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            LSystemApplication.ClearReflectionCache();
            Calls.Clear();
        }

        [Test]
        public void EveryInitializationPhaseUsesDescendingPriorityAndStableRegistrationOrder()
        {
            CreateApplication();

            _application.TryRegisterProvider(InitialState);

            var expected = InitialOrder();
            AssertPhase("Inject", expected);
            AssertPhase("Awake", expected);
            AssertPhase("Subscribe", expected);
            AssertPhase("Setup", expected);
            Assert.That(Calls, Is.EqualTo(new[] { "Inject", "Awake", "Subscribe", "Setup" }
                .SelectMany(phase => expected.Select(provider => $"{phase}:{provider}"))));
        }

        [Test]
        public void UpdateUsesGlobalPriorityOrderAndPreservesElapsedTime()
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);
            Calls.Clear();

            _application.Update(0.25f, 0.5f);

            AssertPhase("Update", InitialOrder());
            var singleProvider = LServices.Get<HighSingleProvider>();
            Assert.That(singleProvider.LastElapsedSeconds, Is.EqualTo(0.25f));
            Assert.That(singleProvider.LastRealElapsedSeconds, Is.EqualTo(0.5f));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SyncUsesGlobalPriorityOrderAcrossSingleAndListData(bool isLogin)
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);
            Calls.Clear();
            var commonData = CreateCommonData();

            _application.Sync(commonData, isLogin);

            AssertPhase("Sync", InitialOrder());
            var singleProvider = LServices.Get<HighSingleProvider>();
            var listProvider = LServices.Get<MiddleListProvider>();
            Assert.That(singleProvider.LastData, Is.SameAs(commonData.Single));
            Assert.That(listProvider.LastData, Is.SameAs(commonData.Items));
            Assert.That(singleProvider.LastIsLogin, Is.EqualTo(isLogin));
            Assert.That(listProvider.LastIsLogin, Is.EqualTo(isLogin));
        }

        [Test]
        public void EqualPrioritiesPreserveRegistrationOrderForLargeRegistrationBatch()
        {
            var markerTypes = new[]
            {
                typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint),
                typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal), typeof(char),
                typeof(bool), typeof(string), typeof(DateTime), typeof(TimeSpan), typeof(Guid),
                typeof(SingleData), typeof(ListData), typeof(CommonData)
            };
            CreateApplication(markerTypes.Select(marker => typeof(EqualPriorityProvider<>).MakeGenericType(marker)).ToArray());

            _application.TryRegisterProvider(EqualPriorityState);
            _application.Update(0, 0);
            _application.Sync(CreateCommonData(), false);

            var expected = markerTypes.Select(marker => marker.Name).ToArray();
            AssertPhase("Inject", expected);
            AssertPhase("Awake", expected);
            AssertPhase("Subscribe", expected);
            AssertPhase("Setup", expected);
            AssertPhase("Update", expected);
            AssertPhase("Sync", expected);
        }

        [Test]
        public void LaterProcedureMergesWithForeverProvidersAndReregistrationDoesNotDuplicateCallbacks()
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);
            _application.TryUnRegisterProvider(InitialState);
            Calls.Clear();

            _application.TryRegisterProvider(NextState);

            var newProviderOrder = new[]
            {
                nameof(NextHighSingleProvider), nameof(NextEqualSingleProvider), nameof(NextLowSingleProvider)
            };
            AssertPhase("Inject", newProviderOrder);
            AssertPhase("Awake", newProviderOrder);
            AssertPhase("Subscribe", newProviderOrder);
            AssertPhase("Setup", newProviderOrder);
            Calls.Clear();
            _application.Update(0, 0);
            _application.Sync(CreateCommonData(), false);
            var mergedOrder = new[]
            {
                nameof(NextHighSingleProvider), nameof(MiddleListProvider),
                nameof(NextEqualSingleProvider), nameof(NextLowSingleProvider)
            };
            AssertPhase("Update", mergedOrder);
            AssertPhase("Sync", mergedOrder);

            _application.TryUnRegisterProvider(NextState);
            _application.TryRegisterProvider(InitialState);
            _application.TryRegisterProvider(InitialState);
            Calls.Clear();
            _application.Update(0, 0);
            _application.Sync(CreateCommonData(), false);

            AssertPhase("Update", InitialOrder());
            AssertPhase("Sync", InitialOrder());
        }

        [Test]
        public void UnregisterRemovesCurrentProvidersButPreservesForeverUpdatesAndSync()
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);
            Calls.Clear();

            _application.TryUnRegisterProvider(InitialState);

            var removedOrder = new[]
            {
                nameof(HighSingleProvider), nameof(EqualSingleProvider), nameof(LowSingleProvider)
            };
            AssertPhase("Unsubscribe", removedOrder);
            AssertPhase("Stop", removedOrder);
            Calls.Clear();
            _application.Update(0, 0);
            _application.Sync(CreateCommonData(), false);

            AssertPhase("Update", nameof(MiddleListProvider));
            AssertPhase("Sync", nameof(MiddleListProvider));
            Assert.That(LServices.TryGet<LowSingleProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<HighSingleProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<EqualSingleProvider>(out _), Is.False);
        }

        [Test]
        public void FailedRegistrationLeavesExistingGlobalOrderWithoutGhostUpdatesOrSync()
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);

            Assert.Throws<InvalidOperationException>(() => _application.TryRegisterProvider(FailingState));

            Calls.Clear();
            _application.Update(0, 0);
            _application.Sync(CreateCommonData(), false);

            AssertPhase("Update", InitialOrder());
            AssertPhase("Sync", InitialOrder());
            Assert.That(LServices.TryGet<HealthyRollbackProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<FailingProvider>(out _), Is.False);
        }

        [Test]
        public void FailedRegistrationKeepsRollbackFailureTrackedForDisposeRetry()
        {
            CreateApplication(typeof(RollbackStopFailsOnceProvider), typeof(FailingProvider));

            var exception = Assert.Throws<AggregateException>(
                () => _application.TryRegisterProvider(FailingState));

            Assert.That(exception.InnerExceptions.Count, Is.EqualTo(2));
            Assert.That(exception.InnerExceptions[0].Message, Does.Contain("ordering rollback test"));
            Assert.That(exception.InnerExceptions[1].Message, Does.Contain("rollback stop retry test"));
            Assert.That(RollbackStopFailsOnceProvider.LastInstance.StopCallCount, Is.EqualTo(1));
            Assert.DoesNotThrow(() => _application.DisposeAllActiveProviders());
            Assert.That(RollbackStopFailsOnceProvider.LastInstance.StopCallCount, Is.EqualTo(2));
        }

        [Test]
        public void DisposeUsesGlobalOrderAndRemovesAllUpdatesAndSync()
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);
            Calls.Clear();

            _application.Dispose();

            AssertPhase("Unsubscribe", InitialOrder());
            AssertPhase("Stop", InitialOrder());
            Calls.Clear();
            _application.Update(0, 0);
            _application.Sync(CreateCommonData(), false);
            _application.Dispose();

            Assert.That(Calls, Is.Empty);
            Assert.That(LServices.TryGet<MiddleListProvider>(out _), Is.False);
        }

        [Test]
        public void DisposeRetriesProviderWhoseFirstStopFailsWithoutLeavingUpdatesOrSync()
        {
            CreateApplication(typeof(StopFailsOnceProvider));
            _application.TryRegisterProvider(StopRetryState);
            var provider = LServices.Get<StopFailsOnceProvider>();
            provider.ThrowOnNextStop = true;

            try
            {
                Assert.Throws<InvalidOperationException>(() => _application.DisposeAllActiveProviders());
                Assert.That(provider.StopCallCount, Is.EqualTo(1));

                _application.DisposeAllActiveProviders();

                Assert.That(provider.StopCallCount, Is.EqualTo(2));
                Calls.Clear();
                _application.Update(0, 0);
                _application.Sync(CreateCommonData(), false);
                Assert.That(Calls, Is.Empty);
                Assert.That(LServices.TryGet<StopFailsOnceProvider>(out _), Is.False);
            }
            finally
            {
                _application.DisposeAllActiveProviders();
                if (provider.StopCallCount == 1)
                {
                    ReferencePool.Release(provider);
                }
            }
        }

        [Test]
        public void SyncSkipsNullPropertiesAndEmptyListsWithoutChangingSingleDataDispatch()
        {
            CreateApplication();
            _application.TryRegisterProvider(InitialState);
            Calls.Clear();

            _application.Sync(new CommonData(), false);
            _application.Sync(new CommonData { Items = new List<ListData>() }, true);

            Assert.That(Calls, Is.Empty);

            _application.Sync(new CommonData { Single = new SingleData(), Items = new List<ListData>() }, false);

            AssertPhase("Sync", nameof(HighSingleProvider), nameof(EqualSingleProvider), nameof(LowSingleProvider));
            Calls.Clear();
            _application.Sync(new CommonData { Items = new List<ListData> { new() } }, true);

            AssertPhase("Sync", nameof(MiddleListProvider));
        }

        [Test]
        public void SyncPropertyCacheIncludesOriginDtoType()
        {
            CreateApplication(typeof(LowSingleProvider));
            _application.TryRegisterProvider(InitialState);
            var first = new SingleData();
            var second = new SingleData();

            _application.Sync(new CommonData { Single = first }, false);
            _application.Sync(new AlternateCommonData { Value = second }, false);

            Assert.That(LServices.Get<LowSingleProvider>().LastData, Is.SameAs(second));
        }

        [Test]
        public void SyncFailureInOneProviderDoesNotSkipLaterProviders()
        {
            CreateApplication(typeof(ThrowingSyncProvider), typeof(LowSingleProvider));
            _application.TryRegisterProvider(InitialState);
            var payload = new SingleData();

            _application.Sync(new CommonData { Single = payload }, false);

            Assert.That(LServices.Get<LowSingleProvider>().LastData, Is.SameAs(payload));
        }

        private void CreateApplication(params Type[] types)
        {
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            if (types.Length == 0)
            {
                types = new[]
                {
                    typeof(LowSingleProvider), typeof(MiddleListProvider), typeof(HighSingleProvider),
                    typeof(EqualSingleProvider), typeof(NextLowSingleProvider), typeof(NextEqualSingleProvider),
                    typeof(NextHighSingleProvider), typeof(HealthyRollbackProvider), typeof(FailingProvider)
                };
            }
            var typeMap = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                typeMap.Add(type.FullName, type);
            }

            var parseAttributes = typeof(HotfixComponent).GetMethod(
                "ParseAttributes", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(parseAttributes, Is.Not.Null);
            parseAttributes.Invoke(hotfixComponent, new object[] { typeMap });
            _application = new LSystemApplication();
        }

        private static void AssertPhase(string phase, params string[] expected)
        {
            var actualCalls = Calls.Where(call => call.StartsWith(phase + ":", StringComparison.Ordinal)).ToArray();
            var expectedCalls = expected.Select(provider => $"{phase}:{provider}").ToArray();
            Assert.That(actualCalls, Is.EqualTo(expectedCalls),
                $"{phase}; expected=[{string.Join(", ", expectedCalls)}]; actual=[{string.Join(", ", actualCalls)}]");
        }

        private static string[] InitialOrder()
        {
            return new[]
            {
                nameof(HighSingleProvider), nameof(MiddleListProvider),
                nameof(EqualSingleProvider), nameof(LowSingleProvider)
            };
        }

        private static CommonData CreateCommonData()
        {
            return new CommonData { Single = new SingleData(), Items = new List<ListData> { new() } };
        }

        public sealed class SingleData
        {
        }

        public sealed class ListData
        {
        }

        public sealed class CommonData
        {
            public SingleData Single { get; set; }
            public List<ListData> Items { get; set; }
        }

        public sealed class AlternateCommonData
        {
            public SingleData Value { get; set; }
        }

        public abstract class RecordingProvider : SystemProviderBase, IInjectable
        {
            public object LastData { get; private set; }
            public bool LastIsLogin { get; private set; }
            public float LastElapsedSeconds { get; private set; }
            public float LastRealElapsedSeconds { get; private set; }
            protected virtual string ProviderName => GetType().Name;

            void IInjectable.Inject(IServiceResolver resolver) => RecordInjection(resolver.Get<EventComponent>());

            public void RecordInjection(EventComponent eventComponent)
            {
                EventComponent = eventComponent;
                Record("Inject");
            }

            public override void AwakeComponent() => Record("Awake");
            public override void SetUp() => Record("Setup");
            protected override void Subscribe(EventComponent eventComponent) => Record("Subscribe");
            protected override void UnSubscribe(EventComponent eventComponent) => Record("Unsubscribe");

            public override void OnStop()
            {
                LastData = null;
                LastIsLogin = false;
                LastElapsedSeconds = 0;
                LastRealElapsedSeconds = 0;
                Record("Stop");
            }

            public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
            {
                LastElapsedSeconds = elapseSeconds;
                LastRealElapsedSeconds = realElapseSeconds;
                Record("Update");
            }

            protected void RecordSync(object serverData, bool isBasedLogin)
            {
                LastData = serverData;
                LastIsLogin = isBasedLogin;
                Record("Sync");
            }

            private void Record(string phase) => Calls.Add($"{phase}:{ProviderName}");
        }

        public abstract class SingleProvider : RecordingProvider, ISyncServerData<SingleData>
        {
            public void SyncServerData(SingleData serverData, bool isBasedLogin) => RecordSync(serverData, isBasedLogin);
        }

        [BelongTo(EqualPriorityState, sort: 50)]
        public sealed class EqualPriorityProvider<TMarker> : SingleProvider
        {
            protected override string ProviderName => typeof(TMarker).Name;
        }

        [BelongTo(InitialState)]
        public sealed class LowSingleProvider : SingleProvider
        {
        }

        [BelongTo(InitialState, sort: 200)]
        public sealed class ThrowingSyncProvider : RecordingProvider, ISyncServerData<SingleData>
        {
            public void SyncServerData(SingleData serverData, bool isBasedLogin)
            {
                throw new InvalidOperationException("Provider sync isolation test.");
            }
        }

        [BelongTo(InitialState, ProviderLifeCycle.Forever, 50)]
        public sealed class MiddleListProvider : RecordingProvider, ISyncServerListData<ListData>
        {
            public void SyncServerData(IList<ListData> serverData, bool isBasedLogin) => RecordSync(serverData, isBasedLogin);
        }

        [BelongTo(InitialState, sort: 100)]
        public sealed class HighSingleProvider : SingleProvider
        {
        }

        [BelongTo(InitialState, sort: 50)]
        public sealed class EqualSingleProvider : SingleProvider
        {
        }

        [BelongTo(NextState, sort: 200)]
        public sealed class NextHighSingleProvider : SingleProvider
        {
        }

        [BelongTo(NextState, sort: 50)]
        public sealed class NextEqualSingleProvider : SingleProvider
        {
        }

        [BelongTo(NextState, sort: -10)]
        public sealed class NextLowSingleProvider : SingleProvider
        {
        }

        [BelongTo(FailingState)]
        public sealed class HealthyRollbackProvider : SingleProvider
        {
        }

        [BelongTo(StopRetryState)]
        public sealed class StopFailsOnceProvider : SingleProvider
        {
            public int StopCallCount { get; private set; }
            public bool ThrowOnNextStop { get; set; }

            public override void AwakeComponent()
            {
                StopCallCount = 0;
                ThrowOnNextStop = false;
                base.AwakeComponent();
            }

            public override void OnStop()
            {
                StopCallCount++;
                if (ThrowOnNextStop)
                {
                    ThrowOnNextStop = false;
                    throw new InvalidOperationException("Provider stop retry test.");
                }

                base.OnStop();
            }
        }

        [BelongTo(FailingState, sort: 100)]
        public sealed class RollbackStopFailsOnceProvider : SingleProvider
        {
            public static RollbackStopFailsOnceProvider LastInstance { get; private set; }
            public int StopCallCount { get; private set; }

            public override void AwakeComponent()
            {
                LastInstance = this;
                StopCallCount = 0;
                base.AwakeComponent();
            }

            public override void OnStop()
            {
                StopCallCount++;
                if (StopCallCount == 1)
                {
                    throw new InvalidOperationException("Provider rollback stop retry test.");
                }

                base.OnStop();
            }
        }

        [BelongTo(FailingState, sort: 250)]
        public sealed class FailingProvider : SingleProvider
        {
            public override void SetUp()
            {
                base.SetUp();
                throw new InvalidOperationException("Provider ordering rollback test.");
            }
        }
    }
}
