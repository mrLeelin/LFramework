using LFramework.Runtime;
using LFramework.Runtime.Procedure;
using NUnit.Framework;
using VContainer;

namespace LFramework.Editor.Tests.DI
{
    public class RuntimeProcedureScopeRegistryTests
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
        public void EnterProcedureScope_SetsProcedureResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var registry = new RuntimeProcedureScopeRegistry(context);

            var scope = registry.EnterProcedureScope(new object());

            Assert.That(scope, Is.Not.Null);
            Assert.That(context.ProcedureResolver, Is.SameAs(scope));
            Assert.That(context.ActiveResolver.Resolve<string>(), Is.EqualTo("hello"));
            Assert.That(context.ActiveResolver, Is.Not.SameAs(context.RootResolver));
        }

        [Test]
        public void ExitProcedureScope_ClearsProcedureResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var registry = new RuntimeProcedureScopeRegistry(context);
            registry.EnterProcedureScope(new object());

            registry.ExitProcedureScope();

            Assert.That(context.ProcedureResolver, Is.Null);
            Assert.That(context.ActiveResolver, Is.SameAs(context.RootResolver));
        }

        [Test]
        public void RuntimeBaseProcedure_OnEnter_InjectsUsingProcedureResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var aspect = new LFrameworkAspect(context);
            aspect.Register();

            var procedure = new TestRuntimeProcedure();
            procedure.InvokeOnEnter();

            Assert.That(procedure.Value, Is.EqualTo("hello"));
            Assert.That(context.ProcedureResolver, Is.Not.Null);
        }

        private sealed class TestRuntimeProcedure : RuntimeBaseProcedure
        {
            [Inject]
            public string Value { get; private set; }

            public void InvokeOnEnter()
            {
                base.OnEnter(null);
            }
        }
    }
}
