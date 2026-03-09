using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 资源下载启动任务。
    /// 通过 <see cref="ResourceDownloadComponent"/> 创建并执行资源下载处理器，
    /// 使用事件回调转换为 UniTask 异步模式等待下载完成。
    /// <para>
    /// 该任务为框架级实现，提供基础的下载基础设施。具体的下载标签、包名等配置
    /// 通过 <see cref="LaunchContext.CustomData"/> 传递，子项目可通过重写
    /// <see cref="GetDownloadLabels"/> 和 <see cref="GetDownloadHandlerName"/> 方法自定义下载行为。
    /// </para>
    /// </summary>
    public class DownloadResourceTask : ILaunchTask
    {
        /// <summary>
        /// 资源下载组件，通过 Zenject 依赖注入获取。
        /// </summary>
        [Inject]
        private ResourceDownloadComponent _resourceDownloadComponent;

        /// <summary>
        /// 任务名称。
        /// </summary>
        public string TaskName => "DownloadResource";

        /// <summary>
        /// 任务描述。
        /// </summary>
        public string Description => "下载热更资源";

        /// <summary>
        /// 判断任务是否可以执行。
        /// 当版本检查结果为 <see cref="VersionCheckResultType.NoUpdate"/> 时跳过下载。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>当不需要更新时返回 <c>false</c>，否则返回 <c>true</c>。</returns>
        public bool CanExecute(LaunchContext context)
        {
            return context.VersionCheckResult?.ResultType != VersionCheckResultType.NoUpdate;
        }

        /// <summary>
        /// 异步执行资源下载任务。
        /// 通过 <see cref="ResourceDownloadComponent"/> 创建下载处理器，
        /// 订阅成功和失败事件并使用 <see cref="UniTaskCompletionSource{T}"/> 转换为异步等待。
        /// </summary>
        /// <param name="context">启动管线上下文，可通过 CustomData 传递下载配置。</param>
        /// <returns>任务执行结果。</returns>
        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            try
            {
                Log.Info("[DownloadResourceTask] 开始下载热更资源");

                // 从上下文获取下载配置（项目特定）
                var labels = GetDownloadLabels(context);
                var handlerName = GetDownloadHandlerName(context);

                if (labels == null || labels.Count == 0)
                {
                    Log.Info("[DownloadResourceTask] 没有指定下载标签，跳过下载");
                    return LaunchTaskResult.CreateSuccess(TaskName);
                }

                Log.Info("[DownloadResourceTask] 下载处理器名称: {0}, 标签数量: {1}", handlerName, labels.Count);

                // 创建下载处理器（不自动运行）
                var serialId = _resourceDownloadComponent.AddUpdateHandlerNotRun(handlerName, labels);
                var handler = _resourceDownloadComponent.GetHandler(serialId);

                if (handler == null)
                {
                    Log.Error("[DownloadResourceTask] 创建下载处理器失败");
                    return LaunchTaskResult.CreateFailed(TaskName, "创建下载处理器失败");
                }

                // 使用 UniTaskCompletionSource 将事件回调转换为异步等待
                var tcs = new UniTaskCompletionSource<bool>();
                string errorMessage = null;

                handler.DownloadSuccessfulEventHandler += (sender, e) =>
                {
                    tcs.TrySetResult(true);
                };

                handler.DownloadFailureEventHandler += (sender, e) =>
                {
                    errorMessage = $"资源下载失败，错误类型: {e.UpdateResultType}";
                    tcs.TrySetResult(false);
                };

                // 启动下载
                handler.CheckAndLoadAsync();

                Log.Info("[DownloadResourceTask] 下载处理器已启动，等待下载完成");

                // 等待下载完成
                var success = await tcs.Task;

                if (success)
                {
                    Log.Info("[DownloadResourceTask] 资源下载完成");
                    return LaunchTaskResult.CreateSuccess(TaskName);
                }
                else
                {
                    Log.Error("[DownloadResourceTask] 资源下载失败: {0}", errorMessage);
                    return LaunchTaskResult.CreateFailed(TaskName, errorMessage ?? "资源下载失败");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[DownloadResourceTask] 资源下载异常: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
        }

        /// <summary>
        /// 获取下载标签列表。
        /// 默认从 <see cref="LaunchContext.CustomData"/> 中以 "DownloadLabels" 键获取。
        /// 子项目可重写此方法以自定义下载标签的获取方式。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>下载标签列表，返回 <c>null</c> 或空列表表示无需下载。</returns>
        protected virtual List<string> GetDownloadLabels(LaunchContext context)
        {
            return context.GetCustomData<List<string>>("DownloadLabels");
        }

        /// <summary>
        /// 获取下载处理器名称。
        /// 默认从 <see cref="LaunchContext.CustomData"/> 中以 "DownloadHandlerName" 键获取，
        /// 若未设置则使用默认值 "LaunchDownload"。
        /// 子项目可重写此方法以自定义处理器名称。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>下载处理器名称。</returns>
        protected virtual string GetDownloadHandlerName(LaunchContext context)
        {
            return context.GetCustomData<string>("DownloadHandlerName", "LaunchDownload");
        }
    }
}
