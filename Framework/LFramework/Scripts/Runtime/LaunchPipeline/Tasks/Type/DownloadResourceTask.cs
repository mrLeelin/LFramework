using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using LFramework.Runtime;
using LFramework.Runtime.LaunchPipeline.Basic;

#if ADDRESSABLE_SUPPORT
using UnityEngine.AddressableAssets;
#endif

using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 资源下载启动任务。
    /// 通过 <see cref="ResourceDownloadComponent"/> 创建并执行资源下载处理器，
    /// 支持 Addressable 和 YooAsset 两种下载模式，使用事件回调转换为 UniTask 异步等待。
    /// <para>
    /// 该任务为框架级实现，提供基础的下载基础设施。具体的下载标签、包名等配置
    /// 通过 <see cref="LaunchContext.CustomData"/> 传递，子项目可通过重写虚方法自定义下载行为。
    /// </para>
    /// </summary>
    public class DownloadResourceTask : LaunchTaskBase
    {
        /// <summary>
        /// 资源下载组件，通过 Zenject 依赖注入获取
        /// </summary>
        [Inject] private ResourceDownloadComponent _resourceDownloadComponent;

        /// <summary>
        /// 资源组件
        /// </summary>
        [Inject] private ResourceComponent _resourceComponent;

        /// <summary>
        /// 当前下载处理器的序列 ID，用于清理
        /// </summary>
        private int _handlerSerialId;

        /// <summary>
        /// 异步等待源，桥接事件回调与 async/await
        /// </summary>
        private UniTaskCompletionSource<bool> _tcs;

        /// <summary>
        /// 下载失败时的错误信息
        /// </summary>
        private string _errorMessage;

        /// <summary>
        /// 当前执行的启动上下文引用，用于在事件回调中汇报进度
        /// </summary>
        private LaunchContext _context;

        /// <summary>
        /// 任务名称
        /// </summary>
        public override string TaskName => "DownloadResource";

        /// <summary>
        /// 任务描述
        /// </summary>
        public override string Description => "下载热更资源";

        /// <summary>
        /// 判断任务是否可以执行。
        /// 当版本检查结果为 <see cref="VersionCheckResultType.NoUpdate"/> 时跳过下载。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>当不需要更新时返回 <c>false</c>，否则返回 <c>true</c>。</returns>
        public override bool CanExecute(LaunchContext context)
        {
            if (context.VersionCheckResult == null)
            {
                Log.Warning("[DownloadResourceTask] VersionCheckResult is null, skip download.");
                return false;
            }

            return context.VersionCheckResult.ResultType != VersionCheckResultType.ForceUpdate;
        }

        /// <summary>
        /// 异步执行资源下载任务。
        /// 根据下载模式创建对应的下载处理器，订阅成功/失败事件，
        /// 启动下载并异步等待完成，最后清理处理器。
        /// </summary>
        /// <param name="context">启动管线上下文，可通过 CustomData 传递下载配置。</param>
        /// <returns>任务执行结果。</returns>
        public override async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            IResourceDownloadHandler handler = null;
            try
            {
                _context = context;
                Log.Info("[DownloadResourceTask] 开始下载热更资源");
                context.ProgressReporter.ReportProgress(0f, "正在准备下载...");

                // 1. 从上下文获取下载配置
                var labels = GetDownloadLabels(context);
                var handlerName = GetDownloadHandlerName(context);

                if (labels == null || labels.Count == 0)
                {
                    Log.Info("[DownloadResourceTask] 没有指定下载标签，跳过下载");
                    return LaunchTaskResult.CreateSuccess(TaskName);
                }

                Log.Info("[DownloadResourceTask] 下载模式: {0}, 处理器名称: {1}, 标签数量: {2}",
                    _resourceComponent.ResourceMode, handlerName, labels.Count);

                // 2. 根据模式创建下载处理器（不自动运行）
                _handlerSerialId = CreateHandler(context, _resourceComponent.ResourceMode, handlerName, labels);
                handler = _resourceDownloadComponent.GetHandler(_handlerSerialId);

                if (handler == null)
                {
                    Log.Error("[DownloadResourceTask] 创建下载处理器失败");
                    return LaunchTaskResult.CreateFailed(TaskName, "创建下载处理器失败");
                }

                // 3. 订阅事件
                _tcs = new UniTaskCompletionSource<bool>();
                _errorMessage = null;
                handler.DownloadSuccessfulEventHandler += OnDownloadSuccessful;
                handler.DownloadFailureEventHandler += OnDownloadFailure;
                handler.DownloadUpdateEventHandler += OnDownloadUpdate;
                handler.DownloadStepEventHandler += OnDownloadStep;
                // 4. 启动下载
                handler.CheckAndLoadAsync();
                Log.Info("[DownloadResourceTask] 下载处理器已启动，等待下载完成");

                // 5. 等待下载完成
                var success = await _tcs.Task;

                if (success)
                {
                    Log.Info("[DownloadResourceTask] 资源下载完成");
                    return LaunchTaskResult.CreateSuccess(TaskName);
                }
                else
                {
                    Log.Error("[DownloadResourceTask] 资源下载失败: {0}", _errorMessage);
                    return LaunchTaskResult.CreateFailed(TaskName, _errorMessage ?? "资源下载失败");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[DownloadResourceTask] 资源下载异常: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
            finally
            {
                // 6. 清理：取消订阅事件 + 移除处理器
                if (handler != null)
                {
                    handler.DownloadSuccessfulEventHandler -= OnDownloadSuccessful;
                    handler.DownloadFailureEventHandler -= OnDownloadFailure;
                    handler.DownloadUpdateEventHandler -= OnDownloadUpdate;
                    handler.DownloadStepEventHandler -= OnDownloadStep;
                    _resourceDownloadComponent.RemoveHandler(_handlerSerialId);
                    Log.Info("[DownloadResourceTask] 下载处理器已清理，SerialID: {0}", _handlerSerialId);
                }

                _context = null;
            }
        }

        /// <summary>
        /// 步骤改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void OnDownloadStep(object sender, ResourceDownloadStepEvent e)
        {
        }

        /// <summary>
        /// 下载成功回调
        /// </summary>
        protected virtual void OnDownloadSuccessful(object sender, ResourcesDownloadSuccessfulEvent e)
        {
            _tcs?.TrySetResult(true);
        }

        /// <summary>
        /// 下载失败回调
        /// </summary>
        protected virtual void OnDownloadFailure(object sender, ResourcesDownloadFailureEvent e)
        {
            _errorMessage = $"资源下载失败，错误类型: {e.UpdateResultType}";
            _tcs?.TrySetResult(false);
        }

        /// <summary>
        /// 下载进度更新回调
        /// </summary>
        protected virtual void OnDownloadUpdate(object sender, ResourcesDownloadUpdateEvent e)
        {
            var message = string.IsNullOrEmpty(e.TotalDownloadSize)
                ? $"正在下载资源 {e.Progress:P0}"
                : $"正在下载资源 {e.DownloadSize}/{e.TotalDownloadSize}";
            _context?.ProgressReporter.ReportProgress(e.Progress, message);
        }

        /// <summary>
        /// 根据下载模式创建对应的下载处理器
        /// </summary>
        /// <param name="context">启动上下文</param>
        /// <param name="mode">下载模式</param>
        /// <param name="handlerName">处理器名称</param>
        /// <param name="labels">下载标签列表</param>
        /// <returns>处理器序列 ID</returns>
        private int CreateHandler(LaunchContext context, ResourceMode mode, string handlerName, List<string> labels)
        {
            switch (mode)
            {
#if YOOASSET_SUPPORT
                case ResourceMode.YooAsset:
                    var packageName = _resourceComponent.YooAssetPackageName;
                    var checkDownloadedTags = GetCheckDownloadedTags(context);
                    Log.Info("[DownloadResourceTask] 创建 YooAsset 处理器，包名: {0}, 检查已下载标签: {1}",
                        packageName, checkDownloadedTags);
                    return _resourceDownloadComponent.AddYooAssetHandlerNotRun(
                        handlerName, labels, packageName, true, checkDownloadedTags);
#endif

#if ADDRESSABLE_SUPPORT
                case ResourceMode.Addressable:
                    var mergeMode = GetMergeMode(context);
                    Log.Info("[DownloadResourceTask] 创建 Addressable 处理器，合并模式: {0}", mergeMode);
                    return _resourceDownloadComponent.AddUpdateHandlerNotRun(
                        handlerName, labels, mergeMode, true);
#endif
                default:
                    return 0;
            }
        }

        #region 虚方法 - 子项目可重写

        /// <summary>
        /// 获取下载标签列表。
        /// 默认从 <see cref="LaunchContext.CustomData"/> 中以 "DownloadLabels" 键获取。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>下载标签列表，返回 <c>null</c> 或空列表表示无需下载。</returns>
        protected virtual List<string> GetDownloadLabels(LaunchContext context)
        {
            return context.GetCustomData<List<string>>("DownloadLabels");
        }

        /// <summary>
        /// 获取下载处理器名称。
        /// 默认从 CustomData 的 "DownloadHandlerName" 获取，未设置则使用 "LaunchDownload"。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>下载处理器名称。</returns>
        protected virtual string GetDownloadHandlerName(LaunchContext context)
        {
            return context.GetCustomData("DownloadHandlerName", "LaunchDownload");
        }


#if ADDRESSABLE_SUPPORT
        /// <summary>
        /// 获取 Addressable 合并模式。
        /// 默认从 CustomData 的 "MergeMode" 获取，未设置则使用 Union。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>合并模式。</returns>
        protected virtual Addressables.MergeMode GetMergeMode(LaunchContext context)
        {
            return context.GetCustomData("MergeMode", Addressables.MergeMode.Union);
        }
#endif


        /// <summary>
        /// 获取是否检查已下载标签（仅 YooAsset 模式）。
        /// 默认从 CustomData 的 "CheckDownloadedTags" 获取，未设置则为 false。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>是否检查已下载标签。</returns>
        protected virtual bool GetCheckDownloadedTags(LaunchContext context)
        {
            return context.GetCustomData("CheckDownloadedTags", false);
        }

        #endregion
    }
}