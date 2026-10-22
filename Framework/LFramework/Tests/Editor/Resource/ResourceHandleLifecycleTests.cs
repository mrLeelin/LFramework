using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using NUnit.Framework;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Tests.Resource
{
    public class ResourceHandleLifecycleTests
    {
        [SetUp]
        public void SetUp()
        {
            ReferencePool.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            ReferencePool.ClearAll();
        }

        [Test]
        public void DisposeBeforeCompletion_CancelsWaiterAndDefersPoolReuseUntilBackendCompletes()
        {
            ResourceRawFileHandle first = ReferencePool.Acquire<ResourceRawFileHandle>();
            first.MarkFromPool();
            UniTask<byte[]> firstWaiter = first.Task;
            int backendReleaseCount = 0;

            first.Dispose();

            Assert.That(firstWaiter.Status, Is.EqualTo(UniTaskStatus.Canceled),
                "Early release must put the original waiter in a terminal canceled state.");

            ResourceRawFileHandle second = ReferencePool.Acquire<ResourceRawFileHandle>();
            Assert.That(second, Is.Not.SameAs(first), "An unfinished backend still owns the first handle.");

            first.RegisterReleaseAction(() => backendReleaseCount++);
            first.SetResult(new byte[] { 1, 2, 3 });

            Assert.That(backendReleaseCount, Is.EqualTo(1));
            ResourceRawFileHandle reused = ReferencePool.Acquire<ResourceRawFileHandle>();
            Assert.That(reused, Is.SameAs(first), "The handle may be reused only after backend completion and cleanup.");

            ReferencePool.Release(second);
            ReferencePool.Release(reused);
        }

        [Test]
        public void CompletionContinuation_DisposeDoesNotReturnHandleUntilCompletionSignalReturns()
        {
            ResourceRawFileHandle first = ReferencePool.Acquire<ResourceRawFileHandle>();
            first.MarkFromPool();
            UniTask<byte[]> waiter = first.Task;
            ResourceRawFileHandle acquiredInsideContinuation = null;

            waiter.GetAwaiter().OnCompleted(() =>
            {
                first.Dispose();
                acquiredInsideContinuation = ReferencePool.Acquire<ResourceRawFileHandle>();
            });

            first.SetResult(new byte[] { 7 });

            Assert.That(acquiredInsideContinuation, Is.Not.Null);
            Assert.That(acquiredInsideContinuation, Is.Not.SameAs(first),
                "Synchronous continuations must not observe the completing handle back in the pool.");

            ResourceRawFileHandle acquiredAfterCompletion = ReferencePool.Acquire<ResourceRawFileHandle>();
            Assert.That(acquiredAfterCompletion, Is.SameAs(first));

            ReferencePool.Release(acquiredInsideContinuation);
            ReferencePool.Release(acquiredAfterCompletion);
        }

        [Test]
        public void RepeatedDispose_ReleasesBackendExactlyOnce()
        {
            var handle = new ResourceRawFileHandle();
            int backendReleaseCount = 0;
            handle.RegisterReleaseAction(() => backendReleaseCount++);
            handle.SetResult(Array.Empty<byte>());

            handle.Dispose();
            handle.Dispose();

            Assert.That(backendReleaseCount, Is.EqualTo(1));
        }

        [Test]
        public void DisposeBeforeCompletion_CancelsEveryBuiltInHandleWaiter()
        {
            var asset = new ResourceAssetHandle<UnityEngine.Object>();
            var batch = new ResourceBatchHandle<UnityEngine.Object>();
            var raw = new ResourceRawFileHandle();
            var scene = new ResourceSceneHandle();
            UniTask<UnityEngine.Object> assetWaiter = asset.Task;
            UniTask<List<UnityEngine.Object>> batchWaiter = batch.Task;
            UniTask<byte[]> rawWaiter = raw.Task;
            UniTask sceneWaiter = scene.Task;

            asset.Dispose();
            batch.Dispose();
            raw.Dispose();
            scene.Dispose();

            Assert.That(assetWaiter.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(batchWaiter.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(rawWaiter.Status, Is.EqualTo(UniTaskStatus.Canceled));
            Assert.That(sceneWaiter.Status, Is.EqualTo(UniTaskStatus.Canceled));
        }

        [Test]
        public void ResultCleanupFailure_StillReleasesBackendExactlyOnce()
        {
            ThrowingResultCleanupHandle handle = ReferencePool.Acquire<ThrowingResultCleanupHandle>();
            handle.MarkFromPool();
            int backendReleaseCount = 0;
            handle.RegisterReleaseAction(() => backendReleaseCount++);
            handle.Complete();

            Assert.Throws<InvalidOperationException>(() => handle.Dispose());
            Assert.That(backendReleaseCount, Is.EqualTo(1));

            ThrowingResultCleanupHandle reused = ReferencePool.Acquire<ThrowingResultCleanupHandle>();
            Assert.That(reused, Is.SameAs(handle), "A result cleanup exception must not strand the pooled handle.");
            Assert.That(backendReleaseCount, Is.EqualTo(1));
            ReferencePool.Release(reused);
        }

        public sealed class ThrowingResultCleanupHandle : ResourceHandleBase
        {
            public void Complete()
            {
                Assert.That(TryBeginCompletion(), Is.True);
                EndCompletion();
            }

            protected override void SetErrorInternal(string errorMessage)
            {
            }

            protected override void ReleaseResultInternal()
            {
                throw new InvalidOperationException("result cleanup failed");
            }
        }
    }
}
