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
        private ResourceComponentSetting _resourceComponentSetting;
        private readonly PackageRegistry _packageRegistry = new PackageRegistry();
        private PackageResolver _packageResolver;
        private bool _packageRegistryConfigured;
        private readonly HashSet<string> _initializedPackageNames = new HashSet<string>(StringComparer.Ordinal);
        private PackageInitializationCoordinator _packageInitializationCoordinator;


        private void Awake()
        {
            _gameSetting = SettingManager.GetSetting<GameSetting>();
            _settingComponent = LFrameworkAspect.Instance.Get<SettingComponent>();
            _resourceComponentSetting = SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();
        }

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public override void InitializeResources(ResourceInitCallBack callback)
        {
            YooAssets.Initialize();
            EnsurePackageRegistryConfigured();
            EnsurePackageInitializationCoordinator();
            string defaultPackageName = ResolveYooAssetPackageName(null);
            var package = YooAssets.TryGetPackage(defaultPackageName)
                          ?? YooAssets.CreatePackage(defaultPackageName);
            YooAssets.SetDefaultPackage(package);

            InitializePackageAsync(defaultPackageName, callback);
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public override HasAssetResult HasAsset(string assetName)
        {
            var package = GetLoadedPackage(null);
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
            LoadAsset(assetName, assetType, null, callbacks, userData);
        }

        public override void LoadAsset(string assetName, Type assetType, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            LoadAssetInternalAsync(assetName, assetType, packageId, callbacks, userData).Forget();
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
            LoadScene(sceneAssetName, null, callbacks, userData);
        }

        public override void LoadScene(string sceneAssetName, string packageId,
            LoadSceneCallbacks callbacks, object userData)
        {
            LoadSceneInternalAsync(sceneAssetName, packageId, callbacks, userData).Forget();
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
            LoadBinary(binaryAssetName, null, callbacks, userData);
        }

        public override void LoadBinary(string binaryAssetName, string packageId,
            LoadBinaryCallbacks callbacks, object userData)
        {
            LoadBinaryInternalAsync(binaryAssetName, packageId, callbacks, userData).Forget();
        }

        /// <summary>
        /// 实例化资源
        /// </summary>
        public override async void InstantiateAsset(string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            InstantiateAsset(assetName, null, callbacks, userData);
        }

        public override async void InstantiateAsset(string assetName, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            await InstantiateAssetInternalAsync(assetName, packageId, callbacks, userData);
        }

        // ─── Handle 异步 API 实现 ───

        /// <summary>
        /// 异步加载资源（返回 Handle）
        /// </summary>
        private async UniTask LoadAssetInternalAsync(string assetName, Type assetType, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? $"Package '{ResolveYooAssetPackageName(assetName, packageId)}' is not initialized.",
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    $"Package '{ready.PackageName}' is unavailable after initialization.",
                    userData);
                return;
            }

            AssetHandle handle = package.LoadAssetAsync(assetName, assetType);
            while (!handle.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, handle.Progress, userData);
                await UniTask.Yield();
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                UnityEngine.Object asset = handle.AssetObject;
                if (asset != null)
                {
                    int instanceId = asset.GetInstanceID();
                    if (_assetHandles.TryGetValue(instanceId, out List<AssetHandle> handles))
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

                callbacks.LoadAssetSuccessCallback?.Invoke(assetName, asset, 0f, userData);
            }
            else
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotExist,
                    handle.LastError ?? "Load failed.",
                    userData);
            }
        }

        private async UniTask LoadSceneInternalAsync(string sceneAssetName, string packageId,
            LoadSceneCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(sceneAssetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadSceneFailureCallback?.Invoke(
                    sceneAssetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? $"Package '{ResolveYooAssetPackageName(sceneAssetName, packageId)}' is not initialized.",
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadSceneFailureCallback?.Invoke(
                    sceneAssetName,
                    LoadResourceStatus.NotReady,
                    $"Package '{ready.PackageName}' is unavailable after initialization.",
                    userData);
                return;
            }

            YooAsset.SceneHandle handle = package.LoadSceneAsync(sceneAssetName, LoadSceneMode.Additive);
            while (!handle.IsDone)
            {
                callbacks.LoadSceneUpdateCallback?.Invoke(sceneAssetName, handle.Progress, userData);
                await UniTask.Yield();
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                _sceneHandles[sceneAssetName] = handle;
                callbacks.LoadSceneSuccessCallback?.Invoke(sceneAssetName, 0f, userData);
            }
            else
            {
                callbacks.LoadSceneFailureCallback?.Invoke(
                    sceneAssetName,
                    LoadResourceStatus.NotExist,
                    handle.LastError ?? "Load scene failed.",
                    userData);
            }
        }

        private async UniTask LoadBinaryInternalAsync(string binaryAssetName, string packageId,
            LoadBinaryCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(binaryAssetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadBinaryFailureCallback?.Invoke(
                    binaryAssetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? $"Package '{ResolveYooAssetPackageName(binaryAssetName, packageId)}' is not initialized.",
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadBinaryFailureCallback?.Invoke(
                    binaryAssetName,
                    LoadResourceStatus.NotReady,
                    $"Package '{ready.PackageName}' is unavailable after initialization.",
                    userData);
                return;
            }

            RawFileHandle handle = package.LoadRawFileAsync(binaryAssetName);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                _rawFileHandles[binaryAssetName] = handle;
                callbacks.LoadBinarySuccessCallback?.Invoke(binaryAssetName, handle.GetRawFileData(), 0f, userData);
            }
            else
            {
                callbacks.LoadBinaryFailureCallback?.Invoke(
                    binaryAssetName,
                    LoadResourceStatus.NotExist,
                    handle.LastError ?? "Load binary failed.",
                    userData);
                handle.Release();
            }
        }

        private async UniTask InstantiateAssetInternalAsync(string assetName, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? $"Package '{ResolveYooAssetPackageName(assetName, packageId)}' is not initialized.",
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    $"Package '{ready.PackageName}' is unavailable after initialization.",
                    userData);
                return;
            }

            AssetHandle handle = package.LoadAssetAsync<GameObject>(assetName);
            while (!handle.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, handle.Progress, userData);
                await UniTask.Yield();
            }

            if (handle.Status != EOperationStatus.Succeed)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotExist,
                    handle.LastError ?? "Instantiate failed.",
                    userData);
                return;
            }

            var instantiateOp = handle.InstantiateAsync();
            while (!instantiateOp.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, 0.5f + instantiateOp.Progress * 0.5f, userData);
                await UniTask.Yield();
            }

            if (instantiateOp.Status == EOperationStatus.Succeed &&
                instantiateOp.Result != null &&
                handle.AssetObject != null)
            {
                int instanceId = instantiateOp.Result.GetInstanceID();
                int assetInstanceId = handle.AssetObject.GetInstanceID();
                _instanceToAssetMap[instanceId] = assetInstanceId;

                if (_assetHandles.TryGetValue(assetInstanceId, out List<AssetHandle> handles))
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

            if (instantiateOp.Status == EOperationStatus.Succeed)
            {
                callbacks.LoadAssetSuccessCallback?.Invoke(assetName, instantiateOp.Result, 0f, userData);
            }
            else
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotExist,
                    "Instantiate failed.",
                    userData);
                handle.Release();
            }
        }

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
                PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, null);
                if (!ready.Succeeded)
                {
                    handle.SetError(ready.ErrorMessage ?? $"Load asset '{assetName}' failed because its package is not ready.");
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError($"Package '{ready.PackageName}' is unavailable after initialization.");
                    return;
                }

                var op = package.LoadAssetAsync(assetName,typeof(System.Object));
                while (!op.IsDone)
                {
                    handle.SetProgress(op.Progress);
                    await UniTask.Yield();
                }
                if (op.Status == EOperationStatus.Succeed)
                {
                    if (ResourceAssetTypeUtility.TryConvertLoadedObject(
                            op.AssetObject,
                            typeof(T),
                            assetName,
                            out var typedAsset,
                            out var errorMessage))
                    {
                        handle.RegisterReleaseAction(() => op.Release());
                        handle.SetResult((T)typedAsset);
                    }
                    else
                    {
                        handle.SetError(errorMessage);
                        op.Release();
                    }
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
                PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, null);
                if (!ready.Succeeded)
                {
                    handle.SetError(ready.ErrorMessage ?? $"Instantiate asset '{assetName}' failed because its package is not ready.");
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError($"Package '{ready.PackageName}' is unavailable after initialization.");
                    return;
                }

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
                PackageInitializationResult ready = await EnsurePackageReadyAsync(sceneAssetName, null);
                if (!ready.Succeeded)
                {
                    handle.SetError(ready.ErrorMessage ?? $"Load scene '{sceneAssetName}' failed because its package is not ready.");
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError($"Package '{ready.PackageName}' is unavailable after initialization.");
                    return;
                }

                var op = package.LoadSceneAsync(sceneAssetName,LoadSceneMode.Additive);
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
                PackageInitializationResult ready = await EnsurePackageReadyAsync(binaryAssetName, null);
                if (!ready.Succeeded)
                {
                    handle.SetError(ready.ErrorMessage ?? $"Load binary '{binaryAssetName}' failed because its package is not ready.");
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError($"Package '{ready.PackageName}' is unavailable after initialization.");
                    return;
                }

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
                PackageInitializationResult ready = await EnsurePackageReadyAsync(null, null);
                if (!ready.Succeeded)
                {
                    handle.SetError(ready.ErrorMessage ?? $"Load assets by tag '{tag}' failed because the default package is not ready.");
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError($"Package '{ready.PackageName}' is unavailable after initialization.");
                    return;
                }

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

        protected virtual async void InitializePackageAsync(string packageName, ResourceInitCallBack callback)
        {
            EnsurePackageRegistryConfigured();
            EnsurePackageInitializationCoordinator();

            PackageInitializationResult result = await _packageInitializationCoordinator.EnsureInitializedAsync(packageName);
            if (!result.Succeeded)
            {
                callback?.ResourceInitFailureCallBack?.Invoke(result.ErrorMessage ?? "YooAsset initialization failed.");
                return;
            }

            await TryLoadBootstrapRouteIndexAsync();
            await PrewarmConfiguredPackagesAsync(packageName);
            callback?.ResourceInitSuccessCallBack?.Invoke();
        }

        private void EnsurePackageInitializationCoordinator()
        {
            _packageInitializationCoordinator ??= new PackageInitializationCoordinator(
                IsPackageInitialized,
                InitializePackageCoreAsync);
        }

        private bool IsPackageInitialized(string packageName)
        {
            return !string.IsNullOrWhiteSpace(packageName) &&
                   _initializedPackageNames.Contains(packageName) &&
                   YooAssets.TryGetPackage(packageName) != null;
        }

        private async UniTask<PackageInitializationResult> EnsurePackageReadyAsync(string address, string packageId)
        {
            EnsurePackageRegistryConfigured();
            EnsurePackageInitializationCoordinator();

            string packageName = ResolveYooAssetPackageName(address, packageId);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return PackageInitializationResult.CreateFailure(packageName, "Resolved package name is empty.");
            }

            PackageInitializationResult result = await _packageInitializationCoordinator.EnsureInitializedAsync(packageName);
            if (!result.Succeeded)
            {
                return result;
            }

            if (YooAssets.TryGetPackage(packageName) == null)
            {
                return PackageInitializationResult.CreateFailure(packageName,
                    $"Package '{packageName}' is unavailable after initialization.");
            }

            return result;
        }

        private async UniTask<PackageInitializationResult> InitializePackageCoreAsync(string packageName)
        {
            ResourcePackage package = YooAssets.TryGetPackage(packageName) ?? YooAssets.CreatePackage(packageName);
            InitializationOperation initOperation = null;
            YooAssetPlayMode playMode = ResolvePlayMode(packageName);

            switch (playMode)
            {
                case YooAssetPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(packageName);
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
                    IRemoteServices remoteServices = BuildRemoteService(_settingComponent, _gameSetting);
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

            if (initOperation == null)
            {
                return PackageInitializationResult.CreateFailure(
                    packageName,
                    $"Unknown YooAsset play mode '{playMode}', initialization failed.");
            }

            await initOperation.Task;
            if (initOperation.Status == EOperationStatus.Succeed)
            {
                _initializedPackageNames.Add(packageName);
                return PackageInitializationResult.CreateSuccess(packageName);
            }

            return PackageInitializationResult.CreateFailure(
                packageName,
                initOperation.Error ?? "YooAsset initialization failed.");
        }

        private YooAssetPlayMode ResolvePlayMode(string packageName)
        {
            PackageDefinition definition = GetPackageDefinitionByPackageName(packageName);
            return definition != null ? definition.playModeOverride : ResourceComponent.YooAssetsPlayModel;
        }

        private PackageDefinition GetPackageDefinitionByPackageName(string packageName)
        {
            EnsurePackageRegistryConfigured();
            foreach (KeyValuePair<string, PackageDefinition> pair in _packageRegistry.ActivePackages)
            {
                PackageDefinition definition = pair.Value;
                if (definition == null)
                {
                    continue;
                }

                if (string.Equals(definition.yooPackageName, packageName, StringComparison.Ordinal) ||
                    string.Equals(definition.packageId, packageName, StringComparison.Ordinal))
                {
                    return definition;
                }
            }

            return null;
        }

        private async UniTask PrewarmConfiguredPackagesAsync(string defaultPackageName)
        {
            foreach (KeyValuePair<string, PackageDefinition> pair in _packageRegistry.ActivePackages)
            {
                PackageDefinition definition = pair.Value;
                if (definition == null ||
                    !definition.initOnLaunch ||
                    string.IsNullOrWhiteSpace(definition.yooPackageName) ||
                    string.Equals(definition.yooPackageName, defaultPackageName, StringComparison.Ordinal))
                {
                    continue;
                }

                PackageInitializationResult result = await _packageInitializationCoordinator.EnsureInitializedAsync(definition.yooPackageName);
                if (!result.Succeeded)
                {
                    Debug.LogWarning($"[YooAssetResourceHelper] Failed to prewarm package '{definition.packageId}': {result.ErrorMessage}");
                }
            }
        }

        protected virtual IRemoteServices BuildRemoteService(SettingComponent settingComponent, GameSetting gameSetting)
        {
            return new DefaultRemoteServices(settingComponent, gameSetting);
        }

        private void EnsurePackageRegistryConfigured()
        {
            if (_packageRegistryConfigured)
            {
                return;
            }

            _resourceComponentSetting ??= SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();

            if (_resourceComponentSetting != null)
            {
                _packageResolver ??= new PackageResolver(_resourceComponentSetting.YooAssetRouting);
                _packageRegistry.Configure(
                    _resourceComponentSetting.GetEffectivePackageDefinitions(),
                    Application.platform,
                    _gameSetting != null ? _gameSetting.channel : string.Empty);
                _packageRegistryConfigured = true;
            }
            else
            {
                Debug.LogWarning("[YooAssetResourceHelper] ResourceComponentSetting not found, package registry not configured.");
            }
        }

        private string ResolveYooAssetPackageName(string packageId)
        {
            EnsurePackageRegistryConfigured();

            string logicalPackageId = ResolveLogicalPackageId(null, packageId);
            if (!string.IsNullOrWhiteSpace(logicalPackageId))
            {
                PackageDefinition package = _packageRegistry.GetPackage(logicalPackageId);
                if (package != null && !string.IsNullOrWhiteSpace(package.yooPackageName))
                {
                    return package.yooPackageName;
                }

                return logicalPackageId;
            }

            return ResourceComponent.YooAssetPackageName;
        }

        private string ResolveYooAssetPackageName(string address, string packageId)
        {
            EnsurePackageRegistryConfigured();

            string logicalPackageId = ResolveLogicalPackageId(address, packageId);
            if (!string.IsNullOrWhiteSpace(logicalPackageId))
            {
                PackageDefinition package = _packageRegistry.GetPackage(logicalPackageId);
                if (package != null && !string.IsNullOrWhiteSpace(package.yooPackageName))
                {
                    return package.yooPackageName;
                }

                return logicalPackageId;
            }

            return ResourceComponent.YooAssetPackageName;
        }

        private string ResolveLogicalPackageId(string address, string explicitPackageId)
        {
            EnsurePackageRegistryConfigured();

            if (_packageResolver != null && _resourceComponentSetting != null)
            {
                return _packageResolver.ResolvePackageId(
                    address,
                    explicitPackageId,
                    _resourceComponentSetting.GetResolvedDefaultPackageId());
            }

            if (_resourceComponentSetting != null)
            {
                return string.IsNullOrWhiteSpace(explicitPackageId)
                    ? _resourceComponentSetting.GetResolvedDefaultPackageId()
                    : explicitPackageId;
            }

            return explicitPackageId;
        }

        private ResourcePackage GetLoadedPackage(string packageId)
        {
            string packageName = ResolveYooAssetPackageName(packageId);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return null;
            }

            return IsPackageInitialized(packageName) ? YooAssets.TryGetPackage(packageName) : null;
        }

        private ResourcePackage GetLoadedPackage(string address, string packageId)
        {
            string packageName = ResolveYooAssetPackageName(address, packageId);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return null;
            }

            return IsPackageInitialized(packageName) ? YooAssets.TryGetPackage(packageName) : null;
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

        private async UniTask TryLoadBootstrapRouteIndexAsync()
        {
            if (_resourceComponentSetting == null || _packageResolver == null)
            {
                return;
            }

            RoutingSettings routing = _resourceComponentSetting.YooAssetRouting;
            if (!routing.enableRouteIndex)
            {
                return;
            }

            var loader = new RouteIndexBootstrapLoader(_packageRegistry, routing);
            if (!loader.TryGetBootstrapRequest(out string packageId, out string address, out string errorMessage))
            {
                Debug.LogWarning($"[YooAssetResourceHelper] Skip route index bootstrap: {errorMessage}");
                return;
            }

            PackageInitializationResult ready = await EnsurePackageReadyAsync(null, packageId);
            if (!ready.Succeeded)
            {
                Debug.LogWarning($"[YooAssetResourceHelper] Failed to initialize bootstrap package '{packageId}': {ready.ErrorMessage}");
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                Debug.LogWarning($"[YooAssetResourceHelper] Bootstrap package '{packageId}' is unavailable after initialization.");
                return;
            }

            AssetHandle handle = package.LoadAssetAsync<RouteIndexAsset>(address);
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status == EOperationStatus.Succeed && handle.AssetObject is RouteIndexAsset routeIndex)
            {
                _packageResolver.LoadRouteIndex(routeIndex);
                Debug.Log($"[YooAssetResourceHelper] Loaded route index from '{packageId}:{address}'.");
            }
            else
            {
                Debug.LogWarning($"[YooAssetResourceHelper] Failed to load route index from '{packageId}:{address}'. Error: {handle.LastError}");
            }

            handle.Release();
        }

        #endregion
    }
}
#endif
