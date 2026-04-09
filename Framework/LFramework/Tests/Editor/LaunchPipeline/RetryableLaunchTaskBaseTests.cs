using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using LFramework.Runtime.LaunchPipeline;
using LFramework.Runtime.LaunchPipeline.Basic;
using NUnit.Framework;

namespace LFramework.Editor.Tests.LaunchPipeline
{
    public class RetryableLaunchTaskBaseTests
    {
        [Test]
        public async Task RetriesUntilSuccess_AndRecordsRetryState()
        {
            var task = new RetryThenSuccessTask(failCount: 2);
            var context = new LaunchContext();

            LaunchTaskResult result = await task.ExecuteAsync(context).AsTask();

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(task.AttemptCount, Is.EqualTo(3));
            Assert.That(task.BeforeRetryCount, Is.EqualTo(2));
            Assert.That(context.LastRetryCount, Is.EqualTo(2));
            Assert.That(context.LastErrorCategory, Is.EqualTo(LaunchErrorCategory.Network));
        }

        [Test]
        public async Task DoesNotRetry_ForNonRetryableFailures()
        {
            var task = new ConfigFailureTask();
            var context = new LaunchContext();

            LaunchTaskResult result = await task.ExecuteAsync(context).AsTask();

            Assert.That(result.IsFailed, Is.True);
            Assert.That(task.AttemptCount, Is.EqualTo(1));
            Assert.That(context.LastRetryCount, Is.EqualTo(0));
            Assert.That(context.LastErrorCategory, Is.EqualTo(LaunchErrorCategory.Config));
        }

        private sealed class RetryThenSuccessTask : RetryableLaunchTaskBase
        {
            private readonly int _failCount;

            public RetryThenSuccessTask(int failCount)
            {
                _failCount = failCount;
            }

            public int AttemptCount { get; private set; }
            public int BeforeRetryCount { get; private set; }
            public override string TaskName => "RetryThenSuccess";
            public override string Description => "test";

            protected override UniTask<LaunchTaskResult> ExecuteOnceAsync(LaunchContext context)
            {
                AttemptCount++;
                if (AttemptCount <= _failCount)
                {
                    return UniTask.FromResult(LaunchTaskResult.CreateFailed(TaskName, "network timeout"));
                }

                return UniTask.FromResult(LaunchTaskResult.CreateSuccess(TaskName));
            }

            protected override LaunchErrorCategory ClassifyFailure(LaunchTaskResult result, System.Exception exception, LaunchContext context)
            {
                return LaunchErrorCategory.Network;
            }

            protected override UniTask BeforeRetryAsync(int attempt, LaunchContext context, LaunchTaskResult result, System.Exception exception)
            {
                BeforeRetryCount++;
                return UniTask.CompletedTask;
            }

            protected override int GetMaxRetryCount(LaunchContext context)
            {
                return 2;
            }

            protected override float GetRetryDelaySeconds(int attempt, LaunchContext context)
            {
                return 0f;
            }
        }

        private sealed class ConfigFailureTask : RetryableLaunchTaskBase
        {
            public int AttemptCount { get; private set; }
            public override string TaskName => "ConfigFailure";
            public override string Description => "test";

            protected override UniTask<LaunchTaskResult> ExecuteOnceAsync(LaunchContext context)
            {
                AttemptCount++;
                return UniTask.FromResult(LaunchTaskResult.CreateFailed(TaskName, "config invalid"));
            }

            protected override LaunchErrorCategory ClassifyFailure(LaunchTaskResult result, System.Exception exception, LaunchContext context)
            {
                return LaunchErrorCategory.Config;
            }

            protected override float GetRetryDelaySeconds(int attempt, LaunchContext context)
            {
                return 0f;
            }
        }
    }
}
