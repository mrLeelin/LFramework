using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
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


        public int AddYooAssetHandler(string name, List<string> labels,
            string packageName, bool autoReleaseHandle = true)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateYooAssetHandler(name, labels, packageName, autoReleaseHandle);
            handle.CheckAndLoadAsync();
            return handle.SerialID;
        }

        public int AddYooAssetHandlerNotRun(string name, List<string> labels,
            string packageName, bool autoReleaseHandle = true)
        {
            var h = GetHandler(name);
            if (h != null)
            {
                return h.SerialID;
            }

            var handle = CreateYooAssetHandler(name, labels, packageName, autoReleaseHandle);
            return handle.SerialID;
        }

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

        public async Task<bool> CheckAssetsInLocal(string key)
        {
            var downloadSizeOp = Addressables.GetDownloadSizeAsync(key);
            await downloadSizeOp.Task;
            var result = false;
            if (downloadSizeOp.Status == AsyncOperationStatus.Succeeded)
            {
                if (downloadSizeOp.Result == 0)
                {
                    Debug.Log($"资源已经在本地 '{key}'");
                    result = true;
                }
                else
                {
                    Debug.Log($"资源需要下载 '{key}'，大小为: {downloadSizeOp.Result} bytes");
                }
            }
            else
            {
                Log.Error($"获取资源大小失败 '{key}'");
            }

            Addressables.Release(downloadSizeOp);
            return result;
        }

        public void DownloadAssets(List<string> keys)
        {
            Addressables.DownloadDependenciesAsync((IEnumerable)keys, Addressables.MergeMode.Union, true);
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

        private YooAssetDownloadHandler CreateYooAssetHandler(string name, List<string> labels,
            string packageName, bool autoReleaseHandle)
        {
            var serialID = GetNextSerialID();
            var handler = new YooAssetDownloadHandler(name, labels, packageName, serialID, autoReleaseHandle);
            RegisterHandler(handler);
            return handler;
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
    }
}