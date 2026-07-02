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

        [HotfixComponent(typeof(ITestHotfixComponent))]
        public sealed class TestHotfixComponent : GameFrameworkComponent, ITestHotfixComponent
        {
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
    }
}
