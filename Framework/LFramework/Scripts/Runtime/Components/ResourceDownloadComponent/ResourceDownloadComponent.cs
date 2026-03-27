using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using GameFramework.Resource;
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

        private AddressableDownloadHandler CreateAddressableHandler(string name, List<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union,
            bool autoReleaseHandle = true)
        {
            var serialID = GetNextSerialID();
            var handler = new AddressableDownloadHandler(name, labels, mergeMode, serialID, autoReleaseHandle);
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

        private YooAssetDownloadHandler CreateYooAssetHandler(string name, List<string> labels,
            string packageName, bool autoReleaseHandle, bool checkDownloadedTags = false)
        {
            var serialID = GetNextSerialID();
            var handler = new YooAssetDownloadHandler(name, labels, packageName, serialID,
                autoReleaseHandle, checkDownloadedTags);
            RegisterHandler(handler);
            return handler;
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
    }
}