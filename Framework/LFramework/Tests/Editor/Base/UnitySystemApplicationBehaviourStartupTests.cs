using System;
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
}
