using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework.Resource;
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
        private readonly Dictionary<string, SceneHandle> _sceneHandles = new Dictionary<string, SceneHandle>();

        /// <summary>
        /// 二进制资源名称 → RawFileHandle 映射
        /// </summary>
        private readonly Dictionary<string, RawFileHandle> _rawFileHandles = new Dictionary<string, RawFileHandle>();
    

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

            // 获取资源的 InstanceID
            int instanceId = -1;
            if (asset is UnityEngine.Object unityObj)
            {
                instanceId = unityObj.GetInstanceID();
            }
            else
            {
                // 非 Unity 对象，无法释放
                return;
            }

            // 检查是否是实例化对象
            if (_instanceToAssetMap.TryGetValue(instanceId, out int assetInstanceId))
            {
                // 这是一个实例化对象，释放实例化对象的映射
                _instanceToAssetMap.Remove(instanceId);
                instanceId = assetInstanceId; // 使用原始资源的 InstanceID
            }

            // 查找并释放 Handle
            if (_handleRefCounts.TryGetValue(instanceId, out int refCount))
            {
                refCount--;
                _handleRefCounts[instanceId] = refCount;

                if (refCount <= 0)
                {
                    // 引用计数归零，释放所有 Handle
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

            // 查找并释放 RawFileHandle
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
                // 释放场景 Handle
                if (_sceneHandles.TryGetValue(sceneAssetName, out SceneHandle handle))
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
                // 保存 Handle 映射关系
                var asset = handle.AssetObject;
                if (asset != null)
                {
                    int instanceId = asset.GetInstanceID();

                    // 将 Handle 添加到列表中（支持同一资源多次加载）
                    if (_assetHandles.TryGetValue(instanceId, out var handles))
                    {
                        handles.Add(handle);
                        _handleRefCounts[instanceId]++;
                    }
                    else
                    {
                        // 新资源，创建 Handle 列表并初始化引用计数
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

        private IEnumerator WaitForSceneLoad(SceneHandle handle, string sceneAssetName,
            LoadSceneCallbacks callbacks, object userData)
        {
            while (!handle.IsDone)
            {
                callbacks.LoadSceneUpdateCallback?.Invoke(sceneAssetName, handle.Progress, userData);
                yield return null;
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                // 保存场景 Handle 映射
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
                    // 保存 RawFileHandle 映射
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

                // 等待实例化完成
                while (!instantiateOp.IsDone)
                {
                    yield return null;
                }

                // 保存实例化对象到原始资源的映射
                if (instantiateOp.Status == EOperationStatus.Succeed &&
                    instantiateOp.Result != null &&
                    handle.AssetObject != null)
                {
                    int instanceId = instantiateOp.Result.GetInstanceID();
                    int assetInstanceId = handle.AssetObject.GetInstanceID();

                    // 保存实例化对象的映射
                    _instanceToAssetMap[instanceId] = assetInstanceId;

                    // 增加原始资源的引用计数并保存 Handle
                    if (_assetHandles.TryGetValue(assetInstanceId, out var handles))
                    {
                        handles.Add(handle);
                        _handleRefCounts[assetInstanceId]++;
                    }
                    else
                    {
                        // 如果原始资源还没有被加载，创建 Handle 列表
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

        private async void InitializePackageAsync(ResourcePackage package, ResourceInitCallBack callback)
        {
            InitializationOperation initOperation = null;

            switch (ResourceComponent.YooAssetsPlayModel)
            {
                case YooAssetPlayMode.EditorSimulateMode:
                {
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(ResourceComponent.YooAssetPackageName);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var createParameters = new EditorSimulateModeParameters();
                    createParameters.EditorFileSystemParameters =
                        FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.OfflinePlayMode:
                {
                    var createParameters = new OfflinePlayModeParameters();
                    createParameters.BuildinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.HostPlayMode:
                {
                    string defaultHostServer = "";//GetHostServerURL();
                    string fallbackHostServer = "";// GetHostServerURL();
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    var createParameters = new HostPlayModeParameters();
                    createParameters.BuildinFileSystemParameters =
                        FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    createParameters.CacheFileSystemParameters =
                        FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    initOperation = package.InitializeAsync(createParameters);
                }
                    break;
                case YooAssetPlayMode.WebPlayMode:
                {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                    var createParameters = new WebPlayModeParameters();
			        string defaultHostServer = GetHostServerURL();
                    string fallbackHostServer = GetHostServerURL();
                    string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE"; //注意：如果有子目录，请修改此处！
                    IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                    createParameters.WebServerFileSystemParameters =
                    WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices);
                    initOperation = package.InitializeAsync(createParameters);
#else
                    var createParameters = new WebPlayModeParameters();
                    createParameters.WebServerFileSystemParameters =
                        FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
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

        /// <summary>
        /// 加载资源（V2 版本，返回 IResourceHandle）
        /// </summary>
        public override void LoadAssetV2(string assetName, Type assetType,
            LoadAssetCallbacksV2 callbacks, object userData)
        {
            var package = YooAssets.GetPackage(ResourceComponent.YooAssetPackageName);
            var handle = package.LoadAssetAsync(assetName, assetType);
            StartCoroutine(WaitForAssetLoadV2(handle, assetName, callbacks, userData));
        }

        private IEnumerator WaitForAssetLoadV2(AssetHandle handle, string assetName,
            LoadAssetCallbacksV2 callbacks, object userData)
        {
            while (!handle.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, handle.Progress, userData);
                yield return null;
            }

            if (handle.Status == EOperationStatus.Succeed)
            {
                // 创建 IResourceHandle 包装
                var resourceHandle = new YooAssetResourceHandle(handle, assetName);

                callbacks.LoadAssetSuccessCallback?.Invoke(
                    assetName, resourceHandle, 0f, userData);
            }
            else
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName, LoadResourceStatus.NotExist,
                    handle.LastError ?? "Load failed.", userData);
            }
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