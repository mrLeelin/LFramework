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
    /// </summary>
    public class YooAssetDownloadHandler : ResourceDownloadHandlerBase
    {
        private readonly string _packageName;
        private ResourceDownloaderOperation _downloaderOperation;

        public YooAssetDownloadHandler(string name, List<string> updateLabels,
            string packageName, int serialID, bool autoReleaseHandle)
            : base(name, updateLabels, serialID, autoReleaseHandle)
        {
            _packageName = packageName;
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

            // Step 3: 创建资源下载器
            StepEvent(ResourceDownloadStep.DownloadKey, _updateLabels);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            _downloaderOperation = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);

            _totalDownloadSize = _downloaderOperation.TotalDownloadBytes;
            StepEvent(ResourceDownloadStep.GetDownloadSizeSuccessful, _totalDownloadSize);

            // Step 4: 检查下载大小，若为 0 则直接成功
            if (_downloaderOperation.TotalDownloadCount == 0)
            {
                LogInfo("没有需要下载的资源。");
                DownloadSuccessful();
                return;
            }

            LogInfo($"需要下载 {_downloaderOperation.TotalDownloadCount} 个文件，总大小: {ByteToMb(_totalDownloadSize)}");

            // Step 5: 执行下载
            _downloaderOperation.BeginDownload();
            await _downloaderOperation.Task;

            if (_downloaderOperation.Status == EOperationStatus.Succeed)
            {
                DownloadSuccessful();
                LogInfo("Download Successful.===============> ");
            }
            else
            {
                LogError($"下载资源失败: {_downloaderOperation.Error}");
                ExceptionFailure(UpdateResultType.DownloadFailure);
            }
        }
    }
}
