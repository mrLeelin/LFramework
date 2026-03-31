#if YOOASSET_SUPPORT
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;
using YooAsset;

namespace LFramework.Runtime
{
    /// <summary>
    /// YooAsset 资源辅助器（平台初始化 + 查询）
    /// </summary>
    public class YooAssetResourceHelper : ResourceHelperBase
    {

        // ========== 资源 Handle 映射表（用于正确释放资源） ==========

        /// <summary>
        /// Asset InstanceID → YooAsset Handle 列表（支持同一资源多次加载）
        /// </summary>
        private readonly Dictionary<int, List<AssetHandle>> _assetHandles = new Dictionary<int, List<AssetHandle>>();

        /// <summary>
        /// Asset InstanceID → 引用计数
        /// </summary>
        private readonly Dictionary<int, int> _handleRefCounts = new Dictionary<int, int>();

        /// <summary>
        /// 实例化对象 InstanceID → 原始资源 InstanceID 映射
        /// </summary>
        private readonly Dictionary<int, int> _instanceToAssetMap = new Dictionary<int, int>();

        /// <summary>
        /// 场景名称 → SceneHandle 映射
        /// </summary>
        private readonly Dictionary<string, YooAsset.SceneHandle> _sceneHandles = new Dictionary<string, YooAsset.SceneHandle>();

        /// <summary>
        /// 二进制资源名称 → RawFileHandle 映射
        /// </summary>
        private readonly Dictionary<string, RawFileHandle> _rawFileHandles = new Dictionary<string, RawFileHandle>();

        private GameSetting _gameSetting;
        private SettingComponent _settingComponent;


