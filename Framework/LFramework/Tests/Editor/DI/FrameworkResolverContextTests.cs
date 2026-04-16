using LFramework.Runtime;
using NUnit.Framework;
using VContainer;

namespace LFramework.Editor.Tests.DI
{
    public class FrameworkResolverContextTests
    {
        [Test]
        public void ActiveResolver_PrefersProcedureScope_ThenHotfix_ThenRoot()
        {
            var rootResolver = new ContainerBuilder().Build();
            var hotfixResolver = new ContainerBuilder().Build();
            var procedureResolver = new ContainerBuilder().Build();

            var context = new FrameworkResolverContext();
            context.SetRoot(rootResolver);
            context.SetHotfix(hotfixResolver);
            context.SetProcedure(procedureResolver);

            Assert.That(context.ActiveResolver, Is.SameAs(procedureResolver));
        }

        [Test]
        public void ClearProcedure_FallsBackToHotfixResolver()
        {
            var rootResolver = new ContainerBuilder().Build();
            var hotfixResolver = new ContainerBuilder().Build();
            var procedureResolver = new ContainerBuilder().Build();

            var context = new FrameworkResolverContext();
            context.SetRoot(rootResolver);
            context.SetHotfix(hotfixResolver);
            context.SetProcedure(procedureResolver);

            context.ClearProcedure();

            Assert.That(context.ActiveResolver, Is.SameAs(hotfixResolver));
        }

        [Test]
        public void ClearHotfix_FallsBackToRootResolver()
        {
            var rootResolver = new ContainerBuilder().Build();
            var hotfixResolver = new ContainerBuilder().Build();

            var context = new FrameworkResolverContext();
            context.SetRoot(rootResolver);
            context.SetHotfix(hotfixResolver);

            context.ClearHotfix();

            Assert.That(context.ActiveResolver, Is.SameAs(rootResolver));
        }
    }
}
