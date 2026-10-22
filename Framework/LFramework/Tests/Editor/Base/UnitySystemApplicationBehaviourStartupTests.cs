using System;
using System.Reflection;
using LFramework.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Base
{
    public sealed class UnitySystemApplicationBehaviourStartupTests
    {
        [SetUp]
        public void SetUp()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
        }

        [Test]
        public void StartApplicationCleansRegisteredServicesWhenComponentStartupFails()
        {
            var gameObject = new GameObject("Startup Failure Test");
            var application = gameObject.AddComponent<StartupRollbackTestApplicationBehaviour>();

            application.RunStartApplication();

            Assert.That(application.Component.ShutDownCalled, Is.True);
            Assert.That(LServices.TryGet<TestStartupComponent>(out _), Is.False);
            Assert.That(LFrameworkAspect.Instance, Is.Null);

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void DuplicateBootstrapKeepsExistingAspectAndServices()
        {
            var existingAspect = new LFrameworkAspect();
            SingletonManager.AddSingleton(existingAspect);
            var service = new BootstrapService();
            LServices.Register(service);
            var gameObject = new GameObject("Duplicate Bootstrap Test");
            var application = gameObject.AddComponent<StartupRollbackTestApplicationBehaviour>();

            application.RunStartApplication();

            Assert.That(LFrameworkAspect.Instance, Is.SameAs(existingAspect));
            Assert.That(LServices.Get<BootstrapService>(), Is.SameAs(service));

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void GenericSingletonRegistrationRejectsDuplicateBeforeConstruction()
        {
            ConstructorCountingSingleton.ConstructionCount = 0;
            SingletonManager.AddSingleton<ConstructorCountingSingleton>();

            Assert.Throws<Exception>(() => SingletonManager.AddSingleton<ConstructorCountingSingleton>());

            Assert.That(ConstructorCountingSingleton.ConstructionCount, Is.EqualTo(1));
        }

        [Test]
        public void SingletonRegistrationRejectsSameTypeReentryBeforeBaseRegisterAndAllowsRetry()
        {
            ReentrantRegistrationSingleton.ConstructionCount = 0;
            ReentrantRegistrationSingleton.AttemptReentry = true;

            var exception = Assert.Throws<Exception>(
                () => SingletonManager.AddSingleton<ReentrantRegistrationSingleton>());

            Assert.That(exception.Message, Does.Contain("already exist singleton"));
            Assert.That(ReentrantRegistrationSingleton.ConstructionCount, Is.EqualTo(1));
            Assert.That(ReentrantRegistrationSingleton.Instance, Is.Null);

            ReentrantRegistrationSingleton.AttemptReentry = false;
            var retry = SingletonManager.AddSingleton<ReentrantRegistrationSingleton>();
            Assert.That(ReentrantRegistrationSingleton.Instance, Is.SameAs(retry));
            Assert.That(ReentrantRegistrationSingleton.ConstructionCount, Is.EqualTo(2));
        }

        [Test]
        public void SingletonRegistrationFailureCleansInstanceAndAllowsRetry()
        {
            var registrationError = new InvalidOperationException("Singleton registration test.");
            var failed = new RegistrationFailureSingleton { RegistrationError = registrationError };

            var exception = Assert.Throws<InvalidOperationException>(() => SingletonManager.AddSingleton(failed));

            Assert.That(exception, Is.SameAs(registrationError));
            Assert.That(RegistrationFailureSingleton.Instance, Is.Null);
            Assert.That(failed.DisposeCount, Is.EqualTo(1));
            Assert.That(failed.HadNoInstanceDuringDispose, Is.True);
            Assert.That(failed.IsDisposed(), Is.True);
            SingletonManager.Update(0f, 0f);
            Assert.That(failed.UpdateCount, Is.Zero);
            Assert.Throws<ObjectDisposedException>(() => SingletonManager.AddSingleton(failed));

            var retry = SingletonManager.AddSingleton<RegistrationFailureSingleton>();
            Assert.That(RegistrationFailureSingleton.Instance, Is.SameAs(retry));
            SingletonManager.Close();
            Assert.That(retry.DisposeCount, Is.EqualTo(1));
            Assert.That(failed.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void SingletonRollbackFailurePreservesBothErrorsAndAllowsRetry()
        {
            var registrationError = new InvalidOperationException("Singleton registration test.");
            var cleanupError = new InvalidOperationException("Singleton cleanup test.");
            var failed = new RegistrationFailureSingleton
            {
                RegistrationError = registrationError,
                CleanupError = cleanupError
            };

            var exception = Assert.Throws<AggregateException>(() => SingletonManager.AddSingleton(failed));

            Assert.That(exception.InnerExceptions, Is.EqualTo(new[] { registrationError, cleanupError }));
            Assert.That(RegistrationFailureSingleton.Instance, Is.Null);
            Assert.That(failed.DisposeCount, Is.EqualTo(1));
            Assert.That(failed.HadNoInstanceDuringDispose, Is.True);
            Assert.DoesNotThrow(SingletonManager.Close);

            var retry = SingletonManager.AddSingleton<RegistrationFailureSingleton>();
            Assert.That(RegistrationFailureSingleton.Instance, Is.SameAs(retry));
        }

        [Test]
        public void RejectedSingletonRegistrationPreservesDirectlyRegisteredInstance()
        {
            var existing = new RegistrationFailureSingleton();
            existing.Register();
            var rejected = new RegistrationFailureSingleton();

            try
            {
                Assert.Throws<Exception>(() => SingletonManager.AddSingleton(rejected));

                Assert.That(RegistrationFailureSingleton.Instance, Is.SameAs(existing));
                Assert.That(existing.DisposeCount, Is.Zero);
                Assert.That(rejected.DisposeCount, Is.Zero);
                Assert.DoesNotThrow(SingletonManager.Close);
                Assert.That(RegistrationFailureSingleton.Instance, Is.SameAs(existing));
            }
            finally
            {
                existing.Destroy();
            }

            Assert.DoesNotThrow(() => SingletonManager.AddSingleton<RegistrationFailureSingleton>());
        }

        [Test]
        public void RejectedAspectRegistrationKeepsDirectlyRegisteredServices()
        {
            var existing = new LFrameworkAspect();
            existing.Register();
            var service = new BootstrapService();
            LServices.Register(service);

            try
            {
                Assert.Throws<Exception>(() => SingletonManager.AddSingleton(new LFrameworkAspect()));

                Assert.That(LFrameworkAspect.Instance, Is.SameAs(existing));
                Assert.That(LServices.Get<BootstrapService>(), Is.SameAs(service));
            }
            finally
            {
                existing.Destroy();
            }
        }

        [Test]
        public void RegisteringSameSingletonAgainKeepsExistingRegistration()
        {
            var existing = new RegistrationFailureSingleton();
            existing.Register();

            try
            {
                Assert.Throws<Exception>(() => SingletonManager.AddSingleton(existing));

                Assert.That(RegistrationFailureSingleton.Instance, Is.SameAs(existing));
                Assert.That(existing.RegisterCount, Is.EqualTo(1));
                Assert.That(existing.DisposeCount, Is.Zero);
                Assert.That(existing.IsDisposed(), Is.False);
            }
            finally
            {
                existing.Destroy();
            }
        }

        [Test]
        public void RegisteringSameAspectAgainKeepsExistingServices()
        {
            var existing = new LFrameworkAspect();
            existing.Register();
            var service = new BootstrapService();
            LServices.Register(service);

            try
            {
                Assert.Throws<Exception>(() => SingletonManager.AddSingleton(existing));

                Assert.That(LFrameworkAspect.Instance, Is.SameAs(existing));
                Assert.That(existing.IsDisposed(), Is.False);
                Assert.That(LServices.Get<BootstrapService>(), Is.SameAs(service));
            }
            finally
            {
                existing.Destroy();
            }
        }

        [Test]
        public void DestroyingUnregisteredSingletonOnlyDisposesThatInstance()
        {
            var existing = SingletonManager.AddSingleton<RegistrationFailureSingleton>();
            var unregistered = new RegistrationFailureSingleton();

            unregistered.Destroy();

            Assert.That(RegistrationFailureSingleton.Instance, Is.SameAs(existing));
            Assert.That(existing.DisposeCount, Is.Zero);
            Assert.That(unregistered.DisposeCount, Is.EqualTo(1));
        }

        [Test]
        public void RejectedMonoSingletonRegistrationPreservesExistingInstance()
        {
            var existingObject = new GameObject("Existing Singleton Test");
            var rejectedObject = new GameObject("Rejected Singleton Test");
            var existing = existingObject.AddComponent<SingletonRollbackTestBehaviour>();
            var rejected = rejectedObject.AddComponent<SingletonRollbackTestBehaviour>();
            existing.Register();

            try
            {
                Assert.Throws<Exception>(() => SingletonManager.AddSingleton(rejected));
                Assert.That(SingletonRollbackTestBehaviour.Instance, Is.SameAs(existing));
                Assert.That(existing.DisposeCount, Is.Zero);
                Assert.That(rejected.DisposeCount, Is.Zero);

                rejected.Destroy();
                Assert.That(SingletonRollbackTestBehaviour.Instance, Is.SameAs(existing));
                Assert.That(existing.DisposeCount, Is.Zero);
                Assert.That(rejected.DisposeCount, Is.EqualTo(1));
            }
            finally
            {
                existing.Destroy();
                UnityEngine.Object.DestroyImmediate(existingObject);
                UnityEngine.Object.DestroyImmediate(rejectedObject);
            }
        }

        [Test]
        public void BootstrapRegistrationFailureDoesNotCommitSingletonAndCanRetry()
        {
            LServices.RegisterOwned(new ThrowsOnceOnDispose());
            var failedGameObject = new GameObject("Atomic Bootstrap Failure Test");
            var failedApplication = failedGameObject.AddComponent<StartupRollbackTestApplicationBehaviour>();

            failedApplication.RunStartApplication();

            Assert.That(LFrameworkAspect.Instance, Is.Null);

            var retryGameObject = new GameObject("Atomic Bootstrap Retry Test");
            var retryApplication = retryGameObject.AddComponent<EmptyStartupTestApplicationBehaviour>();
            retryApplication.RunStartApplication();

            Assert.That(LFrameworkAspect.Instance, Is.Not.Null);

            UnityEngine.Object.DestroyImmediate(failedGameObject);
            UnityEngine.Object.DestroyImmediate(retryGameObject);
        }

        [Test]
        public void UnityLifecycleCallbacksReachRegisteredLifecycleService()
        {
            var lifecycle = new TestApplicationLifecycle();
            LServices.Register<IApplicationLifecycle>(lifecycle);
            var gameObject = new GameObject("Application Lifecycle Test");
            var application = gameObject.AddComponent<LifecycleTestApplicationBehaviour>();

            InvokeUnityCallback(application, "OnApplicationFocus", false);
            InvokeUnityCallback(application, "OnApplicationPause", true);
            InvokeUnityCallback(application, "OnApplicationQuit");

            Assert.That(lifecycle.HasFocus, Is.False);
            Assert.That(lifecycle.IsPaused, Is.True);
            Assert.That(lifecycle.QuitCallCount, Is.EqualTo(1));
            Assert.That(application.ShutdownType, Is.EqualTo(ShutdownType.Quit));

            UnityEngine.Object.DestroyImmediate(gameObject);
        }

        private static void InvokeUnityCallback(
            UnitySystemApplicationBehaviour application,
            string methodName,
            params object[] args)
        {
            var method = typeof(UnitySystemApplicationBehaviour).GetMethod(
                methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            method.Invoke(application, args);
        }

        private sealed class TestApplicationLifecycle : IApplicationLifecycle
        {
            public bool HasFocus { get; private set; } = true;
            public bool IsPaused { get; private set; }
            public int QuitCallCount { get; private set; }

            public void OnApplicationFocus(bool hasFocus)
            {
                HasFocus = hasFocus;
            }

            public void OnApplicationPause(bool pauseStatus)
            {
                IsPaused = pauseStatus;
            }

            public void OnApplicationQuit()
            {
                QuitCallCount++;
            }
        }

        public sealed class TestStartupComponent : GameFrameworkComponent
        {
            public bool ShutDownCalled { get; private set; }

            public override void StartComponent()
            {
                throw new InvalidOperationException("Startup failure test.");
            }

            public override void ShutDown()
            {
                ShutDownCalled = true;
            }
        }

        public sealed class BootstrapService
        {
        }

        public sealed class ConstructorCountingSingleton : Singleton<ConstructorCountingSingleton>
        {
            public static int ConstructionCount { get; set; }

            public ConstructorCountingSingleton()
            {
                ConstructionCount++;
            }
        }

        public sealed class ReentrantRegistrationSingleton : Singleton<ReentrantRegistrationSingleton>
        {
            public static bool AttemptReentry { get; set; }
            public static int ConstructionCount { get; set; }

            public ReentrantRegistrationSingleton()
            {
                ConstructionCount++;
            }

            public override void Register()
            {
                if (AttemptReentry)
                {
                    SingletonManager.AddSingleton<ReentrantRegistrationSingleton>();
                }

                base.Register();
            }
        }

        public sealed class RegistrationFailureSingleton : Singleton<RegistrationFailureSingleton>, ISingletonUpdate
        {
            public Exception RegistrationError { get; set; }
            public Exception CleanupError { get; set; }
            public int RegisterCount { get; private set; }
            public int DisposeCount { get; private set; }
            public int UpdateCount { get; private set; }
            public bool HadNoInstanceDuringDispose { get; private set; }

            public override void Register()
            {
                RegisterCount++;
                base.Register();
                if (RegistrationError != null)
                {
                    throw RegistrationError;
                }
            }

            public override void Dispose()
            {
                DisposeCount++;
                HadNoInstanceDuringDispose = Instance == null;
                if (CleanupError != null)
                {
                    throw CleanupError;
                }
            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
                UpdateCount++;
            }
        }

        public sealed class ThrowsOnceOnDispose : IDisposable
        {
            private bool _hasThrown;

            public void Dispose()
            {
                if (_hasThrown)
                {
                    return;
                }

                _hasThrown = true;
                throw new InvalidOperationException("Bootstrap registration rollback test.");
            }
        }
    }

    public sealed class SingletonRollbackTestBehaviour : SingletonMonoBehaviour<SingletonRollbackTestBehaviour>
    {
        public int DisposeCount { get; private set; }

        public override void Dispose()
        {
            DisposeCount++;
        }
    }

    public sealed class StartupRollbackTestApplicationBehaviour : UnitySystemApplicationBehaviour
    {
        public UnitySystemApplicationBehaviourStartupTests.TestStartupComponent Component { get; } = new();

        public void RunStartApplication()
        {
            StartApplication();
        }

        protected override bool RegisterSetting()
        {
            return true;
        }

        protected override void RegisterComponents()
        {
            RegisterComponent(Component);
        }

        protected override void ApplicationStarted()
        {
        }
    }

    public sealed class LifecycleTestApplicationBehaviour : UnitySystemApplicationBehaviour
    {
        public ShutdownType ShutdownType { get; private set; }

        public override void StopApplication(ShutdownType shutdownType)
        {
            ShutdownType = shutdownType;
        }

        protected override void ApplicationStarted()
        {
        }
    }

    public sealed class EmptyStartupTestApplicationBehaviour : UnitySystemApplicationBehaviour
    {
        public void RunStartApplication()
        {
            StartApplication();
        }

        protected override bool RegisterSetting()
        {
            return true;
        }

        protected override void RegisterComponents()
        {
        }

        protected override void ApplicationStarted()
        {
        }
    }
}
