using System;
using Cysharp.Threading.Tasks;

namespace LFramework.Runtime.LaunchPipeline.Basic
{
    /// <summary>
    /// 为启动任务提供统一重试能力的基类。
    /// </summary>
    public abstract class RetryableLaunchTaskBase : LaunchTaskBase
    {
        public sealed override async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            if (context == null)
            {
                return LaunchTaskResult.CreateFailed(TaskName, "LaunchContext is null.");
            }

            int maxRetryCount = Math.Max(0, GetMaxRetryCount(context));
            context.LastRetryCount = 0;

            for (int attempt = 0; attempt <= maxRetryCount; attempt++)
            {
                LaunchTaskResult result = null;
                Exception exception = null;

                try
                {
                    result = await ExecuteOnceAsync(context);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    result = LaunchTaskResult.CreateFailed(TaskName, ex.Message);
                }

                if (result != null && !result.IsFailed)
                {
                    return result;
                }

                var errorCategory = ClassifyFailure(result, exception, context);
                RecordFailure(context, result?.ErrorMessage ?? exception?.Message, errorCategory, attempt);

                if (attempt >= maxRetryCount || !ShouldRetry(result, exception, errorCategory, context))
                {
                    await AfterFailureAsync(context, result, exception);
                    return result ?? LaunchTaskResult.CreateFailed(TaskName, exception?.Message ?? "Task failed.");
                }

                context.LastRetryCount = attempt + 1;
                await BeforeRetryAsync(attempt + 1, context, result, exception);

                float delay = Math.Max(0f, GetRetryDelaySeconds(attempt + 1, context));
                if (delay > 0f)
                {
                    await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: context.CancellationToken);
                }
            }

            return LaunchTaskResult.CreateFailed(TaskName, "Task failed after retries.");
        }

        protected abstract UniTask<LaunchTaskResult> ExecuteOnceAsync(LaunchContext context);

        protected virtual bool ShouldRetry(
            LaunchTaskResult result,
            Exception exception,
            LaunchErrorCategory errorCategory,
            LaunchContext context)
        {
            return errorCategory == LaunchErrorCategory.Network ||
                   errorCategory == LaunchErrorCategory.Timeout ||
                   errorCategory == LaunchErrorCategory.Server;
        }

        protected virtual LaunchErrorCategory ClassifyFailure(
            LaunchTaskResult result,
            Exception exception,
            LaunchContext context)
        {
            return exception != null ? LaunchErrorCategory.Unknown : LaunchErrorCategory.None;
        }

        protected virtual UniTask BeforeRetryAsync(
            int attempt,
            LaunchContext context,
            LaunchTaskResult result,
            Exception exception)
        {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask AfterFailureAsync(
            LaunchContext context,
            LaunchTaskResult result,
            Exception exception)
        {
            return UniTask.CompletedTask;
        }

        protected virtual int GetMaxRetryCount(LaunchContext context)
        {
            return context.DefaultRetryCount;
        }

        protected virtual float GetRetryDelaySeconds(int attempt, LaunchContext context)
        {
            if (!context.UseExponentialBackoff)
            {
                return context.DefaultRetryDelaySeconds;
            }

            return context.DefaultRetryDelaySeconds * (float)Math.Pow(2, Math.Max(0, attempt - 1));
        }

        protected void RecordFailure(LaunchContext context, string errorMessage, LaunchErrorCategory category, int attempt)
        {
            context.LastFailedTaskName = TaskName;
            context.LastErrorMessage = errorMessage;
            context.LastErrorCategory = category;
            context.LastAttemptIndex = attempt;
        }
    }
}
