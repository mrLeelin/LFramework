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
    /// YooAsset 资源辅助器。
    /// 负责 YooAsset 包初始化、资源加载、资源实例化、句柄跟踪以及多包路由解析。
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

        /// <summary>
        /// 游戏全局配置，用于获取渠道等运行时信息。
        /// </summary>
        private GameSetting _gameSetting;

        /// <summary>
        /// 设置组件，用于构造远端资源服务。
        /// </summary>
        private SettingComponent _settingComponent;

        /// <summary>
        /// 资源组件配置，包含包定义、默认包和路由规则。
        /// </summary>
        private ResourceComponentSetting _resourceComponentSetting;

        /// <summary>
        /// 当前生效的资源包注册表。
        /// </summary>
        private readonly PackageRegistry _packageRegistry = new PackageRegistry();

        /// <summary>
        /// 资源地址到逻辑包 ID 的解析器。
        /// </summary>
        private PackageResolver _packageResolver;

        /// <summary>
        /// 标记包注册表是否已经完成配置，避免重复初始化。
        /// </summary>
        private bool _packageRegistryConfigured;

        /// <summary>
        /// 已完成初始化的 YooAsset 包名称集合。
        /// </summary>
        private readonly HashSet<string> _initializedPackageNames = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// 包初始化协调器，用于合并并发初始化请求。
        /// </summary>
        private PackageInitializationCoordinator _packageInitializationCoordinator;
        private readonly Dictionary<string, PackageRuntimeState> _packageRuntimeStates =
            new Dictionary<string, PackageRuntimeState>(StringComparer.Ordinal);


        /// <summary>
        /// 缓存运行时依赖的配置对象。
        /// </summary>
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
            var defaultPackageName = ResolveYooAssetPackageName(null);
            if (string.IsNullOrWhiteSpace(defaultPackageName))
            {
                callback?.ResourceInitFailureCallBack?.Invoke("Resolved default YooAsset package name is empty.");
                return;
            }

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
            var package = GetLoadedPackage(assetName, null);
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

            foreach (string cacheKey in CollectMatchingScopedCacheKeys(_rawFileHandles, binaryAssetName))
            {
                RawFileHandle handle = _rawFileHandles[cacheKey];
                handle.Release();
                _rawFileHandles.Remove(cacheKey);
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
                foreach (string cacheKey in CollectMatchingScopedCacheKeys(_sceneHandles, sceneAssetName))
                {
                    YooAsset.SceneHandle handle = _sceneHandles[cacheKey];
                    handle.UnloadAsync();
                    _sceneHandles.Remove(cacheKey);
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

        /// <summary>
        /// 按指定资源包加载资源。
        /// </summary>
        public override void LoadAsset(string assetName, Type assetType, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            LoadAssetInternalAsync(assetName, assetType, packageId, callbacks, userData).Forget();
        }

        /// <summary>
        /// 轮询资源加载句柄并分发进度、成功和失败回调。
        /// </summary>
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

        /// <summary>
        /// 按指定资源包加载场景。
        /// </summary>
        public override void LoadScene(string sceneAssetName, string packageId,
            LoadSceneCallbacks callbacks, object userData)
        {
            LoadSceneInternalAsync(sceneAssetName, packageId, callbacks, userData).Forget();
        }

        /// <summary>
        /// 轮询场景加载句柄并分发进度、成功和失败回调。
        /// </summary>
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

        /// <summary>
        /// 按指定资源包加载原始二进制文件。
        /// </summary>
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

        /// <summary>
        /// 按指定资源包加载并实例化资源。
        /// </summary>
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
                    ready.ErrorMessage ?? BuildPackageFailureMessage(assetName, packageId,
                        $"Package '{ResolveYooAssetPackageName(assetName, packageId)}' is not initialized."),
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    BuildPackageFailureMessage(assetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."),
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

                    // 记录资源句柄和引用计数，供 Release 时统一回收。
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

        /// <summary>
        /// 异步加载场景并通过回调返回结果。
        /// </summary>
        private async UniTask LoadSceneInternalAsync(string sceneAssetName, string packageId,
            LoadSceneCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(sceneAssetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadSceneFailureCallback?.Invoke(
                    sceneAssetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? BuildPackageFailureMessage(sceneAssetName, packageId,
                        $"Package '{ResolveYooAssetPackageName(sceneAssetName, packageId)}' is not initialized."),
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadSceneFailureCallback?.Invoke(
                    sceneAssetName,
                    LoadResourceStatus.NotReady,
                    BuildPackageFailureMessage(sceneAssetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."),
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
                _sceneHandles[ComposePackageScopedCacheKey(ready.PackageName, sceneAssetName)] = handle;
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

        /// <summary>
        /// 异步加载二进制资源并缓存 RawFileHandle。
        /// </summary>
        private async UniTask LoadBinaryInternalAsync(string binaryAssetName, string packageId,
            LoadBinaryCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(binaryAssetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadBinaryFailureCallback?.Invoke(
                    binaryAssetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? BuildPackageFailureMessage(binaryAssetName, packageId,
                        $"Package '{ResolveYooAssetPackageName(binaryAssetName, packageId)}' is not initialized."),
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadBinaryFailureCallback?.Invoke(
                    binaryAssetName,
                    LoadResourceStatus.NotReady,
                    BuildPackageFailureMessage(binaryAssetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."),
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
                _rawFileHandles[ComposePackageScopedCacheKey(ready.PackageName, binaryAssetName)] = handle;
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

        /// <summary>
        /// 异步加载资源并实例化对象，同时维护实例对象到原始资源的映射关系。
        /// </summary>
        private async UniTask InstantiateAssetInternalAsync(string assetName, string packageId,
            LoadAssetCallbacks callbacks, object userData)
        {
            PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, packageId);
            if (!ready.Succeeded)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    ready.ErrorMessage ?? BuildPackageFailureMessage(assetName, packageId,
                        $"Package '{ResolveYooAssetPackageName(assetName, packageId)}' is not initialized."),
                    userData);
                return;
            }

            ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
            if (package == null)
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName,
                    LoadResourceStatus.NotReady,
                    BuildPackageFailureMessage(assetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."),
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
                // 记录实例对象与源资源的关联，确保释放实例时可以正确减少源资源引用计数。
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

        /// <summary>
        /// 创建通用资源句柄，并在异步完成后返回指定类型的资源对象。
        /// </summary>
        public override ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName)
        {
            return LoadAssetHandle<T>(assetName, null);
        }

        public override ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName, string packageId)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceAssetHandle<T>>();
            resourceHandle.MarkFromPool();
            LoadAssetHandleAsync(resourceHandle, assetName, packageId);
            return resourceHandle;
        }

        /// <summary>
        /// 执行基于 ResourceAssetHandle 的资源加载流程。
        /// </summary>
        private async void LoadAssetHandleAsync<T>(ResourceAssetHandle<T> handle, string assetName, string packageId)
            where T : UnityEngine.Object
        {
            try
            {
                PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, packageId);
                if (!ready.Succeeded)
                {
                    handle.SetError(string.IsNullOrWhiteSpace(ready.ErrorMessage)
                        ? BuildPackageFailureMessage(assetName, packageId,
                            $"Load asset '{assetName}' failed because its package is not ready.")
                        : ready.ErrorMessage);
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError(BuildPackageFailureMessage(assetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."));
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
            return InstantiateAssetHandle(assetName, null);
        }

        public override ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName, string packageId)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceAssetHandle<GameObject>>();
            resourceHandle.MarkFromPool();
            InstantiateAssetHandleAsync(resourceHandle, assetName, packageId);
            return resourceHandle;
        }

        /// <summary>
        /// 执行基于 ResourceAssetHandle 的实例化流程。
        /// </summary>
        private async void InstantiateAssetHandleAsync(ResourceAssetHandle<GameObject> handle, string assetName, string packageId)
        {
            try
            {
                PackageInitializationResult ready = await EnsurePackageReadyAsync(assetName, packageId);
                if (!ready.Succeeded)
                {
                    handle.SetError(string.IsNullOrWhiteSpace(ready.ErrorMessage)
                        ? BuildPackageFailureMessage(assetName, packageId,
                            $"Instantiate asset '{assetName}' failed because its package is not ready.")
                        : ready.ErrorMessage);
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError(BuildPackageFailureMessage(assetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."));
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
            return LoadSceneHandle(sceneAssetName, null);
        }

        public override ResourceSceneHandle LoadSceneHandle(string sceneAssetName, string packageId)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceSceneHandle>();
            resourceHandle.MarkFromPool();
            resourceHandle.SetSceneName(sceneAssetName);
            LoadSceneHandleAsync(resourceHandle, sceneAssetName, packageId);
            return resourceHandle;
        }

        /// <summary>
        /// 执行基于 ResourceSceneHandle 的场景加载流程。
        /// </summary>
        private async void LoadSceneHandleAsync(ResourceSceneHandle handle, string sceneAssetName, string packageId)
        {
            try
            {
                PackageInitializationResult ready = await EnsurePackageReadyAsync(sceneAssetName, packageId);
                if (!ready.Succeeded)
                {
                    handle.SetError(string.IsNullOrWhiteSpace(ready.ErrorMessage)
                        ? BuildPackageFailureMessage(sceneAssetName, packageId,
                            $"Load scene '{sceneAssetName}' failed because its package is not ready.")
                        : ready.ErrorMessage);
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError(BuildPackageFailureMessage(sceneAssetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."));
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
            return LoadRawFileHandle(binaryAssetName, null);
        }

        public override ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName, string packageId)
        {
            var resourceHandle = ReferencePool.Acquire<ResourceRawFileHandle>();
            resourceHandle.MarkFromPool();
            LoadRawFileHandleAsync(resourceHandle, binaryAssetName, packageId);
            return resourceHandle;
        }

        /// <summary>
        /// 执行基于 ResourceRawFileHandle 的原始文件加载流程。
        /// </summary>
        private async void LoadRawFileHandleAsync(ResourceRawFileHandle handle, string binaryAssetName, string packageId)
        {
            try
            {
                PackageInitializationResult ready = await EnsurePackageReadyAsync(binaryAssetName, packageId);
                if (!ready.Succeeded)
                {
                    handle.SetError(string.IsNullOrWhiteSpace(ready.ErrorMessage)
                        ? BuildPackageFailureMessage(binaryAssetName, packageId,
                            $"Load binary '{binaryAssetName}' failed because its package is not ready.")
                        : ready.ErrorMessage);
                    return;
                }

                ResourcePackage package = YooAssets.TryGetPackage(ready.PackageName);
                if (package == null)
                {
                    handle.SetError(BuildPackageFailureMessage(binaryAssetName, packageId,
                        $"Package '{ready.PackageName}' is unavailable after initialization."));
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

        /// <summary>
        /// 执行按标签批量加载资源的流程，并将结果转换为指定泛型集合。
        /// </summary>
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

        /// <summary>
        /// 初始化指定资源包，并在完成后预热配置的附加包。
        /// </summary>
        protected virtual async void InitializePackageAsync(string packageName, ResourceInitCallBack callback)
        {
            MarkPackageInitializationStarted(packageName, null);
            PackageInitializationResult result = await _packageInitializationCoordinator.EnsureInitializedAsync(packageName);
            ApplyPackageInitializationResult(packageName, null, result);
            if (!result.Succeeded)
            {
                callback?.ResourceInitFailureCallBack?.Invoke(result.ErrorMessage ?? "YooAsset initialization failed.");
                return;
            }

            await PrewarmConfiguredPackagesAsync(packageName);
            callback?.ResourceInitSuccessCallBack?.Invoke();
        }

        /// <summary>
        /// 确保包初始化协调器已创建。
        /// </summary>
        private void EnsurePackageInitializationCoordinator()
        {
            _packageInitializationCoordinator ??= new PackageInitializationCoordinator(
                IsPackageInitialized,
                InitializePackageCoreAsync);
        }

        /// <summary>
        /// 判断指定包是否已经初始化完成且可用。
        /// </summary>
        private bool IsPackageInitialized(string packageName)
        {
            return !string.IsNullOrWhiteSpace(packageName) &&
                   _initializedPackageNames.Contains(packageName) &&
                   YooAssets.TryGetPackage(packageName) != null;
        }

        /// <summary>
        /// 确保资源地址对应的包已经初始化完成。
        /// </summary>
        private async UniTask<PackageInitializationResult> EnsurePackageReadyAsync(string address, string packageId)
        {
            string logicalPackageId = ResolveLogicalPackageId(address, packageId);
            string packageName = ResolveYooAssetPackageName(address, packageId);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return PackageInitializationResult.CreateFailure(packageName, "Resolved package name is empty.");
            }

            if (IsPackageInitialized(packageName))
            {
                PackageInitializationResult success = PackageInitializationResult.CreateSuccess(packageName);
                ApplyPackageInitializationResult(packageName, logicalPackageId, success);
                return success;
            }

            MarkPackageInitializationStarted(packageName, logicalPackageId);
            PackageInitializationResult result = await _packageInitializationCoordinator.EnsureInitializedAsync(packageName);
            ApplyPackageInitializationResult(packageName, logicalPackageId, result);
            if (!result.Succeeded)
            {
                return result;
            }

            if (YooAssets.TryGetPackage(packageName) == null)
            {
                PackageInitializationResult failure = PackageInitializationResult.CreateFailure(packageName,
                    $"Package '{packageName}' is unavailable after initialization.");
                ApplyPackageInitializationResult(packageName, logicalPackageId, failure);
                return failure;
            }

            return result;
        }

        /// <summary>
        /// 按运行模式创建并初始化 YooAsset 资源包。
        /// </summary>
        private async UniTask<PackageInitializationResult> InitializePackageCoreAsync(string packageName)
        {
            ResourcePackage package = YooAssets.TryGetPackage(packageName) ?? YooAssets.CreatePackage(packageName);
            InitializationOperation initOperation = null;
            YooAssetPlayMode playMode = ResolvePlayMode(packageName);

            // 根据当前包的运行模式组装不同的文件系统参数。
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

        /// <summary>
        /// 解析指定包应使用的 YooAsset 运行模式。
        /// </summary>
        private YooAssetPlayMode ResolvePlayMode(string packageName)
        {
            PackageDefinition definition = GetPackageDefinitionByPackageName(packageName);
            if (definition != null)
            {
                return definition.playModeOverride;
            }

            string defaultPackageName = YooAssetMultiPackageUtility.ResolveDefaultPackageName(
                _resourceComponentSetting,
                Application.platform,
                GetCurrentChannel());
            PackageDefinition defaultPackage = GetPackageDefinitionByPackageName(defaultPackageName);
            return defaultPackage != null ? defaultPackage.playModeOverride : YooAssetPlayMode.EditorSimulateMode;
        }

        /// <summary>
        /// 通过包名或逻辑包 ID 查找包定义配置。
        /// </summary>
        private PackageDefinition GetPackageDefinitionByPackageName(string packageName)
        {
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

        /// <summary>
        /// 预热配置为启动时初始化的附加资源包。
        /// </summary>
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

                MarkPackageInitializationStarted(definition.yooPackageName, definition.packageId);
                PackageInitializationResult result = await _packageInitializationCoordinator.EnsureInitializedAsync(definition.yooPackageName);
                ApplyPackageInitializationResult(definition.yooPackageName, definition.packageId, result);
                if (!result.Succeeded)
                {
                    Debug.LogWarning($"[YooAssetResourceHelper] Failed to prewarm package '{definition.packageId}': {result.ErrorMessage}");
                }
            }
        }

        /// <summary>
        /// 构造远端资源服务，子类可覆盖以替换下载地址策略。
        /// </summary>
        protected virtual IRemoteServices BuildRemoteService(SettingComponent settingComponent, GameSetting gameSetting)
        {
            return new DefaultRemoteServices(settingComponent, gameSetting);
        }

        /// <summary>
        /// 根据资源配置初始化包注册表和路由解析器。
        /// </summary>
        private void EnsurePackageRegistryConfigured()
        {
            if (_packageRegistryConfigured)
            {
                return;
            }

            _resourceComponentSetting ??= SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();

            if (_resourceComponentSetting != null)
            {
                _packageResolver ??= new PackageResolver(_resourceComponentSetting.YooAssetRouting, _packageRegistry);
                _packageRegistry.Configure(
                    _resourceComponentSetting.GetEffectivePackageDefinitions(),
                    Application.platform,
                    _gameSetting != null ? _gameSetting.channel : string.Empty);
                _packageRegistryConfigured = true;
            }
            else
            {
                Debug.LogError("[YooAssetResourceHelper] ResourceComponentSetting not found, package registry not configured.");
            }
        }

        /// <summary>
        /// 根据逻辑包 ID 解析实际的 YooAsset 包名称。
        /// </summary>
        private string ResolveYooAssetPackageName(string packageId)
        {
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

            return YooAssetMultiPackageUtility.ResolveDefaultPackageName(
                _resourceComponentSetting,
                Application.platform,
                GetCurrentChannel());
        }

        /// <summary>
        /// 根据资源地址和逻辑包 ID 解析实际的 YooAsset 包名称。
        /// </summary>
        private string ResolveYooAssetPackageName(string address, string packageId)
        {
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

            return YooAssetMultiPackageUtility.ResolveDefaultPackageName(
                _resourceComponentSetting,
                Application.platform,
                GetCurrentChannel());
        }

        public override UniTask RefreshRouteIndexAsync()
        {
            return TryLoadBootstrapRouteIndexAsync();
        }

        /// <summary>
        /// 判断是否需要等待 manifest 就绪后再加载路由索引。
        /// </summary>
        protected virtual bool ShouldDeferRouteIndexLoadUntilManifestReady(bool packageValid)
        {
            return !packageValid;
        }

        private string GetCurrentChannel()
        {
            return _gameSetting != null ? _gameSetting.channel : string.Empty;
        }

        protected virtual PackageRouteResolutionResult ResolvePackageRouteDiagnostics(string address, string explicitPackageId)
        {
            if (_packageResolver != null && _resourceComponentSetting != null)
            {
                return _packageResolver.ResolveWithDiagnostics(
                    address,
                    explicitPackageId,
                    _resourceComponentSetting.GetResolvedDefaultPackageId());
            }

            return new PackageRouteResolutionResult
            {
                RequestedAddress = address,
                ExplicitPackageId = explicitPackageId,
                DefaultPackageId = _resourceComponentSetting != null ? _resourceComponentSetting.GetResolvedDefaultPackageId() : null,
                FinalPackageId = _resourceComponentSetting != null && string.IsNullOrWhiteSpace(explicitPackageId)
                    ? _resourceComponentSetting.GetResolvedDefaultPackageId()
                    : explicitPackageId,
                UsedExplicitPackageId = !string.IsNullOrWhiteSpace(explicitPackageId),
                UsedDefaultPackage = string.IsNullOrWhiteSpace(explicitPackageId)
            };
        }

        private string BuildPackageFailureMessage(string address, string explicitPackageId, string baseMessage)
        {
            PackageRouteResolutionResult diagnostics = ResolvePackageRouteDiagnostics(address, explicitPackageId);
            string routeSummary = FormatPackageRouteSummary(diagnostics);
            return string.IsNullOrWhiteSpace(routeSummary) ? baseMessage : $"{baseMessage} Route: {routeSummary}";
        }

        private static string FormatPackageRouteSummary(PackageRouteResolutionResult diagnostics)
        {
            if (diagnostics == null)
            {
                return null;
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(diagnostics.RequestedAddress))
            {
                parts.Add($"address={diagnostics.RequestedAddress}");
            }

            if (!string.IsNullOrWhiteSpace(diagnostics.ExplicitPackageId))
            {
                parts.Add($"explicit={diagnostics.ExplicitPackageId}");
            }

            if (!string.IsNullOrWhiteSpace(diagnostics.RouteIndexPackageId))
            {
                parts.Add($"routeIndex={diagnostics.RouteIndexPackageId}");
            }

            if (!string.IsNullOrWhiteSpace(diagnostics.DefaultPackageId))
            {
                parts.Add($"default={diagnostics.DefaultPackageId}");
            }

            if (diagnostics.FallbackChain.Count > 0)
            {
                parts.Add($"fallback={string.Join("->", diagnostics.FallbackChain)}");
            }

            if (!string.IsNullOrWhiteSpace(diagnostics.FinalPackageId))
            {
                parts.Add($"final={diagnostics.FinalPackageId}");
            }

            return parts.Count == 0 ? null : string.Join(", ", parts);
        }

        /// <summary>
        /// 解析逻辑包 ID，优先使用显式包 ID，其次使用路由规则和默认包配置。
        /// </summary>
        private string ResolveLogicalPackageId(string address, string explicitPackageId)
        {
            if (_packageResolver != null && _resourceComponentSetting != null)
            {
                return ResolvePackageRouteDiagnostics(address, explicitPackageId).FinalPackageId;
            }

            if (_resourceComponentSetting != null)
            {
                return string.IsNullOrWhiteSpace(explicitPackageId)
                    ? _resourceComponentSetting.GetResolvedDefaultPackageId()
                    : explicitPackageId;
            }

            return explicitPackageId;
        }

        /// <summary>
        /// 获取已加载的资源包，不会触发初始化流程。
        /// </summary>
        private ResourcePackage GetLoadedPackage(string packageId)
        {
            string packageName = ResolveYooAssetPackageName(packageId);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return null;
            }

            return IsPackageInitialized(packageName) ? YooAssets.TryGetPackage(packageName) : null;
        }

        /// <summary>
        /// 根据资源地址和逻辑包 ID 获取已加载的资源包，不会触发初始化流程。
        /// </summary>
        protected virtual ResourcePackage GetLoadedPackage(string address, string packageId)
        {
            string packageName = ResolveYooAssetPackageName(address, packageId);
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return null;
            }

            return IsPackageInitialized(packageName) ? YooAssets.TryGetPackage(packageName) : null;
        }

        protected virtual string ComposePackageScopedCacheKey(string packageName, string address)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return address ?? string.Empty;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                return packageName;
            }

            return $"{packageName}::{address}";
        }

        private static List<string> CollectMatchingScopedCacheKeys<THandle>(Dictionary<string, THandle> cache, string address)
        {
            var matchingKeys = new List<string>();
            if (cache == null || string.IsNullOrWhiteSpace(address))
            {
                return matchingKeys;
            }

            foreach (string key in cache.Keys)
            {
                if (IsMatchingScopedCacheKey(key, address))
                {
                    matchingKeys.Add(key);
                }
            }

            return matchingKeys;
        }

        private static bool IsMatchingScopedCacheKey(string scopedKey, string address)
        {
            if (string.IsNullOrWhiteSpace(scopedKey) || string.IsNullOrWhiteSpace(address))
            {
                return false;
            }

            return string.Equals(scopedKey, address, StringComparison.Ordinal) ||
                   scopedKey.EndsWith($"::{address}", StringComparison.Ordinal);
        }

        public bool TryGetPackageRuntimeState(string packageName, out PackageRuntimeState state)
        {
            if (_packageRuntimeStates.TryGetValue(packageName, out PackageRuntimeState existingState))
            {
                state = existingState.Clone();
                return true;
            }

            state = null;
            return false;
        }

        protected void MarkPackageInitializationStarted(string packageName, string logicalPackageId)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return;
            }

            PackageRuntimeState state = GetOrCreatePackageRuntimeState(packageName);
            state.PackageName = packageName;
            if (!string.IsNullOrWhiteSpace(logicalPackageId))
            {
                state.LogicalPackageId = logicalPackageId;
            }

            state.IsInitializing = true;
            state.LastError = null;
        }

        protected void ApplyPackageInitializationResult(string packageName, string logicalPackageId, PackageInitializationResult result)
        {
            string resolvedPackageName = !string.IsNullOrWhiteSpace(packageName) ? packageName : result.PackageName;
            if (string.IsNullOrWhiteSpace(resolvedPackageName))
            {
                return;
            }

            PackageRuntimeState state = GetOrCreatePackageRuntimeState(resolvedPackageName);
            state.PackageName = resolvedPackageName;
            if (!string.IsNullOrWhiteSpace(logicalPackageId))
            {
                state.LogicalPackageId = logicalPackageId;
            }

            state.IsInitializing = false;
            state.IsInitialized = result.Succeeded;
            state.LastError = result.Succeeded ? null : result.ErrorMessage;
        }

        protected void MarkPackageRouteIndexRefreshed(string packageName)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                return;
            }

            PackageRuntimeState state = GetOrCreatePackageRuntimeState(packageName);
            state.PackageName = packageName;
            state.LastRouteIndexRefreshUtc = DateTime.UtcNow;
        }

        private PackageRuntimeState GetOrCreatePackageRuntimeState(string packageName)
        {
            if (!_packageRuntimeStates.TryGetValue(packageName, out PackageRuntimeState state))
            {
                state = new PackageRuntimeState
                {
                    PackageName = packageName
                };
                _packageRuntimeStates.Add(packageName, state);
            }

            return state;
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

        /// <summary>
        /// 启动时尝试加载路由索引资源，为地址到包的动态路由提供初始数据。
        /// </summary>
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

            var loader = new RouteIndexBootstrapLoader(_packageRegistry, routing, _resourceComponentSetting.GetResolvedDefaultPackageId());
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

            if (ShouldDeferRouteIndexLoadUntilManifestReady(package.PackageValid))
            {
                Debug.Log($"[YooAssetResourceHelper] Defer route index load for '{packageId}:{address}' until manifest update completes.");
                return;
            }

            if (!package.PackageValid)
            {
                Debug.LogWarning($"[YooAssetResourceHelper] Skip route index load for '{packageId}:{address}' because the package manifest is not ready.");
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
                MarkPackageRouteIndexRefreshed(ready.PackageName);
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
