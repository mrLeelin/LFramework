using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameFramework;
using LFramework.Runtime;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.World
{
    public sealed class AutoWorldHelperAttributeTests
    {
        [SetUp]
        public void SetUp()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            SingletonManager.AddSingleton(new LFrameworkAspect());
            ClearAutoRegisterWorldHelperCache();
        }

        [TearDown]
        public void TearDown()
        {
            SingletonManager.Close();
            LServices.Reset();
            Injection.ClearAll();
            ClearAutoRegisterWorldHelperCache();
        }

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

        [Test]
        public void AutoRegisterWorldHelperUsesSortAscendingOrder()
        {
            var hotfixComponent = new HotfixComponent();
            LServices.Register(hotfixComponent);
            ConfigureHotfixTypes(
                hotfixComponent,
                typeof(LateSortedHelper),
                typeof(DefaultSortedHelper),
                typeof(EarlySortedHelper));

            var helpers = GetRegisterWorldHelpers(typeof(SortedWorld));

            try
            {
                Assert.That(
                    helpers.Select(helper => helper.GetType()).ToArray(),
                    Is.EqualTo(new[]
                    {
                        typeof(EarlySortedHelper),
                        typeof(DefaultSortedHelper),
                        typeof(LateSortedHelper)
                    }));
            }
            finally
            {
                foreach (var helper in helpers)
                {
                    ReferencePool.Release(helper);
                }
            }
        }

        private static IReadOnlyList<IWorldHelper> GetRegisterWorldHelpers(Type worldType)
        {
            var getRegisterWorldHelper = typeof(AutoRegisterWorldHelper).GetMethod(
                "GetRegisterWorldHelper",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(getRegisterWorldHelper, Is.Not.Null);
            return (IReadOnlyList<IWorldHelper>)getRegisterWorldHelper.Invoke(null, new object[] { worldType });
        }

        private static void ConfigureHotfixTypes(HotfixComponent hotfixComponent, params Type[] types)
        {
            var typeMap = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in types)
            {
                typeMap.Add(type.FullName, type);
            }

            var parseAttributes = typeof(HotfixComponent).GetMethod(
                "ParseAttributes",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(parseAttributes, Is.Not.Null);
            parseAttributes.Invoke(hotfixComponent, new object[] { typeMap });
        }

        private static void ClearAutoRegisterWorldHelperCache()
        {
            var cache = typeof(AutoRegisterWorldHelper).GetField(
                "_cacheWorldHelperTypes",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.That(cache, Is.Not.Null);
            ((Dictionary<Type, List<Type>>)cache.GetValue(null)).Clear();
        }

        private sealed class TestWorld
        {
        }

        private sealed class SortedWorld
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

        [AutoWorldHelper(typeof(SortedWorld), Sort = 20)]
        private sealed class LateSortedHelper : WorldHelperBase
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

        [AutoWorldHelper(typeof(SortedWorld))]
        private sealed class DefaultSortedHelper : WorldHelperBase
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

        [AutoWorldHelper(typeof(SortedWorld), Sort = -10)]
        private sealed class EarlySortedHelper : WorldHelperBase
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
