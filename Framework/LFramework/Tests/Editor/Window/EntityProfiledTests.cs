using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Window
{
    public sealed class EntityProfiledTests
    {
        [Test]
        public void EntityComponentReportsUnavailableBeforeAwakeComponent()
        {
            var component = new EntityComponent();

            Assert.That(component.IsInitialized, Is.False);
        }
    }
}
