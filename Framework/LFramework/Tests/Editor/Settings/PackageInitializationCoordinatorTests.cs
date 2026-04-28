using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using NUnit.Framework;

namespace LFramework.Editor.Tests.Settings
{
    public class PackageInitializationCoordinatorTests
    {
        [Test]
        public async Task EnsureInitializedAsync_SkipsInitializer_WhenPackageIsAlreadyReady()
        {
            int initializeCalls = 0;
            var coordinator = new PackageInitializationCoordinator(
                packageName => packageName == "ui",
                packageName =>
                {
                    initializeCalls++;
                    return UniTask.FromResult(PackageInitializationResult.CreateSuccess(packageName));
                });

            PackageInitializationResult result = await coordinator.EnsureInitializedAsync("ui");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.PackageName, Is.EqualTo("ui"));
            Assert.That(initializeCalls, Is.Zero);
        }

        [Test]
        public async Task EnsureInitializedAsync_ReusesInflightInitialization_ForConcurrentRequests()
        {
            int initializeCalls = 0;
            var initSource = new UniTaskCompletionSource<PackageInitializationResult>();
            var coordinator = new PackageInitializationCoordinator(
                _ => false,
                packageName =>
                {
                    initializeCalls++;
                    return initSource.Task;
                });

            UniTask<PackageInitializationResult> first = coordinator.EnsureInitializedAsync("ui");
            UniTask<PackageInitializationResult> second = coordinator.EnsureInitializedAsync("ui");

            Assert.That(initializeCalls, Is.EqualTo(1));

            initSource.TrySetResult(PackageInitializationResult.CreateSuccess("ui"));
            PackageInitializationResult firstResult = await first;
            PackageInitializationResult secondResult = await second;

            Assert.That(firstResult.Succeeded, Is.True);
            Assert.That(secondResult.Succeeded, Is.True);
            Assert.That(firstResult.PackageName, Is.EqualTo("ui"));
            Assert.That(secondResult.PackageName, Is.EqualTo("ui"));
        }

        [Test]
        public async Task EnsureInitializedAsync_AllowsRetry_AfterFailure()
        {
            int initializeCalls = 0;
            bool shouldFail = true;
            var coordinator = new PackageInitializationCoordinator(
                _ => false,
                packageName =>
                {
                    initializeCalls++;
                    return UniTask.FromResult(shouldFail
                        ? PackageInitializationResult.CreateFailure(packageName, "boom")
                        : PackageInitializationResult.CreateSuccess(packageName));
                });

            PackageInitializationResult failed = await coordinator.EnsureInitializedAsync("ui");
            shouldFail = false;
            PackageInitializationResult succeeded = await coordinator.EnsureInitializedAsync("ui");

            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.ErrorMessage, Is.EqualTo("boom"));
            Assert.That(succeeded.Succeeded, Is.True);
            Assert.That(initializeCalls, Is.EqualTo(2));
        }
    }
}
