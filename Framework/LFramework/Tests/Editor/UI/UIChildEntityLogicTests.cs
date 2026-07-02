using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.UI
{
    public sealed class UIChildEntityLogicTests
    {
        [Test]
        public void CanSetParent_WhenNoDependency_ReturnsTrue()
        {
            Assert.That(UIChildEntityLogic.CanSetParent(0, false, false), Is.True);
        }

        [Test]
        public void CanSetParent_WhenDependencyIsMissing_ReturnsFalse()
        {
            Assert.That(UIChildEntityLogic.CanSetParent(1001, false, false), Is.False);
        }

        [Test]
        public void CanSetParent_WhenDependencyHasNoParent_ReturnsFalse()
        {
            Assert.That(UIChildEntityLogic.CanSetParent(1001, true, false), Is.False);
        }

        [Test]
        public void CanSetParent_WhenDependencyHasParent_ReturnsTrue()
        {
            Assert.That(UIChildEntityLogic.CanSetParent(1001, true, true), Is.True);
        }
    }
}
