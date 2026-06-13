using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using LFramework.Runtime.Settings;
using UnityEngine;

#if ADDRESSABLE_SUPPORT
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

using UnityGameFramework.Runtime;
using Zenject;

namespace LFramework.Runtime
{
    public class ResourceDownloadComponent : GameFrameworkComponent
    {
        [Inject] private GameSetting GameSetting { get; }
        [Inject] private SettingComponent SettingComponent { get; }
        [Inject] private ResourceComponent ResourceComponent { get; }


        private int _nextSerialID;
        private readonly Dictionary<int, IResourceDownloadHandler> _activeUpdateHandler = new();


        public override void AwakeComponent()
        {
            base.AwakeComponent();
            _nextSerialID = 0;
            _activeUpdateHandler.Clear();
        }


        public override void ShutDown()
        {
            base.ShutDown();
            foreach (var v in _activeUpdateHandler.Keys)
            {
                RemoveHandlerInternal(v);
            }

            _activeUpdateHandler.Clear();
        }

        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            base.UpdateComponent(elapseSeconds, realElapseSeconds);

            foreach (var handler in _activeUpdateHandler.Values)
            {
                var impl = handler as ResourceDownloadHandlerBase;
                impl.OnUpdate(elapseSeconds, realElapseSeconds);
            }
        }

        public void DownloadAssets(List<string> keys)
        {
            DownloadAssets(keys, null);
        }

        public void DownloadAssets(List<string> keys, string packageId)
        {
            List<string> normalizedKeys = NormalizeKeys(keys);
            if (normalizedKeys.Count == 0)
            {
                Log.Warning("[ResourceDownloadComponent] DownloadAssets skipped because keys is empty.");
                return;
            }

            switch (ResourceComponent.ResourceMode)
            {
#if ADDRESSABLE_SUPPORT
                case ResourceMode.Addressable:
                    if (!string.IsNullOrWhiteSpace(packageId))
                    {
                        Log.Warning("[ResourceDownloadComponent] DownloadAssets packageId '{0}' is ignored in Addressable mode.",
                            packageId);
                    }
                    AddAddressableAssetHandler(BuildDownloadAssetsHandlerName("Addressable", normalizedKeys), normalizedKeys);
                    return;
#endif

#if YOOASSET_SUPPORT
                case ResourceMode.YooAsset:
                    DownloadYooAssets(normalizedKeys, packageId);
                    return;
#endif

                default:
                    Log.Error("[ResourceDownloadComponent] DownloadAssets does not support resource mode '{0}'.",
                        ResourceComponent.ResourceMode);
                    return;
            }
        }

#if ADDRESSABLE_SUPPORT
        public int AddUpdateHandlerNotRun(string name, List<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union, bool autoReleaseHandle = true)
        {
            //不可以重复添加下载器
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateAddressableHandler(name, labels, mergeMode, autoReleaseHandle);
            return handle.SerialID;
        }

        public int AddUpdateHandler(string name, List<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union, bool autoReleaseHandle = true)
        {
            //不可以重复添加下载器
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateAddressableHandler(name, labels, mergeMode, autoReleaseHandle);
            handle.CheckAndLoadAsync();
            return handle.SerialID;
        }

        public int AddAddressableAssetHandler(string name, List<string> keys,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union, bool autoReleaseHandle = true)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateAddressableAssetHandler(name, keys, mergeMode, autoReleaseHandle);
            handle.CheckAndLoadAsync();
            return handle.SerialID;
        }

        public int AddAddressableAssetHandlerNotRun(string name, List<string> keys,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union, bool autoReleaseHandle = true)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateAddressableAssetHandler(name, keys, mergeMode, autoReleaseHandle);
            return handle.SerialID;
        }

        private AddressableDownloadHandler CreateAddressableHandler(string name, List<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union,
            bool autoReleaseHandle = true)
        {
            var serialID = GetNextSerialID();
            var handler = new AddressableDownloadHandler(name, labels, mergeMode, serialID, autoReleaseHandle);
            RegisterHandler(handler);
            return handler;
        }

        private AddressableAssetDownloadHandler CreateAddressableAssetHandler(string name, List<string> keys,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union,
            bool autoReleaseHandle = true)
        {
            var serialID = GetNextSerialID();
            var handler = new AddressableAssetDownloadHandler(name, keys, mergeMode, serialID, autoReleaseHandle);
            RegisterHandler(handler);
            return handler;
        }
#endif

#if YOOASSET_SUPPORT
        public int AddYooAssetHandler(string name, List<string> labels,
            string packageName, bool autoReleaseHandle = true, bool checkDownloadedTags = false)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateYooAssetHandler(name, labels, packageName, autoReleaseHandle, checkDownloadedTags);
            handle.CheckAndLoadAsync();
            return handle.SerialID;
        }

        public int AddYooAssetHandlerNotRun(string name, List<string> labels,
            string packageName, bool autoReleaseHandle = true, bool checkDownloadedTags = false)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateYooAssetHandler(name, labels, packageName, autoReleaseHandle, checkDownloadedTags);
            return handle.SerialID;
        }

        public int AddYooAssetAssetHandler(string name, List<string> assetLocations,
            string packageName, bool autoReleaseHandle = true)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateYooAssetAssetHandler(name, assetLocations, packageName,
                autoReleaseHandle, true);
            handle.CheckAndLoadAsync();
            return handle.SerialID;
        }

        private YooAssetDownloadHandler CreateYooAssetHandler(string name, List<string> labels,
            string packageName, bool autoReleaseHandle, bool checkDownloadedTags = false,
            bool downloadByLocation = false)
        {
            var serialID = GetNextSerialID();
            var handler = new YooAssetDownloadHandler(name, labels, packageName, serialID,
                autoReleaseHandle, checkDownloadedTags, downloadByLocation);
            RegisterHandler(handler);
            return handler;
        }

        private YooAssetAssetDownloadHandler CreateYooAssetAssetHandler(string name, List<string> assetLocations,
            string packageName, bool autoReleaseHandle, bool downloadByLocation)
        {
            var serialID = GetNextSerialID();
            var handler = new YooAssetAssetDownloadHandler(name, assetLocations, packageName, serialID,
                autoReleaseHandle, downloadByLocation, EnsureYooAssetPackageReadyAsync);
            RegisterHandler(handler);
            return handler;
        }
