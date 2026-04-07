using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using LFramework.Runtime.LaunchPipeline.Basic;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 资源初始化启动任务。
    /// 通过 <see cref="ResourceComponent"/> 调用 <c>InitResources</c> 方法异步初始化资源系统，
    /// 使用 <see cref="UniTaskCompletionSource"/> 将回调模式转换为 UniTask 异步模式。
    /// 支持可配置的超时机制，防止回调永远不触发时导致启动管线永久挂起。
    /// </summary>
    public class InitResourceTask : LaunchTaskBase
    {
        /// <summary>
        /// 自定义数据键：资源初始化超时时间（秒）。
        /// 通过 <see cref="LaunchContext.SetCustomData"/> 设置，类型为 <c>int</c>。
        /// </summary>
        public const string TimeoutSecondsKey = "InitResourceTimeoutSeconds";

        /// <summary>
        /// 默认超时时间（秒）。
        /// </summary>
        public const int DefaultTimeoutSeconds = 30;

        /// <summary>
        /// 资源组件，通过 Zenject 依赖注入获取。
        /// </summary>
        [Inject]
        private ResourceComponent _resourceComponent;

        /// <summary>
        /// 任务名称。
        /// </summary>
        public override string TaskName => "InitResource";

        /// <summary>
        /// 任务描述。
        /// </summary>
        public override string Description => "初始化资源系统";

        /// <summary>
        /// 异步执行资源初始化任务。
        /// 调用 <see cref="ResourceComponent.InitResources"/> 并通过
        /// <see cref="UniTaskCompletionSource"/> 将回调转换为可等待的异步操作。
        /// 使用 <see cref="UniTask.WhenAny"/> 实现超时机制，超时时间可通过
        /// <see cref="LaunchContext.CustomData"/> 中的 <see cref="TimeoutSecondsKey"/> 配置。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>任务执行结果。</returns>
        public override async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                var timeoutSeconds = context.GetCustomData<int>(TimeoutSecondsKey, DefaultTimeoutSeconds);
                Log.Info("[InitResourceTask] 开始初始化资源系统 (超时: {0}秒)", timeoutSeconds);
                context.ProgressReporter.ReportProgress(0f, "正在初始化资源系统...");

                var tcs = new UniTaskCompletionSource();
                _resourceComponent.InitResources(() => { tcs.TrySetResult(); });

                // 使用 WhenAny 竞速：资源初始化回调 vs 超时
                var timeoutMs = timeoutSeconds * 1000;
                var winIndex = await UniTask.WhenAny(
                    tcs.Task,
                    UniTask.Delay(timeoutMs, cancellationToken: context.CancellationToken)
                );

                if (winIndex == 1)
                {
                    // 超时
                    Log.Error("[InitResourceTask] 资源系统初始化超时 ({0}秒)", timeoutSeconds);
                    return LaunchTaskResult.CreateFailed(TaskName,
                        $"资源系统初始化超时，已等待 {timeoutSeconds} 秒。请检查资源初始化回调是否正常触发。");
                }

                Log.Info("[InitResourceTask] 资源系统初始化完成");
                return LaunchTaskResult.CreateSuccess(TaskName);
            }
            catch (OperationCanceledException)
            {
                Log.Warning("[InitResourceTask] 资源系统初始化被取消");
                return LaunchTaskResult.CreateFailed(TaskName, "资源系统初始化被取消。");
            }
            catch (Exception ex)
            {
                Log.Error("[InitResourceTask] 资源系统初始化异常: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
    }
}
