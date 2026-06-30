using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using LFramework.Runtime.LaunchPipeline.Basic;
using LFramework.Runtime.Settings;
#if ADDRESSABLE_SUPPORT
using UnityEngine.AddressableAssets;
#endif
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// Downloads hot-update resources during launch.
    /// </summary>
    public partial class DownloadResourceTask : RetryableLaunchTaskBase
    {
        [Inject] private ResourceDownloadComponent _resourceDownloadComponent;
        [Inject] private ResourceComponent _resourceComponent;

        private int _handlerSerialId;
        private UniTaskCompletionSource<bool> _tcs;
        private string _errorMessage;
        private LaunchContext _context;
        private int _currentPlanIndex;
        private int _currentPlanCount = 1;
        private string _currentPlanName;

        public override string TaskName => "DownloadResource";
        public override string Description => "下载热更资源";

        public override bool CanExecute(LaunchContext context)
        {
            if (context.VersionCheckResult == null)
            {
                Log.Warning("[DownloadResourceTask] VersionCheckResult is null, skip download.");
                return false;
            }

            return context.VersionCheckResult.ResultType != VersionCheckResultType.ForceUpdate;
        }

        protected override async UniTask<LaunchTaskResult> ExecuteOnceAsync(LaunchContext context)
        {
            try
            {
                _context = context;
                _currentPlanIndex = 0;
                _currentPlanCount = 1;
                _currentPlanName = null;

                Log.Info("[DownloadResourceTask] Start downloading resources.");
                context.ProgressReporter.ReportProgress(0f, "Preparing resource download...");

                if (_resourceDownloadComponent == null || _resourceComponent == null)
                {
                    return LaunchTaskResult.CreateFailed(TaskName, "Resource components are not ready.");
                }

                switch (_resourceComponent.ResourceMode)
                {
#if YOOASSET_SUPPORT
                    case ResourceMode.YooAsset:
                        return await ExecuteYooAssetAsync(context);
#endif

#if ADDRESSABLE_SUPPORT
                    case ResourceMode.Addressable:
                        return await ExecuteAddressableAsync(context);
#endif

                    default:
                        return LaunchTaskResult.CreateFailed(TaskName, $"Unsupported resource mode '{_resourceComponent.ResourceMode}'.");
                }
            }
            catch (Exception ex)
            {
                Log.Error("[DownloadResourceTask] Resource download exception: {0}", ex);
                return LaunchTaskResult.CreateFailed(TaskName, ex.Message);
            }
            finally
            {
                CleanupHandler();
            }
        }

#if YOOASSET_SUPPORT
        private async UniTask<LaunchTaskResult> ExecuteYooAssetAsync(LaunchContext context)
        {
            ResourceComponentSetting setting = LoadResourceComponentSetting();
            if (setting == null)
            {
                return LaunchTaskResult.CreateFailed(TaskName, "ResourceComponentSetting is null.");
            }
            
            string handlerName = GetDownloadHandlerName(context);
            bool checkDownloadedTags = GetCheckDownloadedTags(context);
            List<YooAssetPackageDownloadPlan> plans = YooAssetMultiPackageUtility.CollectDownloadPlans(
                setting,
                Application.platform,
                GetCurrentChannel(),
                GetGlobalDownloadLabels(context));

            if (plans.Count == 0)
            {
                Log.Info("[DownloadResourceTask] No YooAsset package download plans were generated.");
                return LaunchTaskResult.CreateSuccess(TaskName);
            }

            _currentPlanCount = plans.Count;
            for (int i = 0; i < plans.Count; i++)
            {
                YooAssetPackageDownloadPlan plan = plans[i];
                _currentPlanIndex = i;
                _currentPlanName = plan.PackageId;
                context.ProgressReporter.ReportProgress((float)i / plans.Count, $"Preparing package '{plan.PackageId}'...");

                Log.Info(
                    "[DownloadResourceTask] Create YooAsset download handler. packageId: {0}, packageName: {1}, labels: {2}, checkDownloadedTags: {3}",
                    plan.PackageId,
                    plan.PackageName,
                    string.Join(",", plan.Labels),
                    checkDownloadedTags);

                _handlerSerialId = _resourceDownloadComponent.AddYooAssetHandlerNotRun(
                    $"{handlerName}:{plan.PackageId}",
                    new List<string>(plan.Labels),
                    plan.PackageName,
                    false,
                    checkDownloadedTags);

                IResourceDownloadHandler handler = _resourceDownloadComponent.GetHandler(_handlerSerialId);
                if (handler == null)
                {
                    return LaunchTaskResult.CreateFailed(TaskName, $"Failed to create download handler for package '{plan.PackageId}'.");
                }

                bool succeeded = await RunHandlerAsync(handler);
                CleanupHandler(handler);
                if (!succeeded)
                {
                    string errorMessage = string.IsNullOrWhiteSpace(_errorMessage)
                        ? $"Resource download failed for package '{plan.PackageId}'."
                        : _errorMessage;
                    return LaunchTaskResult.CreateFailed(TaskName, errorMessage);
                }
            }

            context.ProgressReporter.ReportProgress(1f, "Refreshing route index...");
            await _resourceComponent.RefreshRouteIndexAsync();
            Log.Info("[DownloadResourceTask] All YooAsset package downloads completed.");
            return LaunchTaskResult.CreateSuccess(TaskName);
        }
#endif

#if ADDRESSABLE_SUPPORT
        private async UniTask<LaunchTaskResult> ExecuteAddressableAsync(LaunchContext context)
        {
            List<string> labels = GetGlobalDownloadLabels(context);
            if (labels.Count == 0)
            {
                Log.Info("[DownloadResourceTask] No Addressables labels configured, skip download.");
                return LaunchTaskResult.CreateSuccess(TaskName);
            }

            string handlerName = GetDownloadHandlerName(context);
            Addressables.MergeMode mergeMode = GetMergeMode(context);

            Log.Info("[DownloadResourceTask] Create Addressables download handler. mergeMode: {0}, labels: {1}",
                mergeMode,
                string.Join(",", labels));

            _handlerSerialId = _resourceDownloadComponent.AddUpdateHandlerNotRun(
                handlerName,
                labels,
                mergeMode,
                false);

            IResourceDownloadHandler handler = _resourceDownloadComponent.GetHandler(_handlerSerialId);
            if (handler == null)
            {
                return LaunchTaskResult.CreateFailed(TaskName, "Failed to create Addressables download handler.");
            }

            bool succeeded = await RunHandlerAsync(handler);
            CleanupHandler(handler);
            return succeeded
                ? LaunchTaskResult.CreateSuccess(TaskName)
                : LaunchTaskResult.CreateFailed(TaskName, _errorMessage ?? "Resource download failed.");
        }
#endif

        private List<string> GetGlobalDownloadLabels(LaunchContext context)
        {
            var labels = new List<string>(context.GetDownloadLabels() ?? new List<string>());
            string initLabel = SettingManager.GetSetting<HybridCLRSetting>()?.defaultInitLabel;
            if (!string.IsNullOrWhiteSpace(initLabel) && !labels.Contains(initLabel))
            {
                labels.Add(initLabel);
            }

            return labels;
        }

        private async UniTask<bool> RunHandlerAsync(IResourceDownloadHandler handler)
        {
            _tcs = new UniTaskCompletionSource<bool>();
            _errorMessage = null;
            handler.DownloadSuccessfulEventHandler += OnDownloadSuccessful;
            handler.DownloadFailureEventHandler += OnDownloadFailure;
            handler.DownloadUpdateEventHandler += OnDownloadUpdate;
            handler.DownloadStepEventHandler += OnDownloadStep;
            handler.CheckAndLoadAsync();

            Log.Info("[DownloadResourceTask] Download handler started: {0}", handler.Name);
            return await _tcs.Task;
        }

        protected override bool ShouldRetry(LaunchTaskResult result, Exception exception, LaunchErrorCategory errorCategory,
            LaunchContext context)
        {
            return errorCategory == LaunchErrorCategory.Network ||
                   errorCategory == LaunchErrorCategory.Timeout ||
                   errorCategory == LaunchErrorCategory.Server;
        }

        protected override LaunchErrorCategory ClassifyFailure(LaunchTaskResult result, Exception exception, LaunchContext context)
        {
            string errorMessage = result?.ErrorMessage ?? exception?.Message;
            if (string.IsNullOrWhiteSpace(errorMessage))
            {
                return LaunchErrorCategory.Unknown;
            }

            if (ContainsAny(errorMessage, "timeout", "timed out", "超时"))
            {
                return LaunchErrorCategory.Timeout;
            }

            if (ContainsAny(errorMessage, "label", "handler", "not ready", "setting"))
            {
                return LaunchErrorCategory.Config;
            }

            if (ContainsAny(errorMessage, "catalog", "manifest", "package", "server"))
            {
                return LaunchErrorCategory.Server;
            }

            if (ContainsAny(errorMessage, "network", "downloadfailure", "notreachable", "reachable", "连接"))
            {
                return LaunchErrorCategory.Network;
            }

            return LaunchErrorCategory.Unknown;
        }

        protected override UniTask BeforeRetryAsync(int attempt, LaunchContext context, LaunchTaskResult result, Exception exception)
        {
            CleanupHandler();
            string message = $"Resource download failed, retrying ({attempt}/{GetMaxRetryCount(context)})...";
            Log.Warning("[DownloadResourceTask] {0} Error: {1}", message, result?.ErrorMessage ?? exception?.Message);
            context.ProgressReporter.ReportProgress(0f, message);
            return UniTask.CompletedTask;
        }

        protected override UniTask AfterFailureAsync(LaunchContext context, LaunchTaskResult result, Exception exception)
        {
            CleanupHandler();
            return UniTask.CompletedTask;
        }

        protected override int GetMaxRetryCount(LaunchContext context)
        {
            return context.GetCustomData("DownloadRetryCount", context.DefaultRetryCount);
        }

        protected virtual void OnDownloadStep(object sender, ResourceDownloadStepEvent e)
        {
        }

        protected virtual void OnDownloadSuccessful(object sender, ResourcesDownloadSuccessfulEvent e)
        {
            _tcs?.TrySetResult(true);
        }

        protected virtual void OnDownloadFailure(object sender, ResourcesDownloadFailureEvent e)
        {
            _errorMessage = $"Resource download failed, result type: {e.UpdateResultType}";
            _tcs?.TrySetResult(false);
        }

        protected virtual void OnDownloadUpdate(object sender, ResourcesDownloadUpdateEvent e)
        {
            float progress = _currentPlanCount <= 1
                ? e.Progress
                : (_currentPlanIndex + e.Progress) / _currentPlanCount;
            string packagePrefix = string.IsNullOrWhiteSpace(_currentPlanName) ? string.Empty : $"[{_currentPlanName}] ";
            string message = string.IsNullOrEmpty(e.TotalDownloadSize)
                ? $"{packagePrefix}Downloading resources {e.Progress:P0}"
                : $"{packagePrefix}Downloading resources {e.DownloadSize}/{e.TotalDownloadSize}";
            _context?.ProgressReporter.ReportProgress(progress, message);
        }

        protected virtual string GetDownloadHandlerName(LaunchContext context)
        {
            return context.GetCustomData("DownloadHandlerName", "LaunchDownload");
        }

#if ADDRESSABLE_SUPPORT
        protected virtual Addressables.MergeMode GetMergeMode(LaunchContext context)
        {
            return context.GetCustomData("MergeMode", Addressables.MergeMode.Union);
        }
#endif

        protected virtual bool GetCheckDownloadedTags(LaunchContext context)
        {
            return context.GetCustomData("CheckDownloadedTags", false);
        }

        private void CleanupHandler(IResourceDownloadHandler handler = null)
        {
            handler ??= _handlerSerialId > 0 ? _resourceDownloadComponent?.GetHandler(_handlerSerialId) : null;
            if (handler != null)
            {
                handler.DownloadSuccessfulEventHandler -= OnDownloadSuccessful;
                handler.DownloadFailureEventHandler -= OnDownloadFailure;
                handler.DownloadUpdateEventHandler -= OnDownloadUpdate;
                handler.DownloadStepEventHandler -= OnDownloadStep;
            }

            if (_handlerSerialId > 0)
            {
                _resourceDownloadComponent?.RemoveHandler(_handlerSerialId);
                Log.Info("[DownloadResourceTask] Download handler cleaned up. SerialID: {0}", _handlerSerialId);
            }

            _handlerSerialId = 0;
            _tcs = null;
            _errorMessage = null;
        }

        private static ResourceComponentSetting LoadResourceComponentSetting()
        {
            return SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();
        }

        private static string GetCurrentChannel()
        {
            GameSetting gameSetting = SettingManager.GetSetting<GameSetting>();
            return gameSetting != null ? gameSetting.channel : string.Empty;
        }

        private static bool ContainsAny(string source, params string[] values)
        {
            foreach (string value in values)
            {
                if (source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
