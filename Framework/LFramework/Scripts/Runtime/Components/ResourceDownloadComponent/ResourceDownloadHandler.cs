using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameFramework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// 资源更新处理类
    /// </summary>
    public class ResourceDownloadHandler : IResourceDownloadHandler
    {
        private readonly string _handlerName;
        private readonly List<string> _updateLabels;
        private readonly Addressables.MergeMode _addressableMergeMode;
        private readonly int _serialID;
        private readonly bool _autoReleaseHandle;
        private float _lastUpdateTime;
        private long _lastDownloadedBytes;
        private float _downloadSpeed;

        private AsyncOperationHandle<List<string>> _checkCatalogHandle;
        private AsyncOperationHandle<List<IResourceLocator>> _updateCatalogHandle;
        private long _totalDownloadSize;
        private AsyncOperationHandle _downloadHandle;
        private EventHandler<ResourcesDownloadFailureEvent> _downloadFailureEventHandler;
        private EventHandler<ResourcesDownloadSuccessfulEvent> _downloadSuccessfulEventHandler;
        private EventHandler<ResourcesDownloadUpdateEvent> _downloadUpdateEventHandler;
        private EventHandler<ResourceDownloadStepEvent> _downloadStepEventHandler;
        public GameFrameworkAction<ResourceDownloadHandler> RemoveHandleAction;

        public string Name => _handlerName;
        public int SerialID => _serialID;

        public float DownloadSpeed => _downloadSpeed;

        public event EventHandler<ResourceDownloadStepEvent> DownloadStepEventHandler
        {
            add => _downloadStepEventHandler += value;
            remove => _downloadStepEventHandler -= value;
        }

        public event EventHandler<ResourcesDownloadFailureEvent> DownloadFailureEventHandler
        {
            add => _downloadFailureEventHandler += value;
            remove => _downloadFailureEventHandler -= value;
        }

        public event EventHandler<ResourcesDownloadSuccessfulEvent> DownloadSuccessfulEventHandler
        {
            add => _downloadSuccessfulEventHandler += value;
            remove => _downloadSuccessfulEventHandler -= value;
        }

        public event EventHandler<ResourcesDownloadUpdateEvent> DownloadUpdateEventHandler
        {
            add => _downloadUpdateEventHandler += value;
            remove => _downloadUpdateEventHandler -= value;
        }


        public ResourceDownloadHandler(string name, List<string> updateLabels, Addressables.MergeMode mergeMode,
            int serialID, bool autoReleaseHandle)
        {
            _serialID = serialID;
            _handlerName = name;
            _updateLabels = updateLabels;
            _addressableMergeMode = mergeMode;
            if (updateLabels == null || _updateLabels.Count == 0)
            {
                Log.Fatal($"The '{name}' update labels is null.");
            }

            _totalDownloadSize = 0L;
            _autoReleaseHandle = autoReleaseHandle;
            _downloadSpeed = 0F;
            _lastDownloadedBytes = 0L;
        }

        public void OnUpdate(float elapseSeconds, float realElapseSeconds)
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

        public async void CheckAndLoadAsync()
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

            //暂时先去掉移动网络区分
            /*

            if (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            {
                Log.Info("Use local area net work to download.");
                DownLoad(firstDownloadKeys);
            }
            else
            {
                //使用移动网络下载
                Log.Info("Use mobile network to download.");
                var mb = ByteToMbFloat(_totalDownloadSize);
                if (mb > 10f)
                {
                    MobileBigDownloadPopUp(_totalDownloadSize, firstDownloadKeys);
                }
                else
                {
                    DownLoad(firstDownloadKeys);
                }
            }
            */
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
            while (true)
            {
                if (_downloadHandle.IsValid())
                {
                    Addressables.Release(_downloadHandle);
                }

                _downloadHandle =
                    Addressables.DownloadDependenciesAsync((IEnumerable)downloadKeys, _addressableMergeMode,
                        false);
                await _downloadHandle.Task;
                if (_downloadHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    await Task.Delay(500);
                }
                else
                {
                    break;
                }
            }

            if (_downloadHandle.IsValid())
            {
                Addressables.Release(_downloadHandle);
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

        private void SendProgress(float progress, float downloadSize = 0)
        {
            if (_downloadUpdateEventHandler != null)
            {
                ResourcesDownloadUpdateEvent arg;
                if (downloadSize > 0)
                {
                    arg = ResourcesDownloadUpdateEvent.Create(progress, ByteToMb(downloadSize),
                        ByteToMb(_totalDownloadSize), _downloadSpeed);
                }
                else
                {
                    arg = ResourcesDownloadUpdateEvent.Create(progress);
                }

                _downloadUpdateEventHandler(this, arg);
                ReferencePool.Release(arg);
            }
        }


        private void DownloadSuccessful()
        {
            if (_downloadSuccessfulEventHandler != null)
            {
                var arg = ResourcesDownloadSuccessfulEvent.Create(this._serialID);
                _downloadSuccessfulEventHandler(this, arg);
                ReferencePool.Release(arg);
            }


            if (_autoReleaseHandle)
            {
                RemoveHandleAction(this);
            }
        }

        private void ExceptionFailure(UpdateResultType resultType)
        {
            if (_downloadFailureEventHandler != null)
            {
                var arg = ResourcesDownloadFailureEvent.Create(this._serialID, resultType);
                _downloadFailureEventHandler(this, arg);
                ReferencePool.Release(arg);
            }

            if (_autoReleaseHandle)
            {
                RemoveHandleAction(this);
            }
        }

        private void StepEvent(ResourceDownloadStep step, object customData = null)
        {
            if (_downloadStepEventHandler != null)
            {
                var arg = ResourceDownloadStepEvent.Create(this._serialID, step, customData);
                _downloadStepEventHandler(this, arg);
                ReferencePool.Release(arg);
            }
        }

        private void CalculateSpeed(long downloadSize)
        {
            var currentTime = Time.time;
            var timeDelta = currentTime - _lastUpdateTime;
            if (timeDelta <= 0.5f) // 每 0.5 秒计算一次
            {
                return;
            }

            var downloadedBytes = downloadSize;
            var bytesDelta = downloadedBytes - _lastDownloadedBytes;
            _downloadSpeed = bytesDelta / timeDelta; // 计算速度 (B/s)
            //Debug.Log($"下载速度: {speed / 1024:F2} KB/s");
            _lastDownloadedBytes = downloadedBytes;
            _lastUpdateTime = currentTime;
        }

        /*
         //暂时先去掉移动网络区分
        private void MobileBigDownloadPopUp(float size, List<string> firstDownloadKeys)
        {
            RuntimeOneBtnTips.RuntimeShowTips(
                TbRuntimeLocalLanguageData.Instance.GetText("need_download_title")
                , TbRuntimeLocalLanguageData.Instance.GetText("need_download_describe",
                    ByteToMb(size)),
                TbRuntimeLocalLanguageData.Instance.GetText("sure")
                , () => { DownLoad(firstDownloadKeys); });
        }
        */

        private void LogInfo(string message)
        {
            Log.Info($"Name '{_handlerName}' Message '{message}'");
        }

        private void LogError(string message)
        {
            Log.Error($"Name '{_handlerName}' Message '{message}'");
        }

        private static float ByteToMbFloat(float bytes)
        {
            var v = bytes * 1.0f / 1024 / 1024;
            return v;
        }

        private static string ByteToMb(float bytes)
        {
            var v = ByteToMbFloat(bytes);
            return $"{v:F2} MB";
        }
    }
}