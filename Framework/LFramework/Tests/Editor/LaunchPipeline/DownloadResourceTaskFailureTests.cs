using System;
using System.Reflection;
using LFramework.Runtime;
using LFramework.Runtime.LaunchPipeline;
using NUnit.Framework;

namespace LFramework.Editor.Tests.LaunchPipeline
{
    public class DownloadResourceTaskFailureTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void NetworkFailure_RetainsTypedCategoryAndContextAfterCleanup()
        {
            var task = new DownloadResourceTask();
            SetField(task, "_currentPlanName", "base-package");
            Invoke(task, "OnDownloadFailure", null,
                new ResourcesDownloadFailureEvent { UpdateResultType = UpdateResultType.NotReachable });

            Invoke(task, "CleanupHandler", (object)null);
            LaunchTaskResult result = (LaunchTaskResult)Invoke(task, "CreateDownloadFailureResult",
                "Resource download failed.");
            var category = (LaunchErrorCategory)Invoke(task, "ClassifyFailure", result, null, new LaunchContext());

            Assert.That(result.ErrorMessage, Does.Contain(nameof(UpdateResultType.NotReachable)));
            Assert.That(result.ErrorMessage, Does.Contain("base-package"));
            Assert.That(category, Is.EqualTo(LaunchErrorCategory.Network));
            Assert.That((bool)Invoke(task, "ShouldRetry", result, null, category, new LaunchContext()), Is.True);
        }

        [Test]
        public void ConfigurationFailure_DoesNotRetryWithoutAStaleDownloadFailure()
        {
            var task = new DownloadResourceTask();
            LaunchTaskResult result = LaunchTaskResult.CreateFailed(task.TaskName,
                "Failed to create download handler for package 'base-package'.");

            var category = (LaunchErrorCategory)Invoke(task, "ClassifyFailure", result, null, new LaunchContext());

            Assert.That(category, Is.EqualTo(LaunchErrorCategory.Config));
            Assert.That((bool)Invoke(task, "ShouldRetry", result, null, category, new LaunchContext()), Is.False);
        }

        private static object Invoke(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, InstanceFlags);
            Assert.That(method, Is.Not.Null, $"Expected method '{methodName}'.");
            return method.Invoke(target, arguments);
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, InstanceFlags);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}'.");
            field.SetValue(target, value);
        }
    }
}
