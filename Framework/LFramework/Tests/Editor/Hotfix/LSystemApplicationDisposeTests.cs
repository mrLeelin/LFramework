using System;
using System.Collections.Generic;
using System.Reflection;
using LFramework.Hotfix;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Hotfix
{
    public sealed class LSystemApplicationDisposeTests
    {
        private const int ProcedureState = 7101;
        private const int FailingProcedureState = 7102;
        private const int FailingWorldProcedureState = 7103;
        private const int FailingSubscribeProcedureState = 7104;

        [SetUp]
        public void SetUp()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            SingletonManager.AddSingleton(new LFrameworkAspect());
        }

        [TearDown]
        public void TearDown()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
        }

        [Test]
        public void DisposeUnregistersHotfixComponentProviderAndWorldServices()
        {
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            LServices.Register(new EventComponent());
            ConfigureHotfixTypes(
                hotfixComponent,
                typeof(TestHotfixComponent),
                typeof(TestProvider),
                typeof(TestWorld));

            var application = new LSystemApplication();
            application.RegisterHotfixComponents(hotfixComponent);
            application.TryRegisterProvider(ProcedureState);
            application.TryRegisterWorld(ProcedureState);

            Assert.That(LServices.TryGet<ITestHotfixComponent>(out _), Is.True);
            Assert.That(LServices.TryGet<ITestProvider>(out _), Is.True);
            Assert.That(LServices.TryGet<ITestWorld>(out _), Is.True);

            application.Dispose();

            Assert.That(LServices.TryGet<ITestHotfixComponent>(out _), Is.False);
            Assert.That(LServices.TryGet<ITestProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<ITestWorld>(out _), Is.False);
            Assert.That(LServices.TryGet<TestHotfixComponent>(out _), Is.False);
            Assert.That(LServices.TryGet<TestProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<TestWorld>(out _), Is.False);
        }

        [Test]
        public void TryRegisterProviderRollsBackNewServicesWhenLifecycleFails()
        {
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            LServices.Register(new EventComponent());
            ConfigureHotfixTypes(
                hotfixComponent,
                typeof(HealthyProvider),
                typeof(FailingProvider));

            var application = new LSystemApplication();

            Assert.Throws<InvalidOperationException>(() =>
                application.TryRegisterProvider(FailingProcedureState));

            Assert.That(LServices.TryGet<IHealthyProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<IFailingProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<HealthyProvider>(out _), Is.False);
            Assert.That(LServices.TryGet<FailingProvider>(out _), Is.False);

            application.Dispose();
        }

        [Test]
        public void TryRegisterProviderRollsBackPartialSubscriptionWhenSubscribeFails()
        {
            PartiallySubscribedProvider.Reset();
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            LServices.Register(new EventComponent());
            ConfigureHotfixTypes(hotfixComponent, typeof(PartiallySubscribedProvider));

            var application = new LSystemApplication();

            Assert.Throws<InvalidOperationException>(() =>
                application.TryRegisterProvider(FailingSubscribeProcedureState));

            Assert.That(PartiallySubscribedProvider.HasSubscription, Is.False);
            Assert.That(LServices.TryGet<PartiallySubscribedProvider>(out _), Is.False);

            application.Dispose();
        }

        [Test]
        public void TryRegisterWorldRollsBackServicesWhenInitializationFails()
        {
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            LServices.Register(new EventComponent());
            ConfigureHotfixTypes(hotfixComponent, typeof(FailingWorld));

            var application = new LSystemApplication();

            Assert.Throws<InvalidOperationException>(() =>
                application.TryRegisterWorld(FailingWorldProcedureState));

            Assert.That(LServices.TryGet<IFailingWorld>(out _), Is.False);
            Assert.That(LServices.TryGet<FailingWorld>(out _), Is.False);

            application.Dispose();
        }

        [Test]
        public void UnSubscribeFailureCanBeRetried()
        {
            var provider = new FailsOnceUnsubscribeProvider();
            provider.SetEventComponent(new EventComponent());
            provider.SubscribeEvent();

            Assert.Throws<InvalidOperationException>(() => provider.UnSubscribeEvent());
            Assert.DoesNotThrow(() => provider.UnSubscribeEvent());
            Assert.That(provider.UnsubscribeCallCount, Is.EqualTo(2));
        }

        [Test]
        public void ApplicationLifecycleCallbacksReachRegisteredHotfixComponents()
        {
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            ConfigureHotfixTypes(hotfixComponent, typeof(TestHotfixComponent));

            var application = new LSystemApplication();
            application.RegisterHotfixComponents(hotfixComponent);

            Assert.That(LServices.TryGet<IApplicationLifecycle>(out var lifecycle), Is.True);
            lifecycle.OnApplicationFocus(false);
            lifecycle.OnApplicationPause(true);
            lifecycle.OnApplicationQuit();

            Assert.That(LServices.TryGet<ITestHotfixComponent>(out var service), Is.True);
            var component = (TestHotfixComponent)service;
            Assert.That(component.HasFocus, Is.False);
            Assert.That(component.IsPaused, Is.True);
            Assert.That(component.QuitCallCount, Is.EqualTo(1));

            application.Dispose();

            Assert.That(LServices.TryGet<IApplicationLifecycle>(out _), Is.False);
        }

        private static void ConfigureHotfixTypes(HotfixComponent hotfixComponent, params Type[] types)
        {
            var typeMap = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                typeMap.Add(type.FullName, type);
            }

            var parseAttributes = typeof(HotfixComponent).GetMethod(
                "ParseAttributes",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(parseAttributes, Is.Not.Null);
            parseAttributes.Invoke(hotfixComponent, new object[] { typeMap });
        }

        public interface ITestHotfixComponent
        {
        }

        public interface ITestProvider : ISystemProvider
        {
        }

        public interface ITestWorld : IWorld
        {
        }

        public interface IHealthyProvider : ISystemProvider
        {
        }

        public interface IFailingProvider : ISystemProvider
        {
        }

        public interface IFailingWorld : IWorld
        {
        }

        [HotfixComponent(typeof(ITestHotfixComponent))]
        public sealed class TestHotfixComponent : GameFrameworkComponent, ITestHotfixComponent
        {
            public bool HasFocus { get; private set; } = true;
            public bool IsPaused { get; private set; }
            public int QuitCallCount { get; private set; }

            public override void RuntimeOnApplicationFocus(bool hasFocus)
            {
                HasFocus = hasFocus;
            }

            public override void RuntimeOnApplicationPause(bool pauseStatus)
            {
                IsPaused = pauseStatus;
            }

            public override void RuntimeOnApplicationQuit()
            {
                QuitCallCount++;
            }
        }

        [BelongTo(ProcedureState)]
        public sealed class TestProvider : SystemProviderBase, ITestProvider
        {
        }

        [BelongTo(ProcedureState)]
        public sealed class TestWorld : WorldBase, ITestWorld
        {
        }

        [BelongTo(FailingProcedureState, sort: 10)]
        public sealed class HealthyProvider : SystemProviderBase, IHealthyProvider
        {
        }

        [BelongTo(FailingProcedureState)]
        public sealed class FailingProvider : SystemProviderBase, IFailingProvider
        {
            public override void SetUp()
            {
                throw new InvalidOperationException("Provider setup failure test.");
            }
        }

        [BelongTo(FailingSubscribeProcedureState)]
        public sealed class PartiallySubscribedProvider : SystemProviderBase
        {
            public static bool HasSubscription { get; private set; }

            public static void Reset()
            {
                HasSubscription = false;
            }

            protected override void Subscribe(EventComponent eventComponent)
            {
                HasSubscription = true;
                throw new InvalidOperationException("Provider subscribe failure test.");
            }

            protected override void UnSubscribe(EventComponent eventComponent)
            {
                HasSubscription = false;
            }
        }

        [BelongTo(FailingWorldProcedureState)]
        public sealed class FailingWorld : WorldBase, IFailingWorld
        {
            protected override void OnInitialized()
            {
                throw new InvalidOperationException("World initialization failure test.");
            }
        }

        private sealed class FailsOnceUnsubscribeProvider : SystemProviderBase
        {
            public int UnsubscribeCallCount { get; private set; }

            public void SetEventComponent(EventComponent eventComponent)
            {
                EventComponent = eventComponent;
            }

            protected override void UnSubscribe(EventComponent eventComponent)
            {
                UnsubscribeCallCount++;
                if (UnsubscribeCallCount == 1)
                {
                    throw new InvalidOperationException("Unsubscribe failure test.");
                }
            }
        }
    }
}
