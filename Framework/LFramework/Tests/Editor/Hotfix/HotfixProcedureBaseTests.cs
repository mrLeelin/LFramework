using GameFramework.Fsm;
using GameFramework.Procedure;
using LFramework.Hotfix;
using LFramework.Hotfix.Procedure;
using LFramework.Runtime;
using NUnit.Framework;
using System.Reflection;
using UnityEngine;
using VContainer;

namespace LFramework.Editor.Tests.Hotfix
{
    public class HotfixProcedureBaseTests
    {
        [TearDown]
        public void TearDown()
        {
            if (LFrameworkAspect.Instance != null)
            {
                LFrameworkAspect.Instance.Destroy();
            }
        }

        [Test]
        public void OnEnter_RegistersProviderWorldAndInjectsProcedure()
        {
            var providerRegister = new TestProviderRegister();
            var worldRegister = new TestWorldRegister();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(providerRegister).As<ISystemProviderRegister>();
            builder.RegisterInstance(worldRegister).As<IWorldRegister>();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var aspect = new LFrameworkAspect(context);
            aspect.Register();

            var procedure = new TestHotfixProcedure();
            procedure.InvokeOnEnter();

            Assert.That(context.ProcedureResolver, Is.Not.Null);
            Assert.That(procedure.EnterProcedureCalled, Is.True);
            Assert.That(context.ActiveResolver.Resolve<ISystemProviderRegister>(), Is.SameAs(providerRegister));
            Assert.That(context.ActiveResolver.Resolve<IWorldRegister>(), Is.SameAs(worldRegister));
            Assert.That(procedure.Value, Is.EqualTo("hello"));
            Assert.That(providerRegister.RegisteredState, Is.EqualTo(7));
            Assert.That(worldRegister.LastRegisterState, Is.EqualTo(7));
            Assert.That(worldRegister.World.LinkedProcedure, Is.SameAs(procedure));
        }

        [Test]
        public void OnLeave_UnregistersProviderWorldAndClearsProcedureScope()
        {
            var providerRegister = new TestProviderRegister();
            var worldRegister = new TestWorldRegister();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(providerRegister).As<ISystemProviderRegister>();
            builder.RegisterInstance(worldRegister).As<IWorldRegister>();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var aspect = new LFrameworkAspect(context);
            aspect.Register();

            var procedure = new TestHotfixProcedure();
            procedure.InvokeOnEnter();
            procedure.InvokeOnLeave();

            Assert.That(context.ProcedureResolver, Is.Null);
            Assert.That(procedure.LeaveProcedureCalled, Is.True);
            Assert.That(providerRegister.UnregisteredState, Is.EqualTo(7));
            Assert.That(worldRegister.UnregisterCount, Is.EqualTo(1));
        }

        private sealed class TestHotfixProcedure : HotfixProcedureBase
        {
            [Inject]
            public string Value { get; private set; }

            public bool EnterProcedureCalled { get; private set; }
            public bool LeaveProcedureCalled { get; private set; }

            protected override int ProcedureState => 7;

            protected override void OnEnterProcedure(IFsm<IProcedureManager> procedureOwner)
            {
                EnterProcedureCalled = true;
            }

            protected override void OnLeaveProcedure(IFsm<IProcedureManager> procedureOwner, bool isShutdown)
            {
                LeaveProcedureCalled = true;
            }

            public void InvokeOnEnter()
            {
                typeof(HotfixProcedureBase)
                    .GetMethod("OnEnter", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(this, new object[] { null });
            }

            public void InvokeOnLeave()
            {
                typeof(HotfixProcedureBase)
                    .GetMethod("OnLeave", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.Invoke(this, new object[] { null, false });
            }
        }

        private sealed class TestProviderRegister : ISystemProviderRegister
        {
            public int RegisteredState { get; private set; } = -1;
            public int UnregisteredState { get; private set; } = -1;

            public void TryRegisterProvider(int procedureState)
            {
                Debug.Log($"TestProviderRegister.TryRegisterProvider({procedureState})");
                RegisteredState = procedureState;
            }

            public void TryUnRegisterProvider(int procedureState)
            {
                Debug.Log($"TestProviderRegister.TryUnRegisterProvider({procedureState})");
                UnregisteredState = procedureState;
            }
        }

        private sealed class TestWorldRegister : IWorldRegister
        {
            public int LastRegisterState { get; private set; } = -1;
            public int UnregisterCount { get; private set; }
            public TestWorld World { get; } = new();

            public IWorld TryRegisterWorld(int procedureState)
            {
                LastRegisterState = procedureState;
                return World;
            }

            public void TryUnRegisterWorld()
            {
                UnregisterCount++;
            }
        }

        private sealed class TestWorld : IWorld
        {
            public ProcedureBase LinkedProcedure { get; private set; }

            public void Initialized()
            {
            }

            public void Clear()
            {
            }

            public void LinkProcedure(ProcedureBase procedure)
            {
                LinkedProcedure = procedure;
            }

            public TProcedure GetLinkProcedure<TProcedure>() where TProcedure : ProcedureBase
            {
                return LinkedProcedure as TProcedure;
            }

            public T GetWorldHelper<T>() where T : class, IWorldHelper
            {
                return null;
            }

            public void Update(float elapseSeconds, float realElapseSeconds)
            {
            }

            public void LateUpdate()
            {
            }
        }
    }
}
