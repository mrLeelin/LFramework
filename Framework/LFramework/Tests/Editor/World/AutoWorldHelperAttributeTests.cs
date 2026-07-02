using System.Reflection;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.World
{
    public sealed class AutoWorldHelperAttributeTests
    {
        [Test]
        public void CanRegisterReturnsTrueWhenConditionIsMissing()
        {
            var attribute = new AutoWorldHelperAttribute(typeof(TestWorld));

            Assert.That(attribute.CanRegister(), Is.True);
        }

        [Test]
        public void ConditionTypeCanControlHelperRegistration()
        {
            TestRegisterCondition.ShouldRegister = false;

            var attribute = typeof(ConditionTypeHelper).GetCustomAttribute<AutoWorldHelperAttribute>();

            Assert.That(attribute, Is.Not.Null);
            Assert.That(attribute.CanRegister(), Is.False);

            TestRegisterCondition.ShouldRegister = true;

            Assert.That(attribute.CanRegister(), Is.True);
        }

        private sealed class TestWorld
        {
        }

        private sealed class TestRegisterCondition : IAutoWorldHelperRegisterCondition
        {
            public static bool ShouldRegister { get; set; }

            public bool CanRegister()
            {
                return ShouldRegister;
            }
        }

        [AutoWorldHelper(typeof(TestWorld), RegisterConditionType = typeof(TestRegisterCondition))]
        private sealed class ConditionTypeHelper : WorldHelperBase
        {
            public override void Clear()
            {
            }

            public override void Initialize()
            {
            }

            public override void StartGame()
            {
            }

            public override void StopGame()
            {
            }
        }
    }
}
