#if ADDRESSABLE_SUPPORT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace LFramework.Runtime
{
    /// <summary>
    /// Downloads Addressables dependencies for explicit asset keys without running catalog update flow.
    /// </summary>
    public class AddressableAssetDownloadHandler : ResourceDownloadHandlerBase
    {
        private readonly Addressables.MergeMode _addressableMergeMode;
        private AsyncOperationHandle _downloadHandle;
        private const int MaxRetryCount = 5;

        public AddressableAssetDownloadHandler(string name, List<string> updateKeys,
            Addressables.MergeMode mergeMode, int serialID, bool autoReleaseHandle)
            : base(name, updateKeys, serialID, autoReleaseHandle)
        {
            _addressableMergeMode = mergeMode;
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (!_downloadHandle.IsValid() || _downloadHandle.IsDone)
            {
                return;
            }

            if (_downloadHandle.Status == AsyncOperationStatus.Failed)
            {
                return;
            }

            var status = _downloadHandle.GetDownloadStatus();
            long downloadedBytes = status.DownloadedBytes;
            CalculateSpeed(downloadedBytes);

            if (_totalDownloadSize <= 0)
            {
                return;
            }

            float progress = downloadedBytes / (float)_totalDownloadSize;
            SendProgress(progress, downloadedBytes);
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
            StepEvent(ResourceDownloadStep.DownloadKey, _updateLabels);
            UpdateResultType result = await GetDownloadSizeAsync(_updateLabels);
            if (result is UpdateResultType.NotReachable or UpdateResultType.GetDownloadSizeFailure)
            {
                ExceptionFailure(result);
                return;
            }

            StepEvent(ResourceDownloadStep.GetDownloadSizeSuccessful, _totalDownloadSize);
            if (result == UpdateResultType.NoneDownload)
            {
                DownloadSuccessful();
                return;
            }

            await DownloadAsync(_updateLabels);
        }

        private async Task<UpdateResultType> GetDownloadSizeAsync(List<string> keys)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                LogError("Network is not reachable, cannot download Addressables assets.");
                return UpdateResultType.NotReachable;
            }

            bool isSuccessful = false;
            AsyncOperationHandle<long> downloadSizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)keys);
            try
            {
                await downloadSizeHandle.Task;
                _totalDownloadSize = downloadSizeHandle.Result;
                isSuccessful = downloadSizeHandle.Status == AsyncOperationStatus.Succeeded;
            }
            catch (Exception ex)
            {
                LogError($"GetDownloadSizeAsync failed: {ex.Message}");
            }
            finally
            {
                if (downloadSizeHandle.IsValid())
                {
                    Addressables.Release(downloadSizeHandle);
                }
            }

            if (!isSuccessful)
            {
                return UpdateResultType.GetDownloadSizeFailure;
            }

            if (_totalDownloadSize == 0)
            {
                LogInfo("No Addressables asset dependencies need downloading.");
                return UpdateResultType.NoneDownload;
            }

            LogInfo($"Need download total size: {ByteToMb(_totalDownloadSize)}");
            return UpdateResultType.Successful;
        }

        private async Task DownloadAsync(List<string> downloadKeys)
        {
            int retryCount = 0;
            while (retryCount < MaxRetryCount)
            {
                if (_downloadHandle.IsValid())
                {
                    Addressables.Release(_downloadHandle);
                }

                _downloadHandle = Addressables.DownloadDependenciesAsync((IEnumerable)downloadKeys, _addressableMergeMode, false);
                await _downloadHandle.Task;
                if (_downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    break;
                }

                retryCount++;
                int delayMs = Math.Min(500 * (1 << retryCount), 16000);
                LogError($"Download failed, retry {retryCount}/{MaxRetryCount} after {delayMs}ms");
                await Task.Delay(delayMs);
            }

            if (_downloadHandle.IsValid())
            {
                bool succeeded = _downloadHandle.Status == AsyncOperationStatus.Succeeded;
                Addressables.Release(_downloadHandle);

                if (!succeeded)
                {
                    LogError($"Download failed after {MaxRetryCount} retries.");
                    ExceptionFailure(UpdateResultType.DownloadFailure);
                    return;
                }
            }

            DownloadSuccessful();
            LogInfo("Download Successful.===============> ");
        }
    }
}
#endif