        private void Awake()
        {
            _gameSetting = SettingManager.GetSetting<GameSetting>();
            _settingComponent = LFrameworkAspect.Instance.Get<SettingComponent>();
        }

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public override void InitializeResources(ResourceInitCallBack callback)
        {
            YooAssets.Initialize();
            var package = YooAssets.TryGetPackage(ResourceComponent.YooAssetPackageName)
                          ?? YooAssets.CreatePackage(ResourceComponent.YooAssetPackageName);
            YooAssets.SetDefaultPackage(package);

            InitializePackageAsync(package, callback);
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public override HasAssetResult HasAsset(string assetName)
        {
            var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
            if (package == null) return HasAssetResult.NotReady;
            return package.CheckLocationValid(assetName)
                ? HasAssetResult.Exist
                : HasAssetResult.NotExist;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Release(object asset)
        {
            if (asset == null) return;

            int instanceId = -1;
            if (asset is UnityEngine.Object unityObj)
            {
                instanceId = unityObj.GetInstanceID();
            }
            else
            {
                return;
            }

            // 检查是否是实例化对象
            if (_instanceToAssetMap.TryGetValue(instanceId, out int assetInstanceId))
            {
                _instanceToAssetMap.Remove(instanceId);
                instanceId = assetInstanceId;
            }

            // 查找并释放 Handle
            if (_handleRefCounts.TryGetValue(instanceId, out int refCount))
            {
                refCount--;
                _handleRefCounts[instanceId] = refCount;

                if (refCount <= 0)
                {
                    if (_assetHandles.TryGetValue(instanceId, out var handles))
                    {
                        foreach (var h in handles)
                        {
                            h.Release();
                        }
                        _assetHandles.Remove(instanceId);
                    }
                    _handleRefCounts.Remove(instanceId);
                }
            }
        }

        /// <summary>
        /// 释放二进制资源（通过资源名称）
        /// </summary>
        public void ReleaseBinary(string binaryAssetName)
        {
            if (string.IsNullOrEmpty(binaryAssetName)) return;

            if (_rawFileHandles.TryGetValue(binaryAssetName, out RawFileHandle handle))
            {
                handle.Release();
                _rawFileHandles.Remove(binaryAssetName);
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public override void UnloadScene(string sceneAssetName,
            UnloadSceneCallbacks callbacks, object userData)
        {
            var op = SceneManager.UnloadSceneAsync(sceneAssetName);
            if (op == null)
            {
                callbacks.UnloadSceneFailureCallback?.Invoke(sceneAssetName, userData);
                return;
            }

            op.completed += (_) =>
            {
                if (_sceneHandles.TryGetValue(sceneAssetName, out YooAsset.SceneHandle handle))
                {
                    handle.UnloadAsync();
                    _sceneHandles.Remove(sceneAssetName);
                }

                callbacks.UnloadSceneSuccessCallback?.Invoke(sceneAssetName, userData);
            };
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public override void LoadAsset(string assetName, Type assetType,
            LoadAssetCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
            var handle = package.LoadAssetAsync(assetName, assetType);
            StartCoroutine(WaitForAssetLoad(handle, assetName, callbacks, userData));
        }

        private IEnumerator WaitForAssetLoad(AssetHandle handle, string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            while (!handle.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, handle.Progress, userData);
                yield return null;
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                var asset = handle.AssetObject;
                if (asset != null)
                {
                    int instanceId = asset.GetInstanceID();

                    if (_assetHandles.TryGetValue(instanceId, out var handles))
                    {
                        handles.Add(handle);
                        _handleRefCounts[instanceId]++;
                    }
                    else
                    {
                        _assetHandles[instanceId] = new List<AssetHandle> { handle };
                        _handleRefCounts[instanceId] = 1;
                    }
                }

                callbacks.LoadAssetSuccessCallback?.Invoke(
                    assetName, asset, 0f, userData);
            }
            else
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName, LoadResourceStatus.NotExist,
                    handle.LastError ?? "Load failed.", userData);
            }
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public override void LoadScene(string sceneAssetName,
            LoadSceneCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
            var handle = package.LoadSceneAsync(sceneAssetName);
            StartCoroutine(WaitForSceneLoad(handle, sceneAssetName, callbacks, userData));
        }

        private IEnumerator WaitForSceneLoad(YooAsset.SceneHandle handle, string sceneAssetName,
            LoadSceneCallbacks callbacks, object userData)
        {
            while (!handle.IsDone)
            {
                callbacks.LoadSceneUpdateCallback?.Invoke(sceneAssetName, handle.Progress, userData);
                yield return null;
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                _sceneHandles[sceneAssetName] = handle;

                callbacks.LoadSceneSuccessCallback?.Invoke(
                    sceneAssetName, 0f, userData);
            }
            else
            {
                callbacks.LoadSceneFailureCallback?.Invoke(
                    sceneAssetName, LoadResourceStatus.NotExist,
                    handle.LastError ?? "Load scene failed.", userData);
            }
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public override void LoadBinary(string binaryAssetName,
            LoadBinaryCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
            var handle = package.LoadRawFileAsync(binaryAssetName);

            handle.Completed += (op) =>
            {
                if (op.Status == EOperationStatus.Succeed)
                {
                    _rawFileHandles[binaryAssetName] = handle;

                    callbacks.LoadBinarySuccessCallback?.Invoke(
                        binaryAssetName, op.GetRawFileData(), 0f, userData);
                }
                else
                {
                    callbacks.LoadBinaryFailureCallback?.Invoke(
                        binaryAssetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load binary failed.", userData);
                }
            };
        }

        /// <summary>
        /// 实例化资源
        /// </summary>
        public override void InstantiateAsset(string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
            var handle = package.LoadAssetAsync<GameObject>(assetName);
            StartCoroutine(WaitForInstantiateAsset(handle, assetName, callbacks, userData));
        }

        private IEnumerator WaitForInstantiateAsset(AssetHandle handle, string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            while (!handle.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, handle.Progress, userData);
                yield return null;
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                var instantiateOp = handle.InstantiateAsync();

                while (!instantiateOp.IsDone)
                {
                    yield return null;
                }

                if (instantiateOp.Status == EOperationStatus.Succeed &&
                    instantiateOp.Result != null &&
                    handle.AssetObject != null)
                {
                    int instanceId = instantiateOp.Result.GetInstanceID();
                    int assetInstanceId = handle.AssetObject.GetInstanceID();

                    _instanceToAssetMap[instanceId] = assetInstanceId;

                    if (_assetHandles.TryGetValue(assetInstanceId, out var handles))
                    {
                        handles.Add(handle);
                        _handleRefCounts[assetInstanceId]++;
                    }
                    else
                    {
                        _assetHandles[assetInstanceId] = new List<AssetHandle> { handle };
                        _handleRefCounts[assetInstanceId] = 1;
                    }
                }

                callbacks.LoadAssetSuccessCallback?.Invoke(assetName, instantiateOp.Result, 0f, userData);
            }
            else
            {
                callbacks.LoadAssetFailureCallback?.Invoke(assetName, LoadResourceStatus.NotExist,
                    handle.LastError ?? "Instantiate failed.", userData);
            }
        }

        // ─── Handle 异步 API 实现 ───

        /// <summary>
        /// 异步加载资源（返回 Handle）
        /// </summary>
        public override ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceAssetHandle<T>>();
            resourceHandle.MarkFromPool();
            LoadAssetHandleAsync(resourceHandle, assetName);
            return resourceHandle;
        }

        private async void LoadAssetHandleAsync<T>(ResourceAssetHandle<T> handle, string assetName)
            where T : UnityEngine.Object
        {
            try
            {
                var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
                var op = package.LoadAssetAsync<T>(assetName);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.Progress);
                    await UniTask.Yield();
                }
                if (op.Status == EOperationStatus.Succeed)
                {
                    handle.RegisterReleaseAction(() => op.Release());
                    handle.SetResult(op.AssetObject as T);
                }
                else
                {
                    handle.SetError(op.LastError ?? $"Load asset '{assetName}' failed.");
                }
            }
            catch (Exception ex) { handle.SetError(ex.Message); }
        }

        /// <summary>
        /// 异步实例化资源（返回 Handle）
        /// </summary>
        public override ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceAssetHandle<GameObject>>();
            resourceHandle.MarkFromPool();
            InstantiateAssetHandleAsync(resourceHandle, assetName);
            return resourceHandle;
        }

