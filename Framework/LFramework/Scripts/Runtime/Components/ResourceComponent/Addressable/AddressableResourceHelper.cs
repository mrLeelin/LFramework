#if ADDRESSABLE_SUPPORT
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private ResourceComponentSetting _resourceComponentSetting;
        private readonly Dictionary<string, HasAssetResult> _hasAssetCache =
            new Dictionary<string, HasAssetResult>(StringComparer.Ordinal);

        private void Awake()
        {
            _gameSetting = SettingManager.GetSetting<GameSetting>();
            _resourceComponentSetting = SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();
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
                    _hasAssetCache.Clear();
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
            if (string.IsNullOrWhiteSpace(assetName))
            {
                return HasAssetResult.NotExist;
            }

            if (_hasAssetCache.TryGetValue(assetName, out HasAssetResult cachedResult))
            {
                return cachedResult;
            }

            bool hasLocator = false;
            foreach (IResourceLocator locator in Addressables.ResourceLocators)
            {
                if (locator == null)
                {
                    continue;
                }

                hasLocator = true;
                if (locator.Locate(assetName, null, out IList<IResourceLocation> locations) &&
                    locations != null &&
                    locations.Count > 0)
                {
                    _hasAssetCache[assetName] = HasAssetResult.Exist;
                    return HasAssetResult.Exist;
                }
            }

            if (!hasLocator)
            {
                return HasAssetResult.NotReady;
            }

            _hasAssetCache[assetName] = HasAssetResult.NotExist;
            return HasAssetResult.NotExist;
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
            LoadAssetInternal(
                assetName,
                ResourceAssetTypeUtility.GetLoadTypeFallbackChain(assetType),
                callbacks,
                userData,
                0,
                null);
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
                var typedOp = Addressables.LoadAssetAsync<T>(assetName);
                while (!typedOp.IsDone)
                {
                    handle.SetProgress(typedOp.PercentComplete);
                    await UniTask.Yield();
                }

                if (typedOp.Status == AsyncOperationStatus.Succeeded)
                {
                    handle.RegisterReleaseAction(() => SafeRelease(typedOp));
                    handle.SetProgress(1f);
                    handle.SetResult(typedOp.Result);
                    return;
                }

                string accumulatedError = AppendLoadError(
                    null,
                    typeof(T),
                    typedOp.OperationException?.Message ?? $"Load asset '{assetName}' failed.");
                SafeRelease(typedOp);

                var fallbackOp = Addressables.LoadAssetAsync<object>(assetName);
                while (!fallbackOp.IsDone)
                {
                    handle.SetProgress(fallbackOp.PercentComplete);
                    await UniTask.Yield();
                }

                if (fallbackOp.Status == AsyncOperationStatus.Succeeded)
                {
                    if (ResourceAssetTypeUtility.TryConvertLoadedObject(
                            fallbackOp.Result,
                            typeof(T),
                            assetName,
                            out var typedAsset,
                            out var errorMessage))
                    {
                        handle.RegisterReleaseAction(() => SafeRelease(fallbackOp));
                        handle.SetProgress(1f);
                        handle.SetResult((T)typedAsset);
                    }
                    else
                    {
                        handle.SetError(errorMessage);
                        SafeRelease(fallbackOp);
                    }

                    return;
                }

                accumulatedError = AppendLoadError(
                    accumulatedError,
                    typeof(object),
                    fallbackOp.OperationException?.Message ?? $"Load asset '{assetName}' failed.");
                SafeRelease(fallbackOp);
                handle.SetError(accumulatedError);
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

        private static void SafeRelease<T>(AsyncOperationHandle<T> handle)
        {
            if (!handle.IsValid())
            {
                return;
            }

            try
            {
                Addressables.Release(handle);
            }
            catch (InvalidOperationException ex) when (IsInvalidHandleException(ex))
            {
                Log.Warning("Addressables release skipped because the handle is invalid: {0}", ex.Message);
            }
            catch (Exception ex) when (IsInvalidHandleException(ex))
            {
                Log.Warning("Addressables release skipped because the handle is invalid: {0}", ex.Message);
            }
        }

        private static bool IsInvalidHandleException(Exception ex)
        {
            return ex.Message != null && ex.Message.Contains("invalid operation handle");
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
            if (location == null)
            {
                return null;
            }

            string transformedId = location.InternalId;
            string internalId = location.InternalId ?? string.Empty;
            if (_gameSetting.cdnType == CdnType.Local)
            {
                LogLoadUrlIfEnabled(location, transformedId, false);
                return transformedId;
            }

            if (location.ResourceType == typeof(IAssetBundleResource) && internalId.StartsWith(ReplaceRemote))
            {
                transformedId = ReplaceUrl(internalId, _gameSetting);
                LogLoadUrlIfEnabled(location, transformedId, true);
                return transformedId;
            }

            if (location.ResourceType == typeof(ContentCatalogData) && internalId.StartsWith(ReplaceRemote))
            {
                transformedId = ReplaceUrl(internalId, _gameSetting);
                LogLoadUrlIfEnabled(location, transformedId, true);
                return transformedId;
            }

            if (location.PrimaryKey == "AddressablesMainContentCatalogRemoteHash")
            {
                transformedId = ReplaceUrl(internalId, _gameSetting);
                LogLoadUrlIfEnabled(location, transformedId, true);
                return transformedId;
            }

            LogLoadUrlIfEnabled(location, transformedId, false);
            return transformedId;
        }

        private string ReplaceUrl(string internalId, GameSetting setting)
        {
            var newUrl = setting.GetCdnUrl();
            var addressKey =
                internalId.Replace(ReplaceRemote, newUrl)
                    .Replace(ReplaceVersion, setting.GetResourceVersion(_settingComponent));
            if (_resourceComponentSetting != null && _resourceComponentSetting.ForceSingleSlashUrls)
            {
                addressKey = ForceSingleSlashUrl(addressKey);
            }

            return addressKey;
        }

        private void LogLoadUrlIfEnabled(IResourceLocation location, string transformedId, bool remoteMatch)
        {
            if (_resourceComponentSetting == null || !_resourceComponentSetting.LogLoadUrls)
            {
                return;
            }

            string url = transformedId ?? location?.InternalId;
            if (!remoteMatch && !IsLikelyUrl(url))
            {
                return;
            }

            Log.Info(BuildLoadUrlLogMessage(
                location?.PrimaryKey,
                location?.ResourceType,
                location?.InternalId,
                url,
                BuildLoadUrlStackTrace()));
        }

        private static bool IsLikelyUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.IndexOf("://", StringComparison.Ordinal) >= 0 ||
                   value.StartsWith(ReplaceRemote, StringComparison.Ordinal);
        }

        private static string BuildLoadUrlLogMessage(
            string primaryKey,
            Type resourceType,
            string internalId,
            string url,
            string stackTrace)
        {
            return "[ResourceLoadUrl] Backend: Addressables, " +
                   $"PrimaryKey: {primaryKey ?? "<null>"}, " +
                   $"ResourceType: {resourceType?.Name ?? "<null>"}, " +
                   $"InternalId: {internalId ?? "<null>"}, " +
                   $"Url: {url ?? "<null>"}, " +
                   $"Stack: {stackTrace ?? "<empty>"}";
        }

        private static string BuildLoadUrlStackTrace()
        {
            var stackTrace = new StackTrace(2, false);
            StackFrame[] frames = stackTrace.GetFrames();
            if (frames == null || frames.Length == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            for (int i = 0; i < frames.Length && parts.Count < 12; i++)
            {
                var method = frames[i].GetMethod();
                Type declaringType = method?.DeclaringType;
                if (method == null || declaringType == null)
                {
                    continue;
                }

                string fullName = declaringType.FullName;
                if (string.IsNullOrEmpty(fullName) ||
                    fullName.StartsWith("UnityEngine.", StringComparison.Ordinal) ||
                    fullName.StartsWith("UnityEngine.ResourceManagement.", StringComparison.Ordinal) ||
                    fullName.StartsWith("UnityEngine.AddressableAssets.", StringComparison.Ordinal))
                {
                    continue;
                }

                parts.Add($"{fullName}.{method.Name}");
            }

            return string.Join(" <- ", parts);
        }

        private static string ForceSingleSlashUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return url;
            }

            var protocolIndex = url.IndexOf("://", StringComparison.Ordinal);
            var startIndex = protocolIndex >= 0 ? protocolIndex + 3 : 0;
            while (true)
            {
                var duplicateSlashIndex = url.IndexOf("//", startIndex, StringComparison.Ordinal);
                if (duplicateSlashIndex < 0)
                {
                    return url;
                }

                url = url.Remove(duplicateSlashIndex, 1);
            }
        }

        private void LoadAssetInternal(
            string assetName,
            Type[] loadTypes,
            LoadAssetCallbacks callbacks,
            object userData,
            int index,
            string accumulatedError)
        {
            Type loadType = loadTypes[index];
            if (loadType == null || loadType == typeof(object))
            {
                LoadAssetAsObject(assetName, loadTypes, callbacks, userData, index, accumulatedError);
                return;
            }

            var locationHandle = Addressables.LoadResourceLocationsAsync(assetName, loadType);
            locationHandle.Completed += locationsOp =>
            {
                if (locationsOp.Status == AsyncOperationStatus.Succeeded &&
                    locationsOp.Result != null &&
                    locationsOp.Result.Count > 0)
                {
                    LoadAssetFromLocation(
                        assetName,
                        locationsOp.Result[0],
                        loadTypes,
                        callbacks,
                        userData,
                        index,
                        accumulatedError,
                        locationsOp);
                    return;
                }

                string nextError = AppendLoadError(
                    accumulatedError,
                    loadType,
                    locationsOp.OperationException?.Message ?? $"No resource location found for '{assetName}'.");
                Addressables.Release(locationsOp);
                ContinueLoadAssetFallback(assetName, loadTypes, callbacks, userData, index, nextError);
            };
        }

        private void LoadAssetFromLocation(
            string assetName,
            IResourceLocation location,
            Type[] loadTypes,
            LoadAssetCallbacks callbacks,
            object userData,
            int index,
            string accumulatedError,
            AsyncOperationHandle<IList<IResourceLocation>> locationHandle)
        {
            var assetHandle = Addressables.LoadAssetAsync<object>(location);
            assetHandle.Completed += op =>
            {
                Addressables.Release(locationHandle);

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, op.Result, 0f, userData);
                    return;
                }

                string nextError = AppendLoadError(
                    accumulatedError,
                    loadTypes[index],
                    op.OperationException?.Message ?? "Load failed.");
                Addressables.Release(op);
                ContinueLoadAssetFallback(assetName, loadTypes, callbacks, userData, index, nextError);
            };
        }

        private void LoadAssetAsObject(
            string assetName,
            Type[] loadTypes,
            LoadAssetCallbacks callbacks,
            object userData,
            int index,
            string accumulatedError)
        {
            var assetHandle = Addressables.LoadAssetAsync<object>(assetName);
            assetHandle.Completed += op =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, op.Result, 0f, userData);
                    return;
                }

                string nextError = AppendLoadError(
                    accumulatedError,
                    loadTypes[index],
                    op.OperationException?.Message ?? "Load failed.");
                Addressables.Release(op);
                ContinueLoadAssetFallback(assetName, loadTypes, callbacks, userData, index, nextError);
            };
        }

        private void ContinueLoadAssetFallback(
            string assetName,
            Type[] loadTypes,
            LoadAssetCallbacks callbacks,
            object userData,
            int index,
            string nextError)
        {
            if (index + 1 < loadTypes.Length)
            {
                LoadAssetInternal(assetName, loadTypes, callbacks, userData, index + 1, nextError);
                return;
            }

            callbacks.LoadAssetFailureCallback?.Invoke(
                assetName, LoadResourceStatus.NotExist, nextError, userData);
        }

        private static string AppendLoadError(string accumulatedError, Type loadType, string currentError)
        {
            string scopedError = $"[{loadType?.FullName ?? "object"}] {currentError}";
            if (string.IsNullOrEmpty(accumulatedError))
            {
                return scopedError;
            }

            return $"{accumulatedError}\n{scopedError}";
        }
    }
}
#endif