#endif

#if YOOASSET_SUPPORT
        private void DownloadYooAssets(List<string> keys, string packageId)
        {
            ResourceComponentSetting setting = SettingManager.GetProjectSelector()?.GetComponentSetting<ResourceComponentSetting>();
            if (setting == null)
            {
                Log.Error("[ResourceDownloadComponent] DownloadAssets failed because ResourceComponentSetting is null.");
                return;
            }

            List<string> packageNames = YooAssetMultiPackageUtility.ResolveDownloadAssetsPackageNames(
                setting,
                Application.platform,
                GetCurrentChannel(),
                packageId);
            if (packageNames.Count == 0)
            {
                Log.Error("[ResourceDownloadComponent] DownloadAssets failed because YooAsset package name is empty. packageId: {0}",
                    packageId);
                return;
            }

            foreach (string packageName in packageNames)
            {
                AddYooAssetAssetHandler(BuildDownloadAssetsHandlerName("YooAsset", packageName, keys),
                    keys,
                    packageName);
            }
        }

        private async UniTask<PackageInitializationResult> EnsureYooAssetPackageReadyAsync(string packageName)
        {
            var helper = ResourceComponent != null
                ? ResourceComponent.GetComponentInChildren<YooAssetResourceHelper>(true)
                : null;
            if (helper == null)
            {
                return PackageInitializationResult.CreateFailure(packageName,
                    "YooAssetResourceHelper is unavailable.");
            }

            return await helper.EnsurePackageReadyAsync(packageName);
        }
#endif

        public IResourceDownloadHandler GetHandler(int serialID)
        {
            return _activeUpdateHandler.GetValueOrDefault(serialID);
        }

        public IResourceDownloadHandler GetHandler(string name)
        {
            foreach (var handler in _activeUpdateHandler.Values)
            {
                if (!handler.Name.Equals(name))
                {
                    continue;
                }

                return handler;
            }

            return null;
        }

        public void RemoveHandler(int serialID)
        {
            if (!RemoveHandlerInternal(serialID))
            {
                return;
            }

            _activeUpdateHandler.Remove(serialID);
        }

        public bool RemoveHandlerInternal(int serialID)
        {
            if (GetHandler(serialID) is not ResourceDownloadHandlerBase handler)
            {
                return false;
            }

            handler.RemoveHandleAction -= OnRemoveHandleAction;
            handler.DownloadFailureEventHandler -= OnUpdateAssetsFailure;
            handler.DownloadSuccessfulEventHandler -= OnUpdateAssetsSuccessful;

            return true;
        }

        private void RegisterHandler(ResourceDownloadHandlerBase handler)
        {
            _activeUpdateHandler.Add(handler.SerialID, handler);
            handler.RemoveHandleAction += OnRemoveHandleAction;
            handler.DownloadFailureEventHandler += OnUpdateAssetsFailure;
            handler.DownloadSuccessfulEventHandler += OnUpdateAssetsSuccessful;
        }


        private void OnRemoveHandleAction(ResourceDownloadHandlerBase obj)
        {
            RemoveHandler(obj.SerialID);
        }

        private void OnUpdateAssetsSuccessful(object sender, ResourcesDownloadSuccessfulEvent e)
        {
            //Fire
        }

        private void OnUpdateAssetsFailure(object sender, ResourcesDownloadFailureEvent e)
        {
            //Fire
        }


        private int GetNextSerialID() => ++_nextSerialID;

        private static List<string> NormalizeKeys(List<string> keys)
        {
            var normalizedKeys = new List<string>();
            if (keys == null)
            {
                return normalizedKeys;
            }

            var uniqueKeys = new HashSet<string>();
            foreach (string key in keys)
            {
                if (string.IsNullOrWhiteSpace(key) || !uniqueKeys.Add(key))
                {
                    continue;
                }

                normalizedKeys.Add(key);
            }

            return normalizedKeys;
        }

        private static string BuildDownloadAssetsHandlerName(string resourceType, List<string> keys)
        {
            return BuildDownloadAssetsHandlerName(resourceType, string.Empty, keys);
        }

        private static string BuildDownloadAssetsHandlerName(string resourceType, string packageName, List<string> keys)
        {
            string keyPart = string.Join("|", keys);
            return string.IsNullOrWhiteSpace(packageName)
                ? $"DownloadAssets:{resourceType}:{keyPart}"
                : $"DownloadAssets:{resourceType}:{packageName}:{keyPart}";
        }

        private static string GetCurrentChannel()
        {
            GameSetting gameSetting = SettingManager.GetSetting<GameSetting>();
            return gameSetting != null ? gameSetting.channel : string.Empty;
        }
    }
}