        private async void InstantiateAssetHandleAsync(ResourceAssetHandle<GameObject> handle, string assetName)
        {
            try
            {
                var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
                var assetOp = package.LoadAssetAsync<GameObject>(assetName);
                while (!assetOp.IsDone)
                {
                    handle.SetProgress(assetOp.Progress * 0.5f);
                    await UniTask.Yield();
                }
                if (assetOp.Status != EOperationStatus.Succeed)
                {
                    handle.SetError(assetOp.LastError ?? $"Load asset '{assetName}' for instantiate failed.");
                    return;
                }
                var instOp = assetOp.InstantiateAsync();
                while (!instOp.IsDone)
                {
                    handle.SetProgress(0.5f + instOp.Progress * 0.5f);
                    await UniTask.Yield();
                }
                if (instOp.Status == EOperationStatus.Succeed && instOp.Result != null)
                {
                    var instance = instOp.Result;
                    handle.RegisterReleaseAction(() =>
                    {
                        if (instance != null) UnityEngine.Object.Destroy(instance);
                        assetOp.Release();
                    });
                    handle.SetResult(instance);
                }
                else
                {
                    assetOp.Release();
                    handle.SetError($"Instantiate asset '{assetName}' failed.");
                }
            }
            catch (Exception ex) { handle.SetError(ex.Message); }
        }

        /// <summary>
        /// 异步加载场景（返回 Handle）
        /// </summary>
        public override ResourceSceneHandle LoadSceneHandle(string sceneAssetName)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceSceneHandle>();
            resourceHandle.MarkFromPool();
            resourceHandle.SetSceneName(sceneAssetName);
            LoadSceneHandleAsync(resourceHandle, sceneAssetName);
            return resourceHandle;
        }

