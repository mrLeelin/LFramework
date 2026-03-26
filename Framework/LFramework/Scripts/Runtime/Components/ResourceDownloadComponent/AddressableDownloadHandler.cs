#if ADDRESSABLE_SUPPORT
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// 基于 Unity Addressables 的资源下载处理器实现类
    /// </summary>
    public class AddressableDownloadHandler : ResourceDownloadHandlerBase
    {
        private readonly Addressables.MergeMode _addressableMergeMode;
        private AsyncOperationHandle<List<string>> _checkCatalogHandle;
        private AsyncOperationHandle<List<IResourceLocator>> _updateCatalogHandle;
        private AsyncOperationHandle _downloadHandle;
        private const int MaxRetryCount = 5;

        public AddressableDownloadHandler(string name, List<string> updateLabels,
            Addressables.MergeMode mergeMode, int serialID, bool autoReleaseHandle)
            : base(name, updateLabels, serialID, autoReleaseHandle)
        {
            _addressableMergeMode = mergeMode;
        }

        public override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            if (_checkCatalogHandle.IsValid() && !_checkCatalogHandle.IsDone)
            {
                // 0 -> 0.05
                SendProgress(_checkCatalogHandle.PercentComplete * 0.05f);
            }

            if (_updateCatalogHandle.IsValid() && !_updateCatalogHandle.IsDone)
            {
                //0.05 -> 0.1
                SendProgress(_updateCatalogHandle.PercentComplete * 0.05f + 0.05f);
            }

            if (!_downloadHandle.IsValid() || _downloadHandle.IsDone)
            {
                return;
            }

            if (_downloadHandle.Status == AsyncOperationStatus.Failed)
            {
                return;
            }

            var status = _downloadHandle.GetDownloadStatus();
            var needDownloadSize = status.TotalBytes - status.DownloadedBytes;
            var downloadSize = _totalDownloadSize - needDownloadSize;
            CalculateSpeed(downloadSize);
            if (downloadSize >= _totalDownloadSize)
            {
                return;
            }

            var progress = (downloadSize / (float)_totalDownloadSize);
            SendProgress(progress * 0.9f + 0.1f, downloadSize);
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
            var catalogsToUpdate = new List<string>();
            var result = await CheckCatalogs(catalogsToUpdate);
            if (result != UpdateResultType.Successful)
            {
                //检查Catalog 失败
                ExceptionFailure(result);
                return;
            }

            StepEvent(ResourceDownloadStep.CatalogSuccessful);
            if (catalogsToUpdate is { Count: > 0 })
            {
                result = await UpdateCatalogs(catalogsToUpdate);
                if (result != UpdateResultType.Successful)
                {
                    ExceptionFailure(result);
                    return;
                }
            }
            else
            {
                LogInfo("没有需要更新的资源目录。");
            }

            StepEvent(ResourceDownloadStep.UpdateCatalogsSuccessful);
            var firstDownloadKeys = GetInitDownedKeys();
            StepEvent(ResourceDownloadStep.DownloadKey, firstDownloadKeys);
            result = await GetDownloadSizeAsync(firstDownloadKeys);
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

            DownLoad(firstDownloadKeys);
        }

        /// <summary>
        /// 检查资源更新情况
        /// </summary>
        /// <param name="catalogsToUpdate"></param>
        /// <returns></returns>
        private async Task<UpdateResultType> CheckCatalogs(List<string> catalogsToUpdate)
        {
            _checkCatalogHandle = Addressables.CheckForCatalogUpdates(false);
            bool isSuccessful = false;
            try
            {
                catalogsToUpdate.AddRange(await _checkCatalogHandle.Task);
                isSuccessful = _checkCatalogHandle.Status == AsyncOperationStatus.Succeeded;
            }
            catch (System.Exception ex)
            {
                LogError($"检查资源目录更新时出错: {ex.Message}");
            }
            finally
            {
                if (_checkCatalogHandle.IsValid())
                {
                    Addressables.Release(_checkCatalogHandle);
                }
            }

            return isSuccessful ? UpdateResultType.Successful : UpdateResultType.CheckCatalogsFailure;
        }

        /// <summary>
        /// 更新CheckCatalogs返回的目录
        /// </summary>
        /// <param name="catalogs"></param>
        /// <returns></returns>
        private async Task<UpdateResultType> UpdateCatalogs(List<string> catalogs)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                LogError("网络连接不可用，无法更新资源目录。");
                return UpdateResultType.NotReachable;
            }

            bool isSuccessful = false;
            LogInfo($"发现 {catalogs.Count} 个资源目录需要更新，开始更新...");
            _updateCatalogHandle = Addressables.UpdateCatalogs(catalogs, false);
            try
            {
                await _updateCatalogHandle.Task;
                isSuccessful = _updateCatalogHandle.Status == AsyncOperationStatus.Succeeded;
                LogInfo("资源目录更新完成。");
            }
            catch (System.Exception ex)
            {
                LogError($"更新资源目录时出错: {ex.Message}");
            }
            finally
            {
                LogInfo("释放资源目录更新句柄。");

                if (_updateCatalogHandle.IsValid())
                {
                    Addressables.Release(_updateCatalogHandle);
                }
            }

            return isSuccessful ? UpdateResultType.Successful : UpdateResultType.NotReachable;
        }

        private async Task<UpdateResultType> GetDownloadSizeAsync(List<string> keys)
        {
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                LogError("网络连接不可用，无法更新资源目录。");
                return UpdateResultType.NotReachable;
            }

            bool isSuccessful = false;
            var downloadSizeHandle = Addressables.GetDownloadSizeAsync((IEnumerable)keys);
            try
            {
                LogInfo("Get Download Size Before.");
                await downloadSizeHandle.Task;
                _totalDownloadSize = downloadSizeHandle.Result;
                isSuccessful = downloadSizeHandle.Status == AsyncOperationStatus.Succeeded;
                LogInfo("Get DownloadSize Successful。");
            }
            catch (System.Exception ex)
            {
                LogError($"Get DownloadSize error: {ex.Message}");
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
                LogInfo("None DownloadSize.");
                return UpdateResultType.NoneDownload;
            }

            Log.Info("[Need Download] Total Size: " + ByteToMb(_totalDownloadSize));

            return UpdateResultType.Successful;
        }

        private async void DownLoad(List<string> downloadKeys)
        {
            int retryCount = 0;
            while (retryCount < MaxRetryCount)
            {
                if (_downloadHandle.IsValid())
                {
                    Addressables.Release(_downloadHandle);
                }

                _downloadHandle =
                    Addressables.DownloadDependenciesAsync((IEnumerable)downloadKeys, _addressableMergeMode,
                        false);
                await _downloadHandle.Task;
                if (_downloadHandle.Status == AsyncOperationStatus.Succeeded)
                {
                    break;
                }

                retryCount++;
                var delayMs = Math.Min(500 * (1 << retryCount), 16000);
                LogError($"Download failed, retry {retryCount}/{MaxRetryCount} after {delayMs}ms");
                await Task.Delay(delayMs);
            }

            if (_downloadHandle.IsValid())
            {
                var succeeded = _downloadHandle.Status == AsyncOperationStatus.Succeeded;
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

        /// <summary>
        /// 获取更新key
        /// </summary>
        /// <returns></returns>
        private List<string> GetInitDownedKeys()
        {
            return _updateLabels;
        }
    }
}
#endif


