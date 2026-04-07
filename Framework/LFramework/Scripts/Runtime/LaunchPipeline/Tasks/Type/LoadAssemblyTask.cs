using System;
using Cysharp.Threading.Tasks;
using LFramework.Runtime.LaunchPipeline.Basic;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动流程任务：加载热更新Assembly文件
    /// 通过 HotfixComponent 加载热更程序集，将回调模式转换为 UniTask 异步等待
    /// </summary>
    public class LoadAssemblyTask : LaunchTaskBase
    {
        /// <summary>
        /// 热更新组件，负责加载热更程序集
        /// </summary>
        [Inject] private HotfixComponent _hotfixComponent;

        /// <summary>
        /// 任务名称
        /// </summary>
        public override  string TaskName => nameof(LoadAssemblyTask);

        /// <summary>
        /// 任务描述
        /// </summary>
        public override string Description => "加载热更Assembly文件";
        

        /// <summary>
        /// 异步执行加载热更程序集任务
        /// </summary>
        /// <param name="context">启动上下文</param>
        /// <returns>任务执行结果，成功或失败</returns>
        public override async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                Log.Info("[LoadAssemblyTask] 开始加载热更程序集");
                context.ProgressReporter.ReportProgress(0f, "正在加载热更程序集...");

                // 使用 UniTaskCompletionSource 将回调模式转换为可 await 的异步操作
                var tcs = new UniTaskCompletionSource();
                _hotfixComponent.LoadHotfixAssemblies((result) =>
                {
                    if (result.ResultType != LoadAssemblyResultType.Successful)
                    {
                        tcs.TrySetException(new Exception("[LoadAssemblyTask] load assembly error."));
                    }
                    else
                    {
                        tcs.TrySetResult();
                    }
                });
                // 等待热更程序集加载完成
                await tcs.Task;

                Log.Info("[LoadAssemblyTask] 加载热更数据完成");
                return LaunchTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                Log.Error("[LoadAssemblyTask] 加载热更数据失败: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
    }
}