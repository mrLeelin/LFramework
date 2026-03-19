using System;
using Cysharp.Threading.Tasks;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动流程任务：进入热更程序集
    /// 通过 HotfixComponent 调用热更入口方法，启动热更逻辑
    /// </summary>
    public class HotfixEntryTask : ILaunchTask
    {
        /// <summary>
        /// 热更新组件，负责进入热更程序集
        /// </summary>
        [Inject] private HotfixComponent _hotfixComponent;

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName => nameof(HotfixEntryTask);

        /// <summary>
        /// 任务描述
        /// </summary>
        public string Description => "进入热更程序集";

        /// <summary>
        /// 判断任务是否可以执行
        /// </summary>
        /// <param name="context">启动上下文</param>
        /// <returns>始终返回 true，该任务无前置条件</returns>
        public bool CanExecute(LaunchContext context)
        {
            return true;
        }

        /// <summary>
        /// 异步执行进入热更程序集任务
        /// </summary>
        /// <param name="context">启动上下文</param>
        /// <returns>任务执行结果，成功或失败</returns>
        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                Log.Info("[HotfixEntryTask] 开始进入热更程序集");
                context.ProgressReporter.ReportProgress(0f, "正在进入热更程序集...");

                _hotfixComponent.EnterHotfixAssembly();

                Log.Info("[HotfixEntryTask] 进入热更程序集完成");
                return LaunchTaskResult.CreateSuccess(TaskName);
            }
            catch (Exception ex)
            {
                Log.Error("[HotfixEntryTask] 进入热更程序集失败: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }
    }
}
