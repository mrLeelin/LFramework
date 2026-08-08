using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
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
            ReferencePool.ClearAll();
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

        [Test]
        public void WorldHelperAliasesDoNotDuplicateLifecycleOrRelease()
        {
            CountingWorldHelper.Reset();
            var world = new LifecycleTestWorld();
            var helper = world.RegisterHelper<CountingWorldHelper>();

            Assert.That(world.GetWorldHelper<ICountingWorldHelper>(), Is.SameAs(helper));
            Assert.That(world.GetWorldHelper<CountingWorldHelper>(), Is.SameAs(helper));

            world.InitializeHelpers();
            world.StartHelpers();
            world.InstantiateHelpers();
            world.StopHelpers();

            Assert.That(CountingWorldHelper.InitializeCount, Is.EqualTo(1));
            Assert.That(CountingWorldHelper.StartCount, Is.EqualTo(1));
            Assert.That(CountingWorldHelper.InstantiateCount, Is.EqualTo(1));
            Assert.That(CountingWorldHelper.StopCount, Is.EqualTo(1));
            Assert.DoesNotThrow(world.Clear);
            Assert.That(CountingWorldHelper.ClearCount, Is.EqualTo(1));
        }

        [Test]
        public void DuplicateWorldHelperInterfaceFailsBeforePartialRegistration()
        {
            var world = new LifecycleTestWorld();
            world.RegisterHelper<CountingWorldHelper>();

            var exception = Assert.Throws<GameFrameworkException>(
                () => world.RegisterHelper<ConflictingWorldHelper>());

            Assert.That(exception.Message, Does.Contain(typeof(ICountingWorldHelper).FullName));
            Assert.That(exception.Message, Does.Contain(typeof(CountingWorldHelper).FullName));
            Assert.That(exception.Message, Does.Contain(typeof(ConflictingWorldHelper).FullName));
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

        private interface ICountingWorldHelper : IWorldHelper
        {
        }

        private sealed class LifecycleTestWorld : WorldBase
        {
            public T RegisterHelper<T>() where T : class, IWorldHelper, new()
            {
                return BuildWorldHelpers<T>();
            }

            public void InitializeHelpers()
            {
                var method = typeof(WorldBase).GetMethod(
                    "InitializeAllHelpers",
                    BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(method, Is.Not.Null);
                method.Invoke(this, null);
            }

            public void StartHelpers()
            {
                StartHelperGame();
            }

            public void InstantiateHelpers()
            {
                InstantiateHelperGame().GetAwaiter().GetResult();
            }

            public void StopHelpers()
            {
                StopHelperGame();
            }
        }

        private sealed class CountingWorldHelper : WorldHelperBase, ICountingWorldHelper
        {
            public static int ClearCount { get; private set; }
            public static int InitializeCount { get; private set; }
            public static int StartCount { get; private set; }
            public static int InstantiateCount { get; private set; }
            public static int StopCount { get; private set; }

            public static void Reset()
            {
                ClearCount = 0;
                InitializeCount = 0;
                StartCount = 0;
                InstantiateCount = 0;
                StopCount = 0;
            }

            public override void Clear()
            {
                ClearCount++;
            }

            public override void Initialize()
            {
                InitializeCount++;
            }

            public override void StartGame()
            {
                StartCount++;
            }

            public override UniTask InstantiateWorld()
            {
                InstantiateCount++;
                return UniTask.CompletedTask;
            }

            public override void StopGame()
            {
                StopCount++;
            }
        }

        private sealed class ConflictingWorldHelper : WorldHelperBase, ICountingWorldHelper
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
