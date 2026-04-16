using LFramework.Runtime;
using NUnit.Framework;
using VContainer;

namespace LFramework.Editor.Tests.Hotfix
{
    public class HotfixScopeRegistryTests
    {
        [Test]
        public void EnterHotfixScope_SetsHotfixResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var registry = new HotfixScopeRegistry(context);

            var scope = registry.EnterHotfixScope();

            Assert.That(scope, Is.Not.Null);
            Assert.That(context.HotfixResolver, Is.SameAs(scope));
            Assert.That(context.ActiveResolver.Resolve<string>(), Is.EqualTo("hello"));
            Assert.That(context.ActiveResolver, Is.SameAs(scope));
        }

        [Test]
        public void ExitHotfixScope_ClearsHotfixResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var registry = new HotfixScopeRegistry(context);
            registry.EnterHotfixScope();

            registry.ExitHotfixScope();

            Assert.That(context.HotfixResolver, Is.Null);
            Assert.That(context.ActiveResolver, Is.SameAs(context.RootResolver));
        }
    }
}