        private async void LoadSceneHandleAsync(ResourceSceneHandle handle, string sceneAssetName)
        {
            try
            {
                var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
                var op = package.LoadSceneAsync(sceneAssetName);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.Progress);
                    await UniTask.Yield();
                }
                if (op.Status == EOperationStatus.Succeed)
                {
                    handle.RegisterReleaseAction(() => op.UnloadAsync());
                    handle.SetCompleted();
                }
                else
                {
                    handle.SetError(op.LastError ?? $"Load scene '{sceneAssetName}' failed.");
                }
            }
            catch (Exception ex) { handle.SetError(ex.Message); }
        }

        /// <summary>
        /// 异步加载二进制/原始文件（返回 Handle）
        /// </summary>
        public override ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceRawFileHandle>();
            resourceHandle.MarkFromPool();
            LoadRawFileHandleAsync(resourceHandle, binaryAssetName);
            return resourceHandle;
        }

        private async void LoadRawFileHandleAsync(ResourceRawFileHandle handle, string binaryAssetName)
        {
            try
            {
                var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
                var op = package.LoadRawFileAsync(binaryAssetName);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.Progress);
                    await UniTask.Yield();
                }
                if (op.Status == EOperationStatus.Succeed)
                {
                    handle.RegisterReleaseAction(() => op.Release());
                    handle.SetResult(op.GetRawFileData());
                }
                else
                {
                    handle.SetError(op.LastError ?? $"Load binary '{binaryAssetName}' failed.");
                }
            }
            catch (Exception ex) { handle.SetError(ex.Message); }
        }

        /// <summary>
        /// 异步批量加载资源（通过标签，返回 Handle）
        /// </summary>
        public override ResourceBatchHandle<T> LoadAssetsByTagHandle<T>(string tag)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceBatchHandle<T>>();
            resourceHandle.MarkFromPool();
            LoadAssetsByTagHandleAsync(resourceHandle, tag);
            return resourceHandle;
        }

        private async void LoadAssetsByTagHandleAsync<T>(ResourceBatchHandle<T> handle, string tag)
            where T : UnityEngine.Object
        {
            try
            {
                var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
                var op = package.LoadAllAssetsAsync<T>(tag);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.Progress);
                    await UniTask.Yield();
                }
                if (op.Status == EOperationStatus.Succeed)
                {
                    var result = new List<T>();
                    foreach (var obj in op.AllAssetObjects)
                    {
                        if (obj is T typedObj) result.Add(typedObj);
                    }
                    handle.RegisterReleaseAction(() => op.Release());
                    handle.SetResult(result);
                }
                else
                {
                    handle.SetError(op.LastError ?? $"Load assets by tag '{tag}' failed.");
                }
            }
            catch (Exception ex) { handle.SetError(ex.Message); }
        }

        protected virtual async void InitializePackageAsync(ResourcePackage package, ResourceInitCallBack callback)
        {
            InitializationOperation initOperation = null;

            switch (ResourceComponent.YooAssetsPlayModel)
            {
                case YooAssetPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(ResourceComponent.YooAssetPackageName);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var createParameters = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot)
                    };
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.OfflinePlayMode:
                {
                    var createParameters = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters()
                    };
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.HostPlayMode:
                {
                    IRemoteServices remoteServices = BuildRemoteService(_settingComponent,_gameSetting);
                    var createParameters = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters(),
                        CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices)
                    };
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.WebPlayMode:
                {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                    var createParameters = new WebPlayModeParameters();
			        string defaultHostServer = GetHostServerURL();
                    string fallbackHostServer = GetHostServerURL();
                    string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE";
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    createParameters.WebServerFileSystemParameters =
                    WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices);
                    initOperation = package.InitializeAsync(createParameters);
#else
                    var createParameters = new WebPlayModeParameters
                    {
                        WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters()
                    };
                    initOperation = package.InitializeAsync(createParameters);
#endif
                }
                    break;
            }

            if (initOperation != null)
            {
                await initOperation.Task;
                if (initOperation.Status == EOperationStatus.Succeed)
                {
                    callback?.ResourceInitSuccessCallBack?.Invoke();
                }
                else
                {
                    callback?.ResourceInitFailureCallBack?.Invoke(
                        initOperation.Error ?? "YooAsset initialization failed.");
                }
            }
            else
            {
                callback?.ResourceInitFailureCallBack?.Invoke(
                    "Unknown YooAsset play mode, initialization failed.");
            }
        }

        protected virtual IRemoteServices BuildRemoteService(SettingComponent settingComponent, GameSetting gameSetting)
        {
            return new DefaultRemoteServices(settingComponent, gameSetting);
        }

        #region 调试工具方法

        /// <summary>
        /// 获取当前所有资源的引用计数信息
        /// </summary>
        public Dictionary<int, int> GetAllRefCounts()
        {
            return new Dictionary<int, int>(_handleRefCounts);
        }

        /// <summary>
        /// 获取当前加载的资源数量
        /// </summary>
        public int GetLoadedAssetCount()
        {
            return _assetHandles.Count;
        }

        /// <summary>
        /// 获取当前加载的场景数量
        /// </summary>
        public int GetLoadedSceneCount()
        {
            return _sceneHandles.Count;
        }

        /// <summary>
        /// 获取当前加载的二进制资源数量
        /// </summary>
        public int GetLoadedBinaryCount()
        {
            return _rawFileHandles.Count;
        }

        /// <summary>
        /// 打印所有资源的引用计数信息（用于调试）
        /// </summary>
        public void LogAllRefCounts()
        {
            UnityEngine.Debug.Log($"[YooAssetResourceHelper] 当前加载的资源数量: {_assetHandles.Count}");
            UnityEngine.Debug.Log($"[YooAssetResourceHelper] 当前加载的场景数量: {_sceneHandles.Count}");
            UnityEngine.Debug.Log($"[YooAssetResourceHelper] 当前加载的二进制资源数量: {_rawFileHandles.Count}");

            foreach (var kvp in _handleRefCounts)
            {
                UnityEngine.Debug.Log($"[YooAssetResourceHelper] InstanceID: {kvp.Key}, RefCount: {kvp.Value}");
            }
        }

        #endregion
    }
}
#endif
