using System.Reflection;
using LFramework.Editor.Inspector;
using LFramework.Runtime;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests
{
    public sealed class LSystemApplicationBehaviourInspectorTests
    {
        [Test]
        public void OnCompileCompleteSynchronizesSerializedComponentTypesWithoutInspectorRepaint()
        {
            var gameObject = new GameObject("LSystemApplicationBehaviour Inspector Test");
            var application = gameObject.AddComponent<LSystemApplicationBehaviour>();
            UnityEditor.Editor inspector = null;

            try
            {
                inspector = UnityEditor.Editor.CreateEditor(
                    application,
                    typeof(LSystemApplicationBehaviourInspector));

                var serializedApplication = new SerializedObject(application);
                var allComponentTypes = serializedApplication.FindProperty("allComponentTypes");
                allComponentTypes.arraySize = 1;
                allComponentTypes.GetArrayElementAtIndex(0).stringValue = "Stale.Component";
                serializedApplication.ApplyModifiedProperties();

                InvokeCompileComplete(inspector);

                serializedApplication.Update();
                allComponentTypes = serializedApplication.FindProperty("allComponentTypes");

                Assert.That(allComponentTypes.arraySize, Is.GreaterThan(1));
                Assert.That(ReadArray(allComponentTypes), Does.Contain(typeof(ResourceComponent).FullName));
                Assert.That(ReadArray(allComponentTypes), Does.Not.Contain("Stale.Component"));
            }
            finally
            {
                if (inspector != null)
                {
                    Object.DestroyImmediate(inspector);
                }

                Object.DestroyImmediate(gameObject);
            }
        }

        private static void InvokeCompileComplete(UnityEditor.Editor inspector)
        {
            var method = typeof(LSystemApplicationBehaviourInspector).GetMethod(
                "OnCompileComplete",
                BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(method, Is.Not.Null);
            method.Invoke(inspector, null);
        }

        private static string[] ReadArray(SerializedProperty property)
        {
            var values = new string[property.arraySize];
            for (int i = 0; i < property.arraySize; i++)
            {
                values[i] = property.GetArrayElementAtIndex(i).stringValue;
            }

            return values;
        }
    }
}
