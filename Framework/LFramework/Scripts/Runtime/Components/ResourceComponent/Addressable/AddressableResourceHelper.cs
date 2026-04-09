#if ADDRESSABLE_SUPPORT
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameFramework;
using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    /// <summary>
    /// Addressable 资源辅助器（平台初始化 + 查询）
    /// </summary>
    public class AddressableResourceHelper : ResourceHelperBase
    {
        private SettingComponent _settingComponent;
        private const string ReplaceRemote = "remote_";
        private const string ReplaceVersion = "_resource_version_";
        private GameSetting _gameSetting;

        private void Awake()
        {
            _gameSetting = SettingManager.GetSetting<GameSetting>();
            _settingComponent = LFrameworkAspect.Instance.Get<SettingComponent>();
            Addressables.InternalIdTransformFunc = OnInternalIdTransformFunc;
        }

        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public override void InitializeResources(ResourceInitCallBack callback)
        {
            var handle = Addressables.InitializeAsync();
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callback.ResourceInitSuccessCallBack?.Invoke();
                }
                else
                {
                    callback.ResourceInitFailureCallBack?.Invoke(
                        op.OperationException?.Message ?? "Addressable initialization failed.");
                }
            };
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public override HasAssetResult HasAsset(string assetName)
        {
            return HasAssetResult.Exist;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Release(object asset)
        {
            if (asset is AsyncOperationHandle handle)
            {
                Addressables.Release(handle);
            }
            else if (asset is GameObject go)
            {
                Addressables.ReleaseInstance(go);
            }
            else
            {
                Addressables.Release(asset);
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

            op.completed += (_) => { callbacks.UnloadSceneSuccessCallback?.Invoke(sceneAssetName, userData); };
        }

        /// <summary>
        /// 加载资源
        /// </summary>
        public override void LoadAsset(string assetName, System.Type assetType,
            LoadAssetCallbacks callbacks, object userData)
        {
            var handle = Addressables.LoadAssetAsync<object>(assetName);
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, op.Result, 0f, userData);
                }
                else
                {
                    callbacks.LoadAssetFailureCallback?.Invoke(
                        assetName, LoadResourceStatus.NotExist,
                        op.OperationException?.Message ?? "Load failed.", userData);
                }
            };
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public override void LoadScene(string sceneAssetName,
            LoadSceneCallbacks callbacks, object userData)
        {
            var handle = Addressables.LoadSceneAsync(sceneAssetName,LoadSceneMode.Additive);
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadSceneSuccessCallback?.Invoke(
                        sceneAssetName, 0f, userData);
                }
                else
                {
                    callbacks.LoadSceneFailureCallback?.Invoke(
                        sceneAssetName, LoadResourceStatus.NotExist,
                        op.OperationException?.Message ?? "Load scene failed.", userData);
                }
            };
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public override void LoadBinary(string binaryAssetName,
            LoadBinaryCallbacks callbacks, object userData)
        {
            var handle = Addressables.LoadAssetAsync<TextAsset>(binaryAssetName);
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadBinarySuccessCallback?.Invoke(
                        binaryAssetName, op.Result.bytes, 0f, userData);
                }
                else
                {
                    callbacks.LoadBinaryFailureCallback?.Invoke(
                        binaryAssetName, LoadResourceStatus.NotExist,
                        op.OperationException?.Message ?? "Load binary failed.", userData);
                }
            };
        }

        /// <summary>
        /// 实例化资源
        /// </summary>
        public override async void InstantiateAsset(string assetName,
            LoadAssetCallbacks callbacks, object userData)
        {
            var handle = Addressables.InstantiateAsync(assetName);

            while (!handle.IsDone)
            {
                callbacks.LoadAssetUpdateCallback?.Invoke(assetName, handle.PercentComplete, userData);
                await UniTask.Yield();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                callbacks.LoadAssetSuccessCallback?.Invoke(
                    assetName, handle.Result, 0f, userData);
            }
            else
            {
                callbacks.LoadAssetFailureCallback?.Invoke(
                    assetName, LoadResourceStatus.NotExist,
                    handle.OperationException?.Message ?? "Instantiate failed.", userData);
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
                var op = Addressables.LoadAssetAsync<T>(assetName);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.PercentComplete);
                    await UniTask.Yield();
                }
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    handle.RegisterReleaseAction(() => Addressables.Release(op));
                    handle.SetResult(op.Result);
                }
                else
                {
                    handle.SetError(op.OperationException?.Message ?? $"Load asset '{assetName}' failed.");
                    Addressables.Release(op);
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
                var op = Addressables.InstantiateAsync(assetName);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.PercentComplete);
                    await UniTask.Yield();
                }
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    handle.RegisterReleaseAction(() => Addressables.ReleaseInstance(op.Result));
                    handle.SetResult(op.Result);
                }
                else
                {
                    handle.SetError(op.OperationException?.Message ?? $"Instantiate asset '{assetName}' failed.");
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
                var op = Addressables.LoadSceneAsync(sceneAssetName,LoadSceneMode.Additive);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.PercentComplete);
                    await UniTask.Yield();
                }
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    handle.RegisterReleaseAction(() => Addressables.Release(op));
                    handle.SetCompleted();
                }
                else
                {
                    handle.SetError(op.OperationException?.Message ?? $"Load scene '{sceneAssetName}' failed.");
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
                var op = Addressables.LoadAssetAsync<TextAsset>(binaryAssetName);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.PercentComplete);
                    await UniTask.Yield();
                }
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    handle.RegisterReleaseAction(() => Addressables.Release(op));
                    handle.SetResult(op.Result.bytes);
                }
                else
                {
                    handle.SetError(op.OperationException?.Message ?? $"Load binary '{binaryAssetName}' failed.");
                    Addressables.Release(op);
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
                var op = Addressables.LoadAssetsAsync<T>(tag, null);
                while (!op.IsDone)
                {
                    handle.SetProgress(op.PercentComplete);
                    await UniTask.Yield();
                }
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    handle.RegisterReleaseAction(() => Addressables.Release(op));
                    handle.SetResult(new List<T>(op.Result));
                }
                else
                {
                    handle.SetError(op.OperationException?.Message ?? $"Load assets by tag '{tag}' failed.");
                    Addressables.Release(op);
                }
            }
            catch (Exception ex) { handle.SetError(ex.Message); }
        }

        private string OnInternalIdTransformFunc(IResourceLocation location)
        {
            if (_gameSetting.cdnType == CdnType.Local)
            {
                return location.InternalId;
            }

            if (location.ResourceType == typeof(IAssetBundleResource) && location.InternalId.StartsWith(ReplaceRemote))
            {
                return ReplaceUrl(location.InternalId, _gameSetting);
            }

            if (location.ResourceType == typeof(ContentCatalogData) && location.InternalId.StartsWith(ReplaceRemote))
            {
                return ReplaceUrl(location.InternalId, _gameSetting);
            }

            if (location.PrimaryKey == "AddressablesMainContentCatalogRemoteHash")
            {
                return ReplaceUrl(location.InternalId, _gameSetting);
            }

            return location.InternalId;
        }

        private string ReplaceUrl(string internalId, GameSetting setting)
        {
            var newUrl = setting.GetCdnUrl();
            var addressKey =
 internalId.Replace(ReplaceRemote, newUrl).Replace(ReplaceVersion, setting.GetResourceVersion(_settingComponent));
            return addressKey;
        }
    }
}
#endif
