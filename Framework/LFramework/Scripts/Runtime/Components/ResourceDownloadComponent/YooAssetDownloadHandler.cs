using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;
using YooAsset;

namespace LFramework.Runtime
{
    /// <summary>
    /// 基于 YooAsset 的资源下载处理器实现类
    /// 支持按 Tag 下载和已下载 Tag 的增量更新检查
    /// </summary>
    public class YooAssetDownloadHandler : ResourceDownloadHandlerBase
    {
        private readonly string _packageName;
        private ResourceDownloaderOperation _downloaderOperation;

        /// <summary>
        /// 是否同时检查已下载过的 Tag 的增量更新
        /// 用于强更阶段：除了下载 force_update 的资源，还要更新之前已下载过的 Tag（如 silent_download）中变化的资源
        /// </summary>
        private readonly bool _checkDownloadedTags;

        public YooAssetDownloadHandler(string name, List<string> updateLabels,
            string packageName, int serialID, bool autoReleaseHandle,
            bool checkDownloadedTags = false)
            : base(name, updateLabels, serialID, autoReleaseHandle)
        {
            _packageName = packageName;
            _checkDownloadedTags = checkDownloadedTags;
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_downloaderOperation == null || _downloaderOperation.IsDone)
            {
                return;
            }

            var totalBytes = _downloaderOperation.TotalDownloadBytes;
            var currentBytes = _downloaderOperation.CurrentDownloadBytes;
            CalculateSpeed(currentBytes);

            if (totalBytes <= 0)
            {
                return;
            }

            var progress = (float)currentBytes / totalBytes;
            SendProgress(progress * 0.9f + 0.1f, currentBytes);
        }

        public override async void CheckAndLoadAsync()
        {
            try
            {
                await CheckAndLoadInternalAsync();
            }
            catch (Exception ex)
            {
                LogError($"CheckAndLoadAsync unhandled exception: {ex}");
                ExceptionFailure(UpdateResultType.CheckCatalogsFailure);
            }
        }

        private async Task CheckAndLoadInternalAsync()
        {
            var package = YooAssets.GetPackage(_packageName);

            // Step 1: 请求资源包版本
            var versionOperation = package.RequestPackageVersionAsync();
            await versionOperation.Task;
            if (versionOperation.Status != EOperationStatus.Succeed)
            {
                LogError($"请求资源包版本失败: {versionOperation.Error}");
                ExceptionFailure(UpdateResultType.CheckCatalogsFailure);
                return;
            }

            var packageVersion = versionOperation.PackageVersion;
            LogInfo($"请求资源包版本成功: {packageVersion}");
            StepEvent(ResourceDownloadStep.CatalogSuccessful);

            // Step 2: 更新资源清单
            var manifestOperation = package.UpdatePackageManifestAsync(packageVersion);
            await manifestOperation.Task;
            if (manifestOperation.Status != EOperationStatus.Succeed)
            {
                LogError($"更新资源清单失败: {manifestOperation.Error}");
                ExceptionFailure(UpdateResultType.CheckCatalogsFailure);
                return;
            }

            LogInfo("更新资源清单成功。");
            StepEvent(ResourceDownloadStep.UpdateCatalogsSuccessful);

            // Step 3: 构建需要下载的 Tag 列表
            var tagsToDownload = new List<string>(_updateLabels);

            // 如果开启了已下载 Tag 检查，把之前下载过的 Tag 也加进来
            if (_checkDownloadedTags)
            {
                var downloadedTags = DownloadedTagTracker.GetDownloadedTags(_packageName);
                foreach (var tag in downloadedTags)
                {
                    if (!tagsToDownload.Contains(tag))
                    {
                        tagsToDownload.Add(tag);
                        LogInfo($"追加已下载 Tag 的增量检查: {tag}");
                    }
                }
            }

            // Step 4: 创建资源下载器（按 Tag 过滤）
            StepEvent(ResourceDownloadStep.DownloadKey, tagsToDownload);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            _downloaderOperation = package.CreateResourceDownloader(
                tagsToDownload.ToArray(), downloadingMaxNum, failedTryAgain);

            _totalDownloadSize = _downloaderOperation.TotalDownloadBytes;
            StepEvent(ResourceDownloadStep.GetDownloadSizeSuccessful, _totalDownloadSize);

            // Step 5: 检查下载大小，若为 0 则直接成功
            if (_downloaderOperation.TotalDownloadCount == 0)
            {
                LogInfo("没有需要下载的资源。");
                MarkTagsAsDownloaded(tagsToDownload);
                DownloadSuccessful();
                return;
            }

            LogInfo($"需要下载 {_downloaderOperation.TotalDownloadCount} 个文件，总大小: {ByteToMb(_totalDownloadSize)}");

            // Step 6: 执行下载
            _downloaderOperation.BeginDownload();
            await _downloaderOperation.Task;

            if (_downloaderOperation.Status == EOperationStatus.Succeed)
            {
                MarkTagsAsDownloaded(tagsToDownload);
                DownloadSuccessful();
                LogInfo("Download Successful.===============> ");
            }
            else
            {
                LogError($"下载资源失败: {_downloaderOperation.Error}");
                ExceptionFailure(UpdateResultType.DownloadFailure);
            }
        }

        /// <summary>
        /// 下载成功后记录已下载的 Tag
        /// </summary>
        private void MarkTagsAsDownloaded(List<string> tags)
        {
            foreach (var tag in tags)
            {
                DownloadedTagTracker.MarkTagDownloaded(_packageName, tag);
            }
        }
    }
}
