using System;
using System.Collections.Generic;
using System.Reflection;
using GameFramework.UI;
using LFramework.Runtime;
using NUnit.Framework;
using UnityEngine;
using EventComponent = UnityGameFramework.Runtime.EventComponent;
using UIComponent = UnityGameFramework.Runtime.UIComponent;
using Object = UnityEngine.Object;
using RuntimeEntityLogic = UnityGameFramework.Runtime.EntityLogic;

namespace LFramework.Editor.Tests.UI
{
    public class DefaultUIFormHelperTests
    {
        private LFrameworkAspect _aspect;
        private readonly List<Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            _aspect = new LFrameworkAspect();
            _aspect.Register();
            LServices.Register(new UIComponent());
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _createdObjects.Count - 1; i >= 0; i--)
            {
                if (_createdObjects[i] != null)
                {
                    Object.DestroyImmediate(_createdObjects[i]);
                }
            }

            _createdObjects.Clear();

            if (LFrameworkAspect.Instance != null)
            {
                LFrameworkAspect.Instance.Destroy();
            }
        }

        [Test]
        public void CreateUIForm_NewInstance_IsHiddenBeforeOnOpen()
        {
            var helper = CreateTrackedGameObject<DefaultUIFormHelper>("UI Form Helper");
            var uiGroupHelper = CreateTrackedGameObject<TestUIGroupHelper>("UI Group Helper");
            var uiGroup = new TestUIGroup(uiGroupHelper);
            var uiInstance = CreateTrackedRectTransformGameObject("Window");

            uiInstance.SetActive(true);

            IUIForm uiForm = helper.CreateUIForm(uiInstance, uiGroup, true, null);

            Assert.That(uiForm, Is.Not.Null);
            Assert.That(uiInstance.transform.parent, Is.EqualTo(uiGroupHelper.transform));
            Assert.That(uiInstance.activeSelf, Is.False,
                "New UI instances should stay hidden until OnOpen to avoid a one-frame flash.");
        }

        [Test]
        public void UIForm_ReusedInstanceRefreshesPerOpenState()
        {
            var uiObject = CreateTrackedRectTransformGameObject("Window");
            uiObject.AddComponent<TestWindow>();
            var uiForm = uiObject.AddComponent<UIForm>();
            var firstGroup = new TestUIGroup(
                CreateTrackedGameObject<TestUIGroupHelper>("First Group"), "First");
            var secondGroup = new TestUIGroup(
                CreateTrackedGameObject<TestUIGroupHelper>("Second Group"), "Second");

            uiForm.OnInit(1, "UI/Window", firstGroup, true, true, null);
            uiForm.OnRecycle();
            uiForm.OnInit(2, "UI/Window", secondGroup, false, false, null);

            Assert.That(uiForm.SerialId, Is.EqualTo(2));
            Assert.That(uiForm.UIFormAssetName, Is.EqualTo("UI/Window"));
            Assert.That(uiForm.UIGroup, Is.SameAs(secondGroup));
            Assert.That(uiForm.DepthInUIGroup, Is.Zero);
            Assert.That(uiForm.PauseCoveredUIForm, Is.False);
        }

        [Test]
        public void UIForm_RealWindowRecycleFailureIsObservable()
        {
            var uiObject = CreateTrackedRectTransformGameObject("Window");
            var window = uiObject.AddComponent<TestWindow>();
            window.ThrowOnRecycle = true;
            var uiForm = uiObject.AddComponent<UIForm>();
            var group = new TestUIGroup(CreateTrackedGameObject<TestUIGroupHelper>("Group"));
            uiForm.OnInit(1, "UI/Window", group, false, true, null);

            Assert.Throws<InvalidOperationException>(() => uiForm.OnRecycle());
            Assert.That(uiForm.SerialId, Is.Zero,
                "Per-open identity must be cleared even when recycle cleanup fails.");
        }

        [Test]
        public void UIForm_RealWindowCloseFailureStillDestroysViewAndIsObservable()
        {
            LServices.Register(new EventComponent());
            LServices.Register(new ViewComponent());

            var uiObject = CreateTrackedRectTransformGameObject("Window");
            var window = uiObject.AddComponent<TestWindow>();
            window.ThrowOnClose = true;
            window.ThrowOnUnSubscribe = true;
            var throwingSubWindow = CreateTrackedGameObject<ThrowingCloseSubWindow>("Throwing Sub Window");
            throwingSubWindow.transform.SetParent(uiObject.transform);
            var firstCleanupSubWindow = CreateTrackedGameObject<CleanupSubWindow>("First Cleanup Sub Window");
            firstCleanupSubWindow.transform.SetParent(uiObject.transform);
            var secondCleanupSubWindow = CreateTrackedGameObject<CleanupSubWindow>("Second Cleanup Sub Window");
            secondCleanupSubWindow.transform.SetParent(uiObject.transform);
            window.FindSubModules();
            var uiForm = uiObject.AddComponent<UIForm>();
            var group = new TestUIGroup(CreateTrackedGameObject<TestUIGroupHelper>("Group"));
            int releaseCount = TestNativeReference.ClearCount;
            TestNativeReference userData = TestNativeReference.Allocate();
            uiForm.OnInit(1, "UI/Window", group, false, true, userData);
            uiForm.OnOpen(userData);

            Assert.That(window.CompositeDisposable, Is.Not.Null);
            Assert.Catch<Exception>(() => uiForm.OnClose(false, null));
            Assert.That(window.CompositeDisposable.IsDisposed, Is.True,
                "View destruction must run after a window close callback throws.");
            Assert.That(firstCleanupSubWindow.UnSubscribeCount, Is.EqualTo(1));
            Assert.That(secondCleanupSubWindow.UnSubscribeCount, Is.EqualTo(1),
                "A parent unsubscribe failure must not skip sub-window unsubscription.");
            Assert.That(firstCleanupSubWindow.CloseCount, Is.EqualTo(1));
            Assert.That(secondCleanupSubWindow.CloseCount, Is.EqualTo(1),
                "One sub-window failure must not skip later sub-window cleanup.");
            Assert.That(TestNativeReference.ClearCount, Is.EqualTo(releaseCount + 1));
            Assert.That(window.Available, Is.False);
            Assert.That(uiObject.activeSelf, Is.False);
        }

        [Test]
        public void BaseSubWindowSetEnabledUsesTheRequestedState()
        {
            var subWindow = CreateTrackedGameObject<InterfaceUpdateSubWindow>("Sub Window");

            subWindow.SetEnabled(false, true);

            Assert.That(subWindow.IsEnabled, Is.False);
            Assert.That(subWindow.gameObject.activeSelf, Is.False);
            Assert.That(subWindow.EnabledChangedCount, Is.EqualTo(1));
        }

        [Test]
        public void SubModuleUpdateDispatchHonorsEnabledState()
        {
            var windowObject = CreateTrackedRectTransformGameObject("Window");
            var window = windowObject.AddComponent<TestWindow>();
            var subWindow = CreateTrackedGameObject<InterfaceUpdateSubWindow>("Sub Window");
            subWindow.transform.SetParent(windowObject.transform);
            window.FindSubModules();
            var uiForm = windowObject.AddComponent<UIForm>();
            uiForm.OnInit(1, "UI/Window",
                new TestUIGroup(CreateTrackedGameObject<TestUIGroupHelper>("Group")), false, true, null);

            var entityObject = CreateTrackedGameObject<TestEntityLogic>("Entity");
            var subEntity = CreateTrackedGameObject<InterfaceUpdateSubEntity>("Sub Entity");
            subEntity.transform.SetParent(entityObject.transform);
            entityObject.FindSubModules();
            entityObject.InitializeForTest();
            
            subWindow.SetEnabled(false);
            subEntity.SetEnabled(false);
            uiForm.OnUpdate(0.1f, 0.1f);
            entityObject.UpdateForTest();

            Assert.That(subWindow.UpdateCount, Is.Zero);
            Assert.That(subEntity.UpdateCount, Is.Zero);

            subWindow.SetEnabled(true);
            subEntity.SetEnabled(true);
            uiForm.OnUpdate(0.1f, 0.1f);
            entityObject.UpdateForTest();

            Assert.That(subWindow.UpdateCount, Is.EqualTo(1));
            Assert.That(subEntity.UpdateCount, Is.EqualTo(1));
        }

        [Test]
        public void SubModulesDisabledBeforeSubscribeSkipUnSubscribe()
        {
            var subWindow = CreateTrackedGameObject<CleanupSubWindow>("Sub Window");
            var subEntity = CreateTrackedGameObject<CleanupSubEntity>("Sub Entity");
            subWindow.OnInit(null, null);
            subEntity.OnInit(null, null);
            subWindow.SetEnabled(false);
            subEntity.SetEnabled(false);

            // 禁用状态下未订阅，取消订阅不应调用子类钩子（否则 EventPool 会因 handler 不存在抛异常）
            subWindow.Subscribe(null);
            subEntity.Subscribe(null);
            subWindow.UnSubscribe(null);
            subEntity.UnSubscribe(null);

            Assert.That(subWindow.UnSubscribeCount, Is.EqualTo(0));
            Assert.That(subEntity.UnSubscribeCount, Is.EqualTo(0));

            // 重新启用后订阅/取消订阅恢复成对，且重复取消只执行一次
            subWindow.SetEnabled(true);
            subWindow.Subscribe(null);
            subWindow.UnSubscribe(null);
            subWindow.UnSubscribe(null);
            Assert.That(subWindow.UnSubscribeCount, Is.EqualTo(1));
        }

        [Test]
        public void DisabledSubModulesStillReceiveCleanupCallbacks()
        {
            var subWindow = CreateTrackedGameObject<CleanupSubWindow>("Sub Window");
            var subEntity = CreateTrackedGameObject<CleanupSubEntity>("Sub Entity");
            subWindow.OnInit(null, null);
            subEntity.OnInit(null, null);
            subWindow.Subscribe(null);
            subEntity.Subscribe(null);

            subWindow.enabled = false;
            subWindow.SetEnabled(false);
            subEntity.SetEnabled(false);
            subWindow.UnSubscribe(null);
            subWindow.OnClose(false, null);
            subEntity.UnSubscribe(null);
            subEntity.OnHide(false, null);
            subEntity.OnDetached(null, null);
            subEntity.OnDetachFrom(null, null);

            Assert.That(subWindow.UnSubscribeCount, Is.EqualTo(1));
            Assert.That(subWindow.CloseCount, Is.EqualTo(1));
            Assert.That(subEntity.UnSubscribeCount, Is.EqualTo(1));
            Assert.That(subEntity.HideCount, Is.EqualTo(1));
            Assert.That(subEntity.DetachChildCount, Is.EqualTo(1));
            Assert.That(subEntity.DetachFromCount, Is.EqualTo(1));
        }

        [Test]
        public void PartialSubscriptionFailure_UnsubscribesAlreadyRegisteredCallbacks()
        {
            var subWindow = CreateTrackedGameObject<PartialSubscribeSubWindow>("Partial Window");
            var subEntity = CreateTrackedGameObject<PartialSubscribeSubEntity>("Partial Entity");
            try
            {
                Assert.Throws<InvalidOperationException>(() => subWindow.Subscribe(null));
                Assert.Throws<InvalidOperationException>(() => subEntity.Subscribe(null));
                PartialSubscribeSubWindow.Fire();
                PartialSubscribeSubEntity.Fire();

                Assert.That(subWindow.CallbackCount, Is.Zero);
                Assert.That(subEntity.CallbackCount, Is.Zero);
                Assert.That(subWindow.UnsubscribeCount, Is.EqualTo(1));
                Assert.That(subEntity.UnsubscribeCount, Is.EqualTo(1));
                subWindow.UnSubscribe(null);
                subEntity.UnSubscribe(null);
                Assert.That(subWindow.UnsubscribeCount, Is.EqualTo(1));
                Assert.That(subEntity.UnsubscribeCount, Is.EqualTo(1));
            }
            finally
            {
                PartialSubscribeSubWindow.ResetListeners();
                PartialSubscribeSubEntity.ResetListeners();
            }
        }

        [Test]
        public void FailedSubscriptionCleanup_ReportsBothErrorsAndCanBeRetried()
        {
            var subWindow = CreateTrackedGameObject<PartialSubscribeSubWindow>("Partial Window");
            var subEntity = CreateTrackedGameObject<PartialSubscribeSubEntity>("Partial Entity");
            subWindow.ThrowOnUnsubscribe = true;
            subEntity.ThrowOnUnsubscribe = true;
            try
            {
                var windowError = Assert.Throws<AggregateException>(() => subWindow.Subscribe(null));
                var entityError = Assert.Throws<AggregateException>(() => subEntity.Subscribe(null));
                Assert.That(windowError.InnerExceptions, Has.Count.EqualTo(2));
                Assert.That(entityError.InnerExceptions, Has.Count.EqualTo(2));

                subWindow.ThrowOnUnsubscribe = false;
                subEntity.ThrowOnUnsubscribe = false;
                subWindow.UnSubscribe(null);
                subEntity.UnSubscribe(null);
                PartialSubscribeSubWindow.Fire();
                PartialSubscribeSubEntity.Fire();
                Assert.That(subWindow.CallbackCount, Is.Zero);
                Assert.That(subEntity.CallbackCount, Is.Zero);
                Assert.That(subWindow.UnsubscribeCount, Is.EqualTo(2));
                Assert.That(subEntity.UnsubscribeCount, Is.EqualTo(2));
            }
            finally
            {
                PartialSubscribeSubWindow.ResetListeners();
                PartialSubscribeSubEntity.ResetListeners();
            }
        }

        [Test]
        public void ReentrantUnsubscribe_DuringSubscribeRunsAfterSubscribeReturns()
        {
            var subWindow = CreateTrackedGameObject<PartialSubscribeSubWindow>("Partial Window");
            var subEntity = CreateTrackedGameObject<PartialSubscribeSubEntity>("Partial Entity");
            subWindow.ReenterUnsubscribe = true;
            subEntity.ReenterUnsubscribe = true;
            try
            {
                Assert.Throws<InvalidOperationException>(() => subWindow.Subscribe(null));
                Assert.Throws<InvalidOperationException>(() => subEntity.Subscribe(null));
                PartialSubscribeSubWindow.Fire();
                PartialSubscribeSubEntity.Fire();
                Assert.That(subWindow.CallbackCount, Is.Zero);
                Assert.That(subEntity.CallbackCount, Is.Zero);
                Assert.That(subWindow.UnsubscribeCount, Is.EqualTo(1));
                Assert.That(subEntity.UnsubscribeCount, Is.EqualTo(1));
            }
            finally
            {
                PartialSubscribeSubWindow.ResetListeners();
                PartialSubscribeSubEntity.ResetListeners();
            }
        }

        [Test]
        public void LegacyEntityUpdateOverrideIsStillDispatched()
        {
            var entityObject = CreateTrackedGameObject<TestEntityLogic>("Legacy Entity");
            var subEntity = CreateTrackedGameObject<LegacyUpdateSubEntity>("Legacy Sub Entity");
            subEntity.transform.SetParent(entityObject.transform);
            entityObject.FindSubModules();
            entityObject.InitializeForTest();

            Assert.That(GetRequiresUpdate(subEntity), Is.True);
            entityObject.UpdateForTest();
            Assert.That(subEntity.UpdateCount, Is.EqualTo(1));
        }

        private static bool GetRequiresUpdate(object subModule)
        {
            var property = subModule.GetType().BaseType?.GetProperty(
                "RequiresUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(property, Is.Not.Null);
            return (bool)property.GetValue(subModule);
        }

        private T CreateTrackedGameObject<T>(string name) where T : Component
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject.AddComponent<T>();
        }

        private GameObject CreateTrackedRectTransformGameObject(string name)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class TestUIGroupHelper : MonoBehaviour, IUIGroupHelper
        {
            public void SetDepth(int depth)
            {
            }
        }

        private sealed class InterfaceUpdateSubWindow : BaseSubWindow, ISubModuleUpdate
        {
            public int UpdateCount { get; private set; }
            public int EnabledChangedCount { get; private set; }

            public void OnSubModuleUpdate(float elapseSeconds, float realElapseSeconds)
            {
                UpdateCount++;
            }

            protected override void OnInternalEnabledChanged(bool en)
            {
                EnabledChangedCount++;
            }
        }

        private sealed class InterfaceUpdateSubEntity : BaseSubEntity, ISubModuleUpdate
        {
            public int UpdateCount { get; private set; }

            public void OnSubModuleUpdate(float elapseSeconds, float realElapseSeconds)
            {
                UpdateCount++;
            }
        }

        private sealed class CleanupSubWindow : BaseSubWindow
        {
            public int CloseCount { get; private set; }
            public int UnSubscribeCount { get; private set; }

            protected override void OnInternalClose(bool isShutDown, object userData)
            {
                CloseCount++;
            }

            protected override void OnInternalUnSubscribe(EventComponent eventComponent)
            {
                UnSubscribeCount++;
            }
        }

        private sealed class PartialSubscribeSubWindow : BaseSubWindow
        {
            private static event Action Raised;
            public int CallbackCount { get; private set; }
            public int UnsubscribeCount { get; private set; }
            public bool ThrowOnUnsubscribe { get; set; }
            public bool ReenterUnsubscribe { get; set; }
            public static void Fire() => Raised?.Invoke();
            public static void ResetListeners() => Raised = null;
            private void OnRaised() => CallbackCount++;

            protected override void OnInternalSubscribe(EventComponent eventComponent)
            {
                Raised += OnRaised;
                if (ReenterUnsubscribe) UnSubscribe(eventComponent);
                throw new InvalidOperationException("Subscription failed after the first callback was registered.");
            }

            protected override void OnInternalUnSubscribe(EventComponent eventComponent)
            {
                UnsubscribeCount++;
                if (ThrowOnUnsubscribe) throw new InvalidOperationException("Cleanup failed.");
                Raised -= OnRaised;
            }
        }

        private sealed class PartialSubscribeSubEntity : BaseSubEntity
        {
            private static event Action Raised;
            public int CallbackCount { get; private set; }
            public int UnsubscribeCount { get; private set; }
            public bool ThrowOnUnsubscribe { get; set; }
            public bool ReenterUnsubscribe { get; set; }
            public static void Fire() => Raised?.Invoke();
            public static void ResetListeners() => Raised = null;
            private void OnRaised() => CallbackCount++;

            protected override void OnInternalSubscribe(EventComponent eventComponent)
            {
                Raised += OnRaised;
                if (ReenterUnsubscribe) UnSubscribe(eventComponent);
                throw new InvalidOperationException("Subscription failed after the first callback was registered.");
            }

            protected override void OnInternalUnSubscribe(EventComponent eventComponent)
            {
                UnsubscribeCount++;
                if (ThrowOnUnsubscribe) throw new InvalidOperationException("Cleanup failed.");
                Raised -= OnRaised;
            }
        }

        private sealed class ThrowingCloseSubWindow : BaseSubWindow
        {
            protected override void OnInternalClose(bool isShutDown, object userData)
            {
                throw new InvalidOperationException("Test sub-window close failure.");
            }
        }

        private sealed class CleanupSubEntity : BaseSubEntity
        {
            public int HideCount { get; private set; }
            public int UnSubscribeCount { get; private set; }
            public int DetachChildCount { get; private set; }
            public int DetachFromCount { get; private set; }

            protected override void OnInternalHide(bool isShutdown, object userData)
            {
                HideCount++;
            }

            protected override void OnInternalUnSubscribe(EventComponent eventComponent)
            {
                UnSubscribeCount++;
            }

            protected override void OnInternalDetachChild(RuntimeEntityLogic childEntity, object userData)
            {
                DetachChildCount++;
            }

            protected override void OnInternalDetachFrom(RuntimeEntityLogic parentEntity, object userData)
            {
                DetachFromCount++;
            }
        }

        private sealed class TestWindow : LFramework.Runtime.Window
        {
            public bool ThrowOnClose { get; set; }
            public bool ThrowOnRecycle { get; set; }
            public bool ThrowOnUnSubscribe { get; set; }

            protected override void UnSubscribe(EventComponent eventComponent)
            {
                if (ThrowOnUnSubscribe)
                {
                    throw new InvalidOperationException("Test unsubscribe failure.");
                }
            }

            protected override void OnClose(bool isShutDown, object userData)
            {
                if (ThrowOnClose)
                {
                    throw new InvalidOperationException("Test close failure.");
                }
            }

            protected override void OnRecycle()
            {
                if (ThrowOnRecycle)
                {
                    throw new InvalidOperationException("Test recycle failure.");
                }
            }
        }

        private sealed class LegacyUpdateSubEntity : BaseSubEntity
        {
            public int UpdateCount { get; private set; }

            protected override void OnInternalUpdate(float elapseSeconds, float realElapseSeconds)
            {
                UpdateCount++;
            }
        }

        private sealed class TestEntityLogic : NoParamEntityLogic
        {
            public void InitializeForTest()
            {
                base.OnInit(null);
            }

            public void UpdateForTest()
            {
                base.OnUpdate(0.1f, 0.1f);
            }
        }

        private sealed class TestNativeReference : NativeReference<TestNativeReference>
        {
            public TestNativeReference()
            {
            }

            public static int ClearCount { get; private set; }

            public override void Clear()
            {
                ClearCount++;
            }
        }

        private sealed class TestUIGroup : IUIGroup
        {
            public TestUIGroup(IUIGroupHelper helper, string name = "TestGroup")
            {
                Helper = helper;
                Name = name;
            }

            public string Name { get; }
            public int Depth { get; set; }
            public bool Pause { get; set; }
            public bool DisableInstancePool => false;
            public int UIFormCount => 0;
            public IUIForm CurrentUIForm => null;
            public IUIGroupHelper Helper { get; }

            public bool HasUIForm(int serialId) => false;
            public bool HasUIForm(string uiFormAssetName) => false;
            public IUIForm GetUIForm(int serialId) => null;
            public IUIForm GetUIForm(string uiFormAssetName) => null;
            public IUIForm[] GetUIForms(string uiFormAssetName) => new IUIForm[0];
            public void GetUIForms(string uiFormAssetName, List<IUIForm> results) => results.Clear();
            public IUIForm[] GetAllUIForms() => new IUIForm[0];
            public void GetAllUIForms(List<IUIForm> results) => results.Clear();
        }
    }
}
