using System;
using GameFramework;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests
{
    public sealed class LServicesTests
    {
        [SetUp]
        public void SetUp()
        {
            LServices.Reset();
            Injection.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            LServices.Reset();
            Injection.ClearAll();
        }

        [Test]
        public void CachedRootResolverSurvivesReset()
        {
            var resolver = LServices.Resolver;
            var service = new TestService("after-reset");

            LServices.Reset();
            LServices.Register(service);

            Assert.That(resolver.Get<TestService>(), Is.SameAs(service));
        }

        [Test]
        public void ScopeResolvesParentAndOverridesLocalServices()
        {
            var rootService = new TestService("root");
            var scopedService = new TestService("scope");
            var rootOnly = new RootOnlyService();

            LServices.Register(rootService);
            LServices.Register(rootOnly);

            using var scope = LServices.CreateScope();
            scope.Register(scopedService);

            Assert.That(scope.Get<TestService>(), Is.SameAs(scopedService));
            Assert.That(scope.Get<RootOnlyService>(), Is.SameAs(rootOnly));
            Assert.That(LServices.Get<TestService>(), Is.SameAs(rootService));
        }

        [Test]
        public void DisposingScopeRemovesDynamicRegistrationsWithoutTouchingParent()
        {
            var rootService = new TestService("root");
            var scopedService = new TestService("scope");

            LServices.Register(rootService);

            var scope = LServices.CreateScope();
            scope.Register(scopedService);

            Assert.That(scope.Get<TestService>(), Is.SameAs(scopedService));

            scope.Dispose();

            Assert.That(scope.TryGet<TestService>(out _), Is.False);
            Assert.That(LServices.Get<TestService>(), Is.SameAs(rootService));
        }

        [Test]
        public void ScopeDisposesOwnedServicesOnly()
        {
            var owned = new DisposableService();
            var external = new DisposableService();

            var scope = LServices.CreateScope();
            scope.RegisterOwned(owned);
            scope.Register(typeof(DisposableService), "external", external);

            scope.Dispose();

            Assert.That(owned.Disposed, Is.True);
            Assert.That(external.Disposed, Is.False);
        }

        [Test]
        public void ReplacingOwnedRegistrationDisposesPreviousInstance()
        {
            var first = new DisposableService();
            var second = new DisposableService();

            using var scope = LServices.CreateScope();
            scope.RegisterOwned(first);
            scope.RegisterOwned(second);

            Assert.That(first.Disposed, Is.True);
            Assert.That(second.Disposed, Is.False);
            Assert.That(scope.Get<DisposableService>(), Is.SameAs(second));
        }

        [Test]
        public void ReplacingOwnedRegistrationWithSameInstanceDoesNotDisposeIt()
        {
            var service = new DisposableService();

            using var scope = LServices.CreateScope();
            scope.RegisterOwned(service);
            scope.RegisterOwned(service);

            Assert.That(service.Disposed, Is.False);
            Assert.That(scope.Get<DisposableService>(), Is.SameAs(service));
        }

        [Test]
        public void ParentScopeDisposesChildScopes()
        {
            var childOwned = new DisposableService();

            var parent = LServices.CreateScope();
            var child = parent.CreateScope();
            child.RegisterOwned(childOwned);

            parent.Dispose();

            Assert.That(parent.IsDisposed, Is.True);
            Assert.That(child.IsDisposed, Is.True);
            Assert.That(childOwned.Disposed, Is.True);
        }

        [Test]
        public void LocalLookupDoesNotReadParentRegistrations()
        {
            var rootOnly = new RootOnlyService();
            LServices.Register(rootOnly);

            using var scope = LServices.CreateScope();

            Assert.That(scope.TryGet<RootOnlyService>(out var resolved), Is.True);
            Assert.That(resolved, Is.SameAs(rootOnly));
            Assert.That(scope.TryGetLocal<RootOnlyService>(out _), Is.False);
            Assert.That(scope.ContainsLocal(typeof(RootOnlyService)), Is.False);
            Assert.That(scope.Contains(typeof(RootOnlyService)), Is.True);
        }

        [Test]
        public void KeyedServicesSupportDynamicViewModelStyleRegistration()
        {
            var first = new TestService("first");
            var second = new TestService("second");

            using var scope = LServices.CreateScope();
            scope.Register(typeof(TestService), "first-view", first);
            scope.Register(typeof(TestService), "second-view", second);

            Assert.That(scope.Get<TestService>("first-view"), Is.SameAs(first));
            Assert.That(scope.Get<TestService>("second-view"), Is.SameAs(second));

            Assert.That(scope.Unregister(typeof(TestService), "first-view"), Is.True);
            Assert.That(scope.TryGet<TestService>("first-view", out _), Is.False);
            Assert.That(scope.Get<TestService>("second-view"), Is.SameAs(second));
        }

        [Test]
        public void InjectUsesGeneratedInterfaceFromScope()
        {
            var service = new TestService("runtime");
            var rootOnly = new RootOnlyService();
            using var scope = LServices.CreateScope();
            scope.Register(service);
            scope.Register(rootOnly);

            var target = new InterfaceInjectTarget();

            Injection.Inject(target, scope);

            Assert.That(target.Service, Is.SameAs(service));
            Assert.That(target.RootOnly, Is.SameAs(rootOnly));
        }

        [Test]
        public void InjectDoesNothingWithoutGeneratedInjector()
        {
            var service = new TestService("no-generated");
            using var scope = LServices.CreateScope();
            scope.Register(service);

            var target = new NoGeneratedInjectTarget();

            Injection.Inject(target, scope);

            Assert.That(target.Service, Is.Null);
        }

        [Test]
        public void GeneratedInterfaceCanResolveKeyedServicesWithoutContainer()
        {
            var defaultService = new TestService("default");
            var keyedService = new TestService("keyed");
            using var scope = LServices.CreateScope();
            scope.Register(defaultService);
            scope.Register(typeof(TestService), "view", keyedService);

            var target = new KeyedInterfaceInjectTarget();

            Injection.Inject(target, scope);

            Assert.That(target.Service, Is.SameAs(keyedService));
        }

        [Test]
        public void GeneratedInjectorCanBeUnregisteredForDynamicAssemblies()
        {
            var service = new TestService("generated");
            using var scope = LServices.CreateScope();
            scope.Register(service);
            Injection.Register<GeneratedInjectTarget>((target, resolver) => target.Assign(resolver.Get<TestService>()));

            var target = new GeneratedInjectTarget();
            Injection.Inject(target, scope);

            Assert.That(target.Service, Is.SameAs(service));

            Assert.That(Injection.UnregisterInjector<GeneratedInjectTarget>(), Is.True);

            var unregisteredTarget = new GeneratedInjectTarget();
            Injection.Inject(unregisteredTarget, scope);

            Assert.That(unregisteredTarget.Service, Is.Null);
        }

        [Test]
        public void ReflectionCacheMethodsAreNoOpsForGeneratedInjectors()
        {
            var service = new TestService("generated");
            using var scope = LServices.CreateScope();
            scope.Register(service);

            Assert.That(Injection.ClearReflectionCache(typeof(InterfaceInjectTarget)), Is.False);

            Injection.Register<GeneratedInjectTarget>((target, resolver) => target.Assign(resolver.Get<TestService>()));
            Injection.ClearReflectionCache();

            var target = new GeneratedInjectTarget();
            Injection.Inject(target, scope);

            Assert.That(target.Service, Is.SameAs(service));
        }

        [Test]
        public void GeneratedInjectorsCanBeUnregisteredByAssembly()
        {
            var service = new TestService("generated-assembly");
            using var scope = LServices.CreateScope();
            scope.Register(service);

            Injection.Register<GeneratedInjectTarget>((target, resolver) => target.Assign(resolver.Get<TestService>()));

            Assert.That(Injection.UnregisterInjectors(type => type.Assembly == typeof(GeneratedInjectTarget).Assembly),
                Is.GreaterThanOrEqualTo(1));

            var target = new GeneratedInjectTarget();
            Injection.Inject(target, scope);

            Assert.That(target.Service, Is.Null);
        }

        [Test]
        public void OptionalDependenciesCanBeMissing()
        {
            using var scope = LServices.CreateScope();
            var target = new OptionalInterfaceInjectTarget();

            Assert.DoesNotThrow(() => Injection.Inject(target, scope));
            Assert.That(target.Service, Is.Null);
        }

        [Test]
        public void GetDerivedInterfacesIgnoresGeneratedInjectableInterface()
        {
            var interfaceType = typeof(InterfaceSelectionProvider).GetDerivedInterfaces(
                typeof(ISystemProvider),
                typeof(IReference),
                typeof(IDisposable));

            Assert.That(interfaceType, Is.EqualTo(typeof(ITestProvider)));
        }

        [Test]
        public void GetDerivedInterfacesReturnsNullWhenMultipleBusinessInterfacesAreAmbiguous()
        {
            var interfaceType = typeof(AmbiguousInterfaceSelectionProvider).GetDerivedInterfaces(
                typeof(ISystemProvider),
                typeof(IReference),
                typeof(IDisposable));

            Assert.That(interfaceType, Is.Null);
        }

        [Test]
        public void GetDerivedInterfacesUsesBindInterfaceWhenMultipleBusinessInterfacesExist()
        {
            var interfaceType = typeof(BoundInterfaceSelectionProvider).GetDerivedInterfaces(
                typeof(ISystemProvider),
                typeof(IReference),
                typeof(IDisposable));

            Assert.That(interfaceType, Is.EqualTo(typeof(ISecondaryTestProvider)));
        }

        [Test]
        public void SystemProviderBaseHasGeneratedInjector()
        {
            Assert.That(typeof(IInjectable).IsAssignableFrom(typeof(SystemProviderBase)), Is.True);
            Assert.That(FindGeneratedInjectMethod(typeof(SystemProviderBase)), Is.Not.Null);
        }

        [Test]
        public void GenericAbstractBaseHasGeneratedInjector()
        {
            Assert.That(typeof(IInjectable).IsAssignableFrom(typeof(GenericInjectBase<>)), Is.True);
            Assert.That(FindGeneratedInjectMethod(typeof(GenericInjectBase<>)), Is.Not.Null);
        }

        [Test]
        public void GeneratedChildInjectorCallsBaseInjector()
        {
            var baseService = new BaseService();
            var childService = new ChildService();
            using var scope = LServices.CreateScope();
            scope.Register(baseService);
            scope.Register(childService);

            var target = new ChildGeneratedInjectTarget();

            ((IInjectable)target).Inject(scope);

            Assert.That(target.BaseService, Is.SameAs(baseService));
            Assert.That(target.ChildService, Is.SameAs(childService));
        }

        private static System.Reflection.MethodInfo FindGeneratedInjectMethod(Type type)
        {
            return type.GetMethod(
                "__LFrameworkInjectGenerated",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        }

        private sealed class TestService
        {
            public TestService(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        private sealed class RootOnlyService
        {
        }

        private sealed class DisposableService : System.IDisposable
        {
            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }

        private sealed class InterfaceInjectTarget : IInjectable
        {
            private TestService _service;

            private RootOnlyService RootOnlyService { get; set; }

            public TestService Service => _service;

            public RootOnlyService RootOnly => RootOnlyService;

            void IInjectable.Inject(IServiceResolver resolver)
            {
                _service = resolver.Get<TestService>();
                RootOnlyService = resolver.Get<RootOnlyService>();
            }
        }

        private sealed class NoGeneratedInjectTarget
        {
            private TestService ServiceProperty { get; }

            public TestService Service => ServiceProperty;
        }

        private sealed class GeneratedInjectTarget
        {
            public TestService Service { get; private set; }

            public void Assign(TestService service)
            {
                Service = service;
            }
        }

        private sealed class KeyedInterfaceInjectTarget : IInjectable
        {
            private TestService _service;

            public TestService Service => _service;

            void IInjectable.Inject(IServiceResolver resolver)
            {
                _service = resolver.Get<TestService>("view");
            }
        }

        private sealed class OptionalInterfaceInjectTarget : IInjectable
        {
            private MissingService _service;

            public MissingService Service => _service;

            void IInjectable.Inject(IServiceResolver resolver)
            {
                resolver.TryGet(out _service);
            }
        }

        private sealed class MissingService
        {
        }

        private sealed class InterfaceSelectionProvider : IInjectable, ITestProvider
        {
            void IInjectable.Inject(IServiceResolver resolver)
            {
            }

            public void AwakeComponent()
            {
            }

            public void SubscribeEvent()
            {
            }

            public void SetUp()
            {
            }

            public void UnSubscribeEvent()
            {
            }

            public void Clear()
            {
            }
        }

        private sealed class AmbiguousInterfaceSelectionProvider :
            IInjectable,
            ITestProvider,
            ISecondaryTestProvider
        {
            void IInjectable.Inject(IServiceResolver resolver)
            {
            }

            public void AwakeComponent()
            {
            }

            public void SubscribeEvent()
            {
            }

            public void SetUp()
            {
            }

            public void UnSubscribeEvent()
            {
            }

            public void Clear()
            {
            }
        }

        [BindInterface(typeof(ISecondaryTestProvider))]
        private sealed class BoundInterfaceSelectionProvider :
            IInjectable,
            ITestProvider,
            ISecondaryTestProvider
        {
            void IInjectable.Inject(IServiceResolver resolver)
            {
            }

            public void AwakeComponent()
            {
            }

            public void SubscribeEvent()
            {
            }

            public void SetUp()
            {
            }

            public void UnSubscribeEvent()
            {
            }

            public void Clear()
            {
            }
        }
    }

    internal interface ITestProvider : ISystemProvider
    {
    }

    internal interface ISecondaryTestProvider : ISystemProvider
    {
    }

    internal sealed class GenericService
    {
    }

    internal abstract partial class GenericInjectBase<T>
    {
        [Inject] private GenericService GenericService { get; set; }

        public GenericService Service => GenericService;
    }

    internal sealed class BaseService
    {
    }

    internal sealed class ChildService
    {
    }

    internal partial class BaseGeneratedInjectTarget
    {
        [Inject] private BaseService BaseServiceProperty { get; set; }

        public BaseService BaseService => BaseServiceProperty;
    }

    internal partial class ChildGeneratedInjectTarget : BaseGeneratedInjectTarget
    {
        [Inject] private ChildService ChildServiceProperty { get; set; }

        public ChildService ChildService => ChildServiceProperty;
    }
}
