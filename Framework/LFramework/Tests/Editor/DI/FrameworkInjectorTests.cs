using LFramework.Runtime;
using NUnit.Framework;
using UnityEngine;
using VContainer;

namespace LFramework.Editor.Tests.DI
{
    public class FrameworkInjectorTests
    {
        private readonly System.Collections.Generic.List<Object> _createdObjects = new();

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
        }

        [Test]
        public void Inject_UsesActiveResolver()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var injector = new FrameworkInjector(context);
            var target = new InjectableTarget();

            injector.Inject(target);

            Assert.That(target.Value, Is.EqualTo("hello"));
        }

        [Test]
        public void InjectGameObject_RecursivelyInjectsChildren()
        {
            var builder = new ContainerBuilder();
            builder.RegisterInstance("hello");

            var context = new FrameworkResolverContext();
            context.SetRoot(builder.Build());

            var injector = new FrameworkInjector(context);
            var root = CreateGameObject("Root");
            var child = CreateGameObject("Child");
            child.transform.SetParent(root.transform, false);
            child.AddComponent<InjectableMonoBehaviour>();

            injector.InjectGameObject(root);

            Assert.That(child.GetComponent<InjectableMonoBehaviour>().Value, Is.EqualTo("hello"));
        }

        private GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _createdObjects.Add(gameObject);
            return gameObject;
        }

        private sealed class InjectableTarget
        {
            [Inject]
            public string Value { get; private set; }
        }

        private sealed class InjectableMonoBehaviour : MonoBehaviour
        {
            [Inject]
            public string Value { get; private set; }
        }
    }
}
