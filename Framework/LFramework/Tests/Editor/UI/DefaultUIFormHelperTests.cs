using System.Collections.Generic;
using GameFramework.UI;
using LFramework.Runtime;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace LFramework.Editor.Tests.UI
{
    public class DefaultUIFormHelperTests
    {
        private LFrameworkAspect _aspect;
        private readonly List<Object> _createdObjects = new();

        [SetUp]
        public void SetUp()
        {
            _aspect = new LFrameworkAspect(new DiContainer());
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
