#if YOOASSET_SUPPORT
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;
using YooAsset;

namespace LFramework.Runtime
{
    /// <summary>
    /// Downloads YooAsset resources for explicit tags or asset locations without running version or manifest update flow.
    /// </summary>
    public class YooAssetAssetDownloadHandler : ResourceDownloadHandlerBase
    {
        private readonly string _packageName;
        private readonly bool _downloadByLocation;
        private readonly Func<string, UniTask<PackageInitializationResult>> _ensurePackageReadyAsync;
        private ResourceDownloaderOperation _downloaderOperation;

        public YooAssetAssetDownloadHandler(string name, List<string> updateKeys,
            string packageName, int serialID, bool autoReleaseHandle, bool downloadByLocation,
            Func<string, UniTask<PackageInitializationResult>> ensurePackageReadyAsync)
            : base(name, updateKeys, serialID, autoReleaseHandle)
        {
            _packageName = packageName;
            _downloadByLocation = downloadByLocation;
            _ensurePackageReadyAsync = ensurePackageReadyAsync;
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_downloaderOperation == null || _downloaderOperation.IsDone)
            {
                return;
            }

            long totalBytes = _downloaderOperation.TotalDownloadBytes;
            long currentBytes = _downloaderOperation.CurrentDownloadBytes;
            CalculateSpeed(currentBytes);

            if (totalBytes <= 0)
            {
                return;
            }

            float progress = currentBytes / (float)totalBytes;
            SendProgress(progress, currentBytes);
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
                ExceptionFailure(UpdateResultType.GetDownloadSizeFailure);
            }
        }

        private async Task CheckAndLoadInternalAsync()
        {
            if (_ensurePackageReadyAsync != null)
            {
                PackageInitializationResult ready = await _ensurePackageReadyAsync(_packageName);
                if (!ready.Succeeded)
                {
                    LogError($"Package '{_packageName}' is not ready: {ready.ErrorMessage}");
                    ExceptionFailure(UpdateResultType.CheckCatalogsFailure);
                    return;
                }
            }

            ResourcePackage package = YooAssets.TryGetPackage(_packageName);
            if (package == null)
            {
                LogError($"Package '{_packageName}' is not initialized.");
                ExceptionFailure(UpdateResultType.CheckCatalogsFailure);
                return;
            }

            StepEvent(ResourceDownloadStep.DownloadKey, _updateLabels);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            _downloaderOperation = _downloadByLocation
                ? package.CreateBundleDownloader(_updateLabels.ToArray(), downloadingMaxNum, failedTryAgain)
                : package.CreateResourceDownloader(_updateLabels.ToArray(), downloadingMaxNum, failedTryAgain);

            _totalDownloadSize = _downloaderOperation.TotalDownloadBytes;
            StepEvent(ResourceDownloadStep.GetDownloadSizeSuccessful, _totalDownloadSize);

            if (_downloaderOperation.TotalDownloadCount == 0)
            {
                LogInfo("No YooAsset resources need downloading.");
                DownloadSuccessful();
                return;
            }

            LogInfo($"Need download {_downloaderOperation.TotalDownloadCount} files, total size {ByteToMb(_totalDownloadSize)}");
            _downloaderOperation.BeginDownload();
            await _downloaderOperation.Task;

            if (_downloaderOperation.Status == EOperationStatus.Succeed)
            {
                DownloadSuccessful();
                LogInfo("Download Successful.===============> ");
            }
            else
            {
                LogError($"Download failed: {_downloaderOperation.Error}");
                ExceptionFailure(UpdateResultType.DownloadFailure);
            }
        }
    }
}
#endif
