using System.Collections.Generic;
using System.Reflection;
using GameFramework.UI;
using LFramework.Runtime;
using NUnit.Framework;
using UnityEngine;
using EventComponent = UnityGameFramework.Runtime.EventComponent;
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
            var subWindow = CreateTrackedGameObject<InterfaceUpdateSubWindow>("Sub Window");
            var subEntity = CreateTrackedGameObject<InterfaceUpdateSubEntity>("Sub Entity");
            
            subWindow.SetEnabled(false);
            subEntity.SetEnabled(false);
         

            Assert.That(subWindow.UpdateCount, Is.EqualTo(1));
            Assert.That(subEntity.UpdateCount, Is.EqualTo(1));
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
        public void LegacyUpdateOverridesAreStillMarkedAsUpdatable()
        {
            var subWindow = CreateTrackedGameObject<LegacyUpdateSubWindow>("Legacy Sub Window");
            var subEntity = CreateTrackedGameObject<LegacyUpdateSubEntity>("Legacy Sub Entity");

            Assert.That(GetRequiresUpdate(subWindow), Is.True);
            Assert.That(GetRequiresUpdate(subEntity), Is.True);
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

        private sealed class LegacyUpdateSubWindow : BaseSubWindow
        {
          
        }

        private sealed class LegacyUpdateSubEntity : BaseSubEntity
        {
          
        }

        private sealed class TestUIGroup : IUIGroup
        {
            public TestUIGroup(IUIGroupHelper helper)
            {
                Helper = helper;
            }

            public string Name => "TestGroup";
            public int Depth { get; set; }
            public bool Pause { get; set; }
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
