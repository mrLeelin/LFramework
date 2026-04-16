using LFramework.Runtime;
using NUnit.Framework;
using VContainer;

namespace LFramework.Editor.Tests.DI
{
    public class LFrameworkAspectTests
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
        public void Get_ResolvesFromRootResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var aspect = new LFrameworkAspect(context);
            aspect.Register();

            Assert.That(aspect.RootResolver, Is.SameAs(context.RootResolver));
            Assert.That(aspect.Get<string>(), Is.EqualTo("hello"));
        }
    }
}
