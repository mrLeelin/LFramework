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


        private const string ReplaceRemote = "remote_";
        private const string ReplaceVersion = "_resource_version_";


        private int _nextSerialID;
        private readonly Dictionary<int, IResourceDownloadHandler> _activeUpdateHandler = new();


        public override void AwakeComponent()
        {
            base.AwakeComponent();
            _nextSerialID = 0;
            _activeUpdateHandler.Clear();
            if (ResourceComponent.ResourceMode == ResourceMode.Addressable)
            {
                Addressables.InternalIdTransformFunc = OnInternalIdTransformFunc;
            }else if (ResourceComponent.ResourceMode == ResourceMode.YooAsset)
            {
                
            }
            else
            {
                Log.Error($"UnSupport ResourceModel '{ResourceComponent.ResourceMode}'");
            }
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
                var impl = handler as ResourceDownloadHandler;
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

            var handle = CreateDownloadHandler(name, labels, mergeMode, autoReleaseHandle);
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

            var handle = CreateDownloadHandler(name, labels, mergeMode, autoReleaseHandle);
            handle.CheckAndLoadAsync();
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
            if (GetHandler(serialID) is not ResourceDownloadHandler handler)
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

        private ResourceDownloadHandler CreateDownloadHandler(string name, List<string> labels,
            Addressables.MergeMode mergeMode = Addressables.MergeMode.Union,
            bool autoReleaseHandle = true)
        {
            //不可以重复添加下载器
            var serialID = GetNextSerialID();
            var handler = new ResourceDownloadHandler(name, labels, mergeMode, serialID, autoReleaseHandle);
            _activeUpdateHandler.Add(serialID, handler);
            handler.RemoveHandleAction += OnRemoveHandleAction;
            handler.DownloadFailureEventHandler += OnUpdateAssetsFailure;
            handler.DownloadSuccessfulEventHandler += OnUpdateAssetsSuccessful;
            return handler;
        }

        private string OnInternalIdTransformFunc(IResourceLocation location)
        {
            /*
            if (!GameSetting.isRelease)
            {
                Log.Debug("OnInternalIdTransformFunc , location = " + location.PrimaryKey);
            }
            */

            if (GameSetting.cdnType == CdnType.Local)
            {
                return location.InternalId;
            }

            if (location.ResourceType == typeof(IAssetBundleResource) && location.InternalId.StartsWith(ReplaceRemote))
            {
                // 远程AssetBundle
                return ReplaceUrl(location.InternalId);
            }

            if (location.ResourceType == typeof(ContentCatalogData) && location.InternalId.StartsWith(ReplaceRemote))
            {
                // 远程catalog文件
                return ReplaceUrl(location.InternalId);
            }

            if (location.PrimaryKey == "AddressablesMainContentCatalogRemoteHash")
            {
                //Log.Info($"LoadFunc , key = {location.PrimaryKey}");
                // 远程catalog文件hash
                return ReplaceUrl(location.InternalId);
            }

            return location.InternalId;

            /*
            var cdnType = GameSetting.cdnType;
            if (location.Data is AssetBundleRequestOptions)
            {
                if (location.InternalId.StartsWith("remote"))
                {
                    return $"{ConstUrl.GetCdnUrl(cdnType)}{location.PrimaryKey}";
                }
            }

            if (location.InternalId.Contains("/catalog"))
            {
                if (GameSetting.cdnType != CdnType.Local)
                {
                    if (location.InternalId.StartsWith("http"))
                    {
                        if (location.InternalId.EndsWith(".json"))
                        {
                            return $"{ConstUrl.GetCdnUrl(cdnType)}{location.PrimaryKey}";
                        }
                        else if (location.InternalId.EndsWith(".hash"))
                        {
                            return $"{ConstUrl.GetCdnUrl(cdnType)}{location.PrimaryKey}";
                        }
                    }
                    /===
                    else if (location.InternalId.EndsWith(".hash"))
                    {
                        return Regex.Replace(location.InternalId, @"/catalog_.*\.hash",
                            string.Format("/catalog_{0}.hash", 0));
                    }
                    ====/
                }
            }
    */

            return location.InternalId;
        }

        private string ReplaceUrl(string internalId)
        {
            var newUrl = GameSetting.GetCdnUrl();
            // 是AssetBundle并且是http网络请求
            var addressKey = internalId.Replace(ReplaceRemote, newUrl)
                .Replace(ReplaceVersion, GameSetting.GetResourceVersion(SettingComponent));
            /*
            if (!GameSetting.isRelease)
            {
                Log.Debug($"replace url , internalId={internalId} addressKey={addressKey}");
            }
            */

            return addressKey;
        }

        private void OnRemoveHandleAction(ResourceDownloadHandler obj)
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