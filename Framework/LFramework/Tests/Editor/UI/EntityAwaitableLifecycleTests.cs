using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Entity;
using GameFramework.Resource;
using GameFramework.ObjectPool;
using System.Collections.Generic;
using LFramework.Runtime;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityGameFramework.Runtime;
using EntityComponent = UnityGameFramework.Runtime.EntityComponent;
using FailureArgs = GameFramework.Entity.ShowEntityFailureEventArgs;

namespace LFramework.Editor.Tests.UI
{
    public class EntityAwaitableLifecycleTests
    {
        private IEntityManager _manager;
        private EntityComponent _component;
        private PendingResourceManager _resources;

        [SetUp]
        public void SetUp()
        {
            _manager = (IEntityManager)Activator.CreateInstance(
                typeof(IEntityManager).Assembly.GetType("GameFramework.Entity.EntityManager"), true);
            _component = new EntityComponent();
            SetField(_component, "m_EntityManager", _manager);
        }

        [TearDown]
        public void TearDown()
        {
            AwaitableExtensions.ClearAll();
            _component.CancelPendingEntityTasks();
            if (_resources != null)
                foreach (var load in _resources.Loads)
                    if (!load.Completed) load.Fail();
        }

        private void ConfigureLoading(IEntityHelper helper = null)
        {
            _resources = new PendingResourceManager();
            _manager.SetResourceManager(_resources);
            _manager.SetEntityHelper(helper ?? new StubEntityHelper());
            var pools = (IObjectPoolManager)Activator.CreateInstance(typeof(IObjectPoolManager).Assembly.GetType("GameFramework.ObjectPool.ObjectPoolManager"), true);
            _manager.SetObjectPoolManager(pools);
            _manager.AddEntityGroup("group", 60, 10, 60, 0, new StubGroupHelper());
            // This fixture has no component startup subscription; perform its pooled wrapper cleanup.
            _manager.ShowEntitySuccess += (_, args) => ReferencePool.Release(UnityGameFramework.Runtime.ShowEntitySuccessEventArgs.Create(args));
            _manager.ShowEntityFailure += (_, args) => ReferencePool.Release(UnityGameFramework.Runtime.ShowEntityFailureEventArgs.Create(args));
            _manager.ShowEntityCanceled += (id, data) => Invoke(_component, "OnShowEntityCanceled", id, data);
            _manager.ShowEntityOperationDispatch += (data, dispatching) => Invoke(_component, "OnShowEntityOperationDispatch", data, dispatching);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void CancelLoadingShow_CompletesImmediatelyAndLateFailureCannotCompleteReusedId(bool useToken)
        {
            ConfigureLoading();
            using var cts = new CancellationTokenSource();
            var first = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null, cts.Token);
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Pending));
            if (useToken) cts.Cancel();
            else _manager.HideEntity(123);
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => first.GetAwaiter().GetResult());
            var second = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null);
            _resources.Loads[0].Fail();
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
            Assert.That(_manager.IsLoadingEntity(123), Is.True);
            _resources.Loads[1].Fail();
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.Throws<GameFrameworkException>(() => second.GetAwaiter().GetResult());
        }

        [Test]
        public void ClearAll_CancelsLoadingWaitAndAllowsSameIdToStartAgain()
        {
            ConfigureLoading();
            var first = _component.ShowEntityAsync(123, typeof(EntityLogic), "asset", "group", 0, null);
            AwaitableExtensions.ClearAll();
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => first.GetAwaiter().GetResult());
            var second = _component.ShowEntityAsync(123, typeof(EntityLogic), "asset", "group", 0, null);
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
            _resources.Loads[0].Fail();
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
        }

        [Test]
        public void ComponentShutdown_CancelsDirectWaitsBeforeBackendReturns()
        {
            ConfigureLoading();
            var task = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null);
            _component.ShutDown();
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            _resources.Loads[0].Fail();
        }

        [Test]
        public void CancellationContinuation_ReusesIdWithoutReusingInfoStillBeingDispatched()
        {
            ConfigureLoading();
            var originalData = new object();
            var first = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, originalData);
            UniTask<int> second = default;
            var awaiter = first.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                second = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, new object());
            });
            object observedData = null;
            _manager.ShowEntityCanceled += (_, data) => observedData = ((UnityGameFramework.Runtime.ShowEntityInfo)data).UserData;
            _manager.HideEntity(123);
            Assert.That(observedData, Is.SameAs(originalData));
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
            _resources.Loads[0].Fail();
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
        }

        [Test]
        public void FailureContinuation_ReusesIdWithoutChangingDataForLaterObservers()
        {
            ConfigureLoading();
            object originalData = new object();
            var first = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, originalData);
            UniTask<int> second = default;
            var awaiter = first.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                Assert.Throws<GameFrameworkException>(() => awaiter.GetResult());
                second = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, new object());
            });
            object observedData = null;
            _manager.ShowEntityFailure += (_, args) => observedData = ((UnityGameFramework.Runtime.ShowEntityInfo)args.UserData).UserData;
            _resources.Loads[0].Fail();
            Assert.That(observedData, Is.SameAs(originalData));
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
        }

        [Test]
        public void ManagerShutdown_RejectsShowFromCancellationContinuation()
        {
            ConfigureLoading();
            var task = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null);
            UniTask<int> restarted = default;
            var awaiter = task.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                restarted = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null);
            });
            Invoke(_manager, "Shutdown");
            Assert.That(restarted.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.Throws<GameFrameworkException>(() => restarted.GetAwaiter().GetResult());
            Assert.That(_resources.Loads.Count, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator BackgroundTokenCancellation_IsDispatchedOnUnityMainThread()
        {
            ConfigureLoading();
            using var cts = new CancellationTokenSource();
            var task = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null, cts.Token);
            int canceledThread = 0;
            _manager.ShowEntityCanceled += (_, _) => canceledThread = Thread.CurrentThread.ManagedThreadId;
            var background = new Thread(() => cts.Cancel());
            background.Start();
            Assert.That(background.Join(5000), Is.True);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Pending));
            for (int i = 0; i < 10 && task.Status == UniTaskStatus.Pending; i++)
            {
                // EditMode must request a player-loop update to pump Unity's native synchronization context.
                if (!UnityEngine.Application.isPlaying) UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
                yield return null;
            }
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
            Assert.That(canceledThread, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ProgressCallbackCancellation_KeepsOldDataStableUntilOuterDispatchEnds(bool dependency)
        {
            ConfigureLoading();
            object originalData = new object();
            var first = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, originalData);
            UniTask<int> second = default;
            var awaiter = first.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                second = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, new object());
            });
            object observedData = null;
            if (dependency)
            {
                _manager.ShowEntityDependencyAsset += (_, args) => _manager.HideEntity(123);
                _manager.ShowEntityDependencyAsset += (_, args) => observedData = ((UnityGameFramework.Runtime.ShowEntityInfo)args.UserData).UserData;
                _resources.Loads[0].Callbacks.LoadAssetDependencyAssetCallback("asset", "dependency", 1, 2, _resources.Loads[0].Data);
            }
            else
            {
                _manager.ShowEntityUpdate += (_, args) => _manager.HideEntity(123);
                _manager.ShowEntityUpdate += (_, args) => observedData = ((UnityGameFramework.Runtime.ShowEntityInfo)args.UserData).UserData;
                _resources.Loads[0].Callbacks.LoadAssetUpdateCallback("asset", 0.5f, _resources.Loads[0].Data);
            }
            Assert.That(observedData, Is.SameAs(originalData));
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
        }

        [Test]
        public void SynchronousBackendThrow_DoesNotLeaveLoadingOrPooledInfoOwned()
        {
            ConfigureLoading();
            _resources.ThrowOnInstantiate = true;
            var task = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.Throws<InvalidOperationException>(() => task.GetAwaiter().GetResult());
            Assert.That(_manager.IsLoadingEntity(123), Is.False);
            _resources.ThrowOnInstantiate = false;
            Assert.That(_component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null).Status, Is.EqualTo(UniTaskStatus.Pending));
        }

        [Test]
        public void HideUnknownOrAlreadyHidden_CompletesWithoutWaitingForAnEvent()
        {
            Assert.That(_component.HideEntityAsync(123).Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(_component.HideEntityAsync(123).Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [TestCase(false, false, false)]
        [TestCase(true, false, false)]
        [TestCase(false, true, false)]
        [TestCase(true, true, false)]
        [TestCase(true, false, true)]
        public void CancellationDuringEntityCallbacks_CleansEntityBeforeContinuationReusesId(bool cancelInInit, bool throwOnShow, bool throwOnInit)
        {
            using var cts = new CancellationTokenSource();
            var helper = new CallbackEntityHelper();
            ConfigureLoading(helper);
            var first = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "asset", "group", 0, null, cts.Token);
            bool remainedPendingInsideCallback = false;
            Action cancel = () =>
            {
                cts.Cancel();
                remainedPendingInsideCallback = first.Status == UniTaskStatus.Pending;
            };
            helper.OnInit = () =>
            {
                if (cancelInInit) cancel();
                if (throwOnInit) throw new InvalidOperationException("init callback failed after cancellation");
            };
            helper.OnShow = () =>
            {
                if (!cancelInInit) cancel();
                if (throwOnShow) throw new InvalidOperationException("show callback failed after cancellation");
            };
            bool continuationCalled = false;
            bool entityExistedAtContinuation = true;
            UniTask<int> second = default;
            var awaiter = first.GetAwaiter();
            awaiter.OnCompleted(() =>
            {
                Assert.Throws<OperationCanceledException>(() => awaiter.GetResult());
                continuationCalled = true;
                entityExistedAtContinuation = _manager.HasEntity(123);
                second = _component.ShowEntityAwaitable(123, typeof(EntityLogic), "replacement", "group", 0, null);
            });

            var instance = new UnityEngine.GameObject("canceled-entity-test");
            try
            {
                _resources.Loads[0].Succeed(instance);
                Assert.That(remainedPendingInsideCallback, Is.True, "Cleanup must wait until the entity callback has returned.");
                Assert.That(continuationCalled, Is.True);
                Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Canceled));
                Assert.That(entityExistedAtContinuation, Is.False);
                Assert.That(_manager.HasEntity(123), Is.False);
                Assert.That(helper.HideCount, Is.EqualTo(1));
                Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Pending));
                Assert.That(_manager.IsLoadingEntity(123), Is.True);
            }
            finally
            {
                _component.CancelPendingEntityTasks();
                _manager.HideAllLoadedEntities();
                Invoke(_manager, "Update", 0f, 0f);
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void HideLoading_CompletesAndRemovesLoadingState()
        {
            var loading = (IDictionary)GetField(_manager, "m_EntitiesBeingLoaded");
            loading.Add(123, 7);
            Assert.That(_component.HideEntityAsync(123).Status, Is.EqualTo(UniTaskStatus.Succeeded));
            Assert.That(_manager.IsLoadingEntity(123), Is.False);
            Assert.That(_component.HideEntityAsync(123).Status, Is.EqualTo(UniTaskStatus.Succeeded));
        }

        [Test]
        public void ShowSynchronousFailure_DoesNotKeepAWaiterForTheSameId()
        {
            var first = _component.ShowEntityAsync(123, typeof(EntityLogic), "asset", "group", 0, null);
            Assert.That(first.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.Throws<GameFrameworkException>(() => first.GetAwaiter().GetResult());
            var second = _component.ShowEntityAsync(123, typeof(EntityLogic), "asset", "group", 0, null);
            Assert.That(second.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.Throws<GameFrameworkException>(() => second.GetAwaiter().GetResult());
        }

        [Test]
        public void AlreadyCanceledToken_DoesNotStartAnEntityOperation()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = _component.ShowEntityAsync(123, typeof(EntityLogic), "asset", "group", 0, null, cts.Token);
            Assert.That(task.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.Throws<OperationCanceledException>(() => task.GetAwaiter().GetResult());
        }

        [Test]
        public void CanceledLoadFailure_ReleasesItsInfoWithoutPublishingLateFailure()
        {
            var infoType = _manager.GetType().GetNestedType("ShowEntityInfo", BindingFlags.NonPublic);
            var info = infoType.GetMethod("Create").Invoke(null, new object[] { 7, 123, null, null });
            ((IDictionary)GetField(_manager, "m_EntitiesBeingLoaded")).Add(123, 7);
            int canceledId = 0;
            _manager.ShowEntityCanceled += (id, _) => canceledId = id;
            bool failed = false;
            _manager.ShowEntityFailure += (_, args) =>
            {
                failed = true;
            };
            _manager.HideEntity(123);
            Invoke(_manager, "LoadAssetFailureCallback", "asset", LoadResourceStatus.NotExist, "failure", info);
            Assert.That(canceledId, Is.EqualTo(123));
            Assert.That(failed, Is.False);
            Assert.That(infoType.GetProperty("SerialId").GetValue(info), Is.EqualTo(0), "Canceled load info must be returned to ReferencePool.");
        }

        [Test]
        public void LegacyEntityAwaitSignatures_RemainCallableWithoutCancellationToken()
        {
            var extensions = typeof(AwaitableExtensions);
            MethodInfo show = extensions.GetMethod("ShowEntityAsync", new[]
            {
                typeof(EntityComponent), typeof(int), typeof(Type), typeof(string), typeof(string),
                typeof(int), typeof(object)
            });
            MethodInfo hide = extensions.GetMethod("HideEntityAsync", new[] { typeof(EntityComponent), typeof(int) });
            MethodInfo genericShow = null;
            foreach (var method in extensions.GetMethods(BindingFlags.Public | BindingFlags.Static))
            {
                if (method.Name == "ShowEntityAsync" && method.IsGenericMethodDefinition && method.GetParameters().Length == 6)
                    genericShow = method;
            }

            Assert.That(show, Is.Not.Null, "Precompiled callers need the original seven-argument method token.");
            Assert.That(hide, Is.Not.Null, "Precompiled callers need the original two-argument method token.");
            Assert.That(genericShow, Is.Not.Null, "Precompiled callers need the original generic six-argument method token.");
            var showTask = (UniTask<int>)show.Invoke(null,
                new object[] { _component, 123, typeof(EntityLogic), "asset", "group", 0, null });
            var genericTask = (UniTask<int>)genericShow.MakeGenericMethod(typeof(EntityLogic)).Invoke(null,
                new object[] { _component, 123, "asset", "group", 0, null });
            Assert.That(showTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.That(genericTask.Status, Is.EqualTo(UniTaskStatus.Faulted));
            Assert.Throws<GameFrameworkException>(() => showTask.GetAwaiter().GetResult());
            Assert.Throws<GameFrameworkException>(() => genericTask.GetAwaiter().GetResult());
            var hideTask = (UniTask)hide.Invoke(null, new object[] { _component, 123 });
            Assert.That(hideTask.Status, Is.EqualTo(UniTaskStatus.Succeeded));
            hideTask.GetAwaiter().GetResult();
        }

        private static object GetField(object target, string name) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
        private static void SetField(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        private static object Invoke(object target, string name, params object[] args) => target.GetType().GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, args);

        private sealed class StubGroupHelper : IEntityGroupHelper { }
        private sealed class StubEntityHelper : IEntityHelper
        {
            public object InstantiateEntity(object asset) => throw new NotSupportedException();
            public IEntity CreateEntity(object instance, IEntityGroup group, object data) => throw new NotSupportedException();
            public void ReleaseEntity(object asset, object instance) { }
            public void BeforeOnInitEntity(object instance) { }
        }

        private sealed class CallbackEntityHelper : IEntityHelper
        {
            internal Action OnInit;
            internal Action OnShow;
            internal int HideCount;
            public object InstantiateEntity(object asset) => asset;
            public IEntity CreateEntity(object instance, IEntityGroup group, object data) => new CallbackEntity(this, instance);
            public void ReleaseEntity(object asset, object instance) { }
            public void BeforeOnInitEntity(object instance) { }
        }

        private sealed class CallbackEntity : IEntity
        {
            private readonly CallbackEntityHelper _helper;
            internal CallbackEntity(CallbackEntityHelper helper, object handle) { _helper = helper; Handle = handle; }
            public int Id { get; private set; }
            public string EntityAssetName { get; private set; }
            public object Handle { get; }
            public IEntityGroup EntityGroup { get; private set; }
            public bool HasUpdate => false;
            public void OnInit(int id, string name, IEntityGroup group, bool fresh, IEntityHelper helper, object data)
            {
                Id = id;
                EntityAssetName = name;
                EntityGroup = group;
                _helper.OnInit?.Invoke();
            }
            public void OnShow(object data) => _helper.OnShow?.Invoke();
            public void OnHide(bool shutdown, object data) => _helper.HideCount++;
            public void OnRecycle() { }
            public void OnAttached(IEntity entity, object data) { }
            public void OnDetached(IEntity entity, object data) { }
            public void OnAttachTo(IEntity entity, object data) { }
            public void OnDetachFrom(IEntity entity, object data) { }
            public void OnUpdate(float elapsed, float realElapsed) { }
        }

        private sealed class PendingResourceManager : IResourceManager
        {
            internal sealed class Load
            {
                internal string Name;
                internal LoadAssetCallbacks Callbacks;
                internal object Data;
                internal bool Completed;
                internal void Succeed(object instance)
                {
                    Completed = true;
                    Callbacks.LoadAssetSuccessCallback(Name, instance, 0f, Data);
                }
                internal void Fail()
                {
                    Completed = true;
                    Callbacks.LoadAssetFailureCallback(Name, LoadResourceStatus.NotExist, "test failure", Data);
                }
            }
            internal readonly List<Load> Loads = new();
            internal bool ThrowOnInstantiate;
            public string ReadOnlyPath => "";
            public string ReadWritePath => "";
            public ResourceMode ResourceMode => ResourceMode.Unspecified;
            public void InstantiateAsset(string name, LoadAssetCallbacks callbacks, object data)
            {
                if (ThrowOnInstantiate) throw new InvalidOperationException("backend threw");
                Loads.Add(new Load { Name = name, Callbacks = callbacks, Data = data });
            }
            public void SetReadOnlyPath(string path) => throw new NotSupportedException();
            public void SetReadWritePath(string path) => throw new NotSupportedException();
            public void SetResourceMode(ResourceMode mode) => throw new NotSupportedException();
            public void SetResourceHelper(IResourceHelper helper) => throw new NotSupportedException();
            public void InitResources(InitResourcesCompleteCallback callback) => throw new NotSupportedException();
            public HasAssetResult HasAsset(string name) => throw new NotSupportedException();
            public void LoadAsset(string name, Type type, int priority, LoadAssetCallbacks callbacks, object data) => throw new NotSupportedException();
            public void LoadAsset(string name, Type type, string package, int priority, LoadAssetCallbacks callbacks, object data) => throw new NotSupportedException();
            public void UnloadAsset(object asset) => throw new NotSupportedException();
            public void LoadScene(string name, int priority, LoadSceneCallbacks callbacks, object data) => throw new NotSupportedException();
            public void LoadScene(string name, string package, int priority, LoadSceneCallbacks callbacks, object data) => throw new NotSupportedException();
            public void UnloadScene(string name, UnloadSceneCallbacks callbacks, object data) => throw new NotSupportedException();
            public void LoadBinary(string name, LoadBinaryCallbacks callbacks, object data) => throw new NotSupportedException();
            public void LoadBinary(string name, string package, LoadBinaryCallbacks callbacks, object data) => throw new NotSupportedException();
            public void InstantiateAsset(string name, string package, LoadAssetCallbacks callbacks, object data) => throw new NotSupportedException();
        }
    }
}
