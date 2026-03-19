using System;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 资源初始化启动任务。
    /// 通过 <see cref="ResourceComponent"/> 调用 <c>InitResources</c> 方法异步初始化资源系统，
    /// 使用 <see cref="UniTaskCompletionSource"/> 将回调模式转换为 UniTask 异步模式。
    /// </summary>
    public class InitResourceTask : ILaunchTask
    {
        /// <summary>
        /// 资源组件，通过 Zenject 依赖注入获取。
        /// </summary>
        [Inject]
        private ResourceComponent _resourceComponent;

        /// <summary>
        /// 任务名称。
        /// </summary>
        public string TaskName => "InitResource";

        /// <summary>
        /// 任务描述。
        /// </summary>
        public string Description => "初始化资源系统";

        /// <summary>
        /// 判断任务是否可以执行。资源初始化任务始终可以执行。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>始终返回 <c>true</c>。</returns>
        public bool CanExecute(LaunchContext context)
        {
            return true;
        }

        /// <summary>
        /// 异步执行资源初始化任务。
        /// 调用 <see cref="ResourceComponent.InitResources"/> 并通过
        /// <see cref="UniTaskCompletionSource"/> 将回调转换为可等待的异步操作。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>任务执行结果。</returns>
        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                Log.Info("[InitResourceTask] 开始初始化资源系统");
                context.ProgressReporter.ReportProgress(0f, "正在初始化资源系统...");

                var tcs = new UniTaskCompletionSource();
                _resourceComponent.InitResources(() => { tcs.TrySetResult(); });
                await tcs.Task;

                Log.Info("[InitResourceTask] 资源系统初始化完成");
                return LaunchTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                Log.Error("[InitResourceTask] 资源系统初始化异常: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
    }
}
