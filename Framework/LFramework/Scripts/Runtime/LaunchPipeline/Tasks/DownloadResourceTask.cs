using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LFramework.Runtime;
using UnityEngine.AddressableAssets;
using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 资源下载模式
    /// </summary>
    public enum DownloadMode
    {
        /// <summary>
        /// Addressable 资源下载
        /// </summary>
        Addressable,

        /// <summary>
        /// YooAsset 资源下载
        /// </summary>
        YooAsset
    }

    /// <summary>
    /// 资源下载启动任务。
    /// 通过 <see cref="ResourceDownloadComponent"/> 创建并执行资源下载处理器，
    /// 支持 Addressable 和 YooAsset 两种下载模式，使用事件回调转换为 UniTask 异步等待。
    /// <para>
    /// 该任务为框架级实现，提供基础的下载基础设施。具体的下载标签、包名等配置
    /// 通过 <see cref="LaunchContext.CustomData"/> 传递，子项目可通过重写虚方法自定义下载行为。
    /// </para>
    /// </summary>
    public class DownloadResourceTask : ILaunchTask
    {
        /// <summary>
        /// 资源下载组件，通过 Zenject 依赖注入获取
        /// </summary>
        [Inject] private ResourceDownloadComponent _resourceDownloadComponent;

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
        /// 任务名称
        /// </summary>
        public string TaskName => "DownloadResource";

        /// <summary>
        /// 任务描述
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
            return context.VersionCheckResult.ResultType != VersionCheckResultType.ForceUpdate;
        }

        /// <summary>
        /// 异步执行资源下载任务。
        /// 根据下载模式创建对应的下载处理器，订阅成功/失败事件，
        /// 启动下载并异步等待完成，最后清理处理器。
        /// </summary>
        /// <param name="context">启动管线上下文，可通过 CustomData 传递下载配置。</param>
        /// <returns>任务执行结果。</returns>
        public async UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context)
        {
            IResourceDownloadHandler handler = null;
            try
            {
                Log.Info("[DownloadResourceTask] 开始下载热更资源");

                // 1. 从上下文获取下载配置
                var labels = GetDownloadLabels(context);
                var handlerName = GetDownloadHandlerName(context);
                var downloadMode = GetDownloadMode(context);

                if (labels == null || labels.Count == 0)
                {
                    Log.Info("[DownloadResourceTask] 没有指定下载标签，跳过下载");
                    return LaunchTaskResult.CreateSuccess(TaskName);
                }

                Log.Info("[DownloadResourceTask] 下载模式: {0}, 处理器名称: {1}, 标签数量: {2}",
                    downloadMode, handlerName, labels.Count);

                // 2. 根据模式创建下载处理器（不自动运行）
                _handlerSerialId = CreateHandler(context, downloadMode, handlerName, labels);
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
                    _resourceDownloadComponent.RemoveHandler(_handlerSerialId);
                    Log.Info("[DownloadResourceTask] 下载处理器已清理，SerialID: {0}", _handlerSerialId);
                }
            }
        }

        /// <summary>
        /// 下载成功回调
        /// </summary>
        private void OnDownloadSuccessful(object sender, ResourcesDownloadSuccessfulEvent e)
        {
            _tcs?.TrySetResult(true);
        }

        /// <summary>
        /// 下载失败回调
        /// </summary>
        private void OnDownloadFailure(object sender, ResourcesDownloadFailureEvent e)
        {
            _errorMessage = $"资源下载失败，错误类型: {e.UpdateResultType}";
            _tcs?.TrySetResult(false);
        }

        /// <summary>
        /// 根据下载模式创建对应的下载处理器
        /// </summary>
        /// <param name="context">启动上下文</param>
        /// <param name="mode">下载模式</param>
        /// <param name="handlerName">处理器名称</param>
        /// <param name="labels">下载标签列表</param>
        /// <returns>处理器序列 ID</returns>
        private int CreateHandler(LaunchContext context, DownloadMode mode, string handlerName, List<string> labels)
        {
            switch (mode)
            {
                case DownloadMode.YooAsset:
                    var packageName = GetPackageName(context);
                    var checkDownloadedTags = GetCheckDownloadedTags(context);
                    Log.Info("[DownloadResourceTask] 创建 YooAsset 处理器，包名: {0}, 检查已下载标签: {1}",
                        packageName, checkDownloadedTags);
                    return _resourceDownloadComponent.AddYooAssetHandlerNotRun(
                        handlerName, labels, packageName, true, checkDownloadedTags);

                case DownloadMode.Addressable:
                default:
                    var mergeMode = GetMergeMode(context);
                    Log.Info("[DownloadResourceTask] 创建 Addressable 处理器，合并模式: {0}", mergeMode);
                    return _resourceDownloadComponent.AddUpdateHandlerNotRun(
                        handlerName, labels, mergeMode, true);
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

        /// <summary>
        /// 获取下载模式。
        /// 默认从 CustomData 的 "DownloadMode" 获取，未设置则使用 Addressable。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>下载模式。</returns>
        protected virtual DownloadMode GetDownloadMode(LaunchContext context)
        {
            return context.GetCustomData("DownloadMode", DownloadMode.Addressable);
        }

        /// <summary>
        /// 获取 YooAsset 包名。
        /// 默认从 CustomData 的 "PackageName" 获取，未设置则使用 "DefaultPackage"。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>YooAsset 包名。</returns>
        protected virtual string GetPackageName(LaunchContext context)
        {
            return context.GetCustomData("PackageName", "DefaultPackage");
        }

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
