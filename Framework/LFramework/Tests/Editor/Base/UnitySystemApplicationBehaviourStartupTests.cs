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
}
