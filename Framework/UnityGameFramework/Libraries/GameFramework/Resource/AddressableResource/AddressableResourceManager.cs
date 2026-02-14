using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Download;
using GameFramework.FileSystem;
using GameFramework.ObjectPool;


namespace GameFramework.Resource
{
    public class AddressableResourceManager : GameFrameworkModule, IResourceManager
    {
        private AddressableResourceLoader _addressableResourceLoader;
        internal IResourceHelper m_ResourceHelper;
        private ResourceMode m_ResourceMode;
        private bool m_RefuseSetFlag;
        private InitResourcesCompleteCallback m_InitResourcesCompleteCallback;
        private AddressableResourceIniter _aaAddressableResourceIniter;
        private string m_ReadOnlyPath;
        private string m_ReadWritePath;


        public string ReadOnlyPath => m_ReadOnlyPath;
        public string ReadWritePath => m_ReadWritePath;

        public ResourceMode ResourceMode => m_ResourceMode;
        public string CurrentVariant { get; }
        public PackageVersionListSerializer PackageVersionListSerializer { get; }
        public UpdatableVersionListSerializer UpdatableVersionListSerializer { get; }
        public ReadOnlyVersionListSerializer ReadOnlyVersionListSerializer { get; }
        public ReadWriteVersionListSerializer ReadWriteVersionListSerializer { get; }
        public ResourcePackVersionListSerializer ResourcePackVersionListSerializer { get; }
        public string ApplicableGameVersion { get; }
        public int InternalResourceVersion { get; }
        public int AssetCount { get; }
        public int ResourceCount { get; }
        public int ResourceGroupCount { get; }
        public string UpdatePrefixUri { get; set; }
        public int GenerateReadWriteVersionListLength { get; set; }
        public string ApplyingResourcePackPath { get; }
        public int ApplyWaitingCount { get; }
        public int UpdateRetryCount { get; set; }
        public IResourceGroup UpdatingResourceGroup { get; }
        public int UpdateWaitingCount { get; }
        public int UpdateWaitingWhilePlayingCount { get; }
        public int UpdateCandidateCount { get; }
        public int LoadTotalAgentCount { get; }
        public int LoadFreeAgentCount { get; }
        public int LoadWorkingAgentCount { get; }
        public int LoadWaitingTaskCount { get; }
        public float AssetAutoReleaseInterval { get; set; }
        public int AssetCapacity { get; set; }
        public float AssetExpireTime { get; set; }
        public int AssetPriority { get; set; }
        public float ResourceAutoReleaseInterval { get; set; }
        public int ResourceCapacity { get; set; }
        public float ResourceExpireTime { get; set; }
        public int ResourcePriority { get; set; }
        public event EventHandler<ResourceVerifyStartEventArgs> ResourceVerifyStart;
        public event EventHandler<ResourceVerifySuccessEventArgs> ResourceVerifySuccess;
        public event EventHandler<ResourceVerifyFailureEventArgs> ResourceVerifyFailure;
        public event EventHandler<ResourceApplyStartEventArgs> ResourceApplyStart;
        public event EventHandler<ResourceApplySuccessEventArgs> ResourceApplySuccess;
        public event EventHandler<ResourceApplyFailureEventArgs> ResourceApplyFailure;
        public event EventHandler<ResourceUpdateStartEventArgs> ResourceUpdateStart;
        public event EventHandler<ResourceUpdateChangedEventArgs> ResourceUpdateChanged;
        public event EventHandler<ResourceUpdateSuccessEventArgs> ResourceUpdateSuccess;
        public event EventHandler<ResourceUpdateFailureEventArgs> ResourceUpdateFailure;
        public event EventHandler<ResourceUpdateAllCompleteEventArgs> ResourceUpdateAllComplete;

        public AddressableResourceManager()
        {
            _addressableResourceLoader = new AddressableResourceLoader(this);
            m_ResourceMode = ResourceMode.Unspecified;
            m_RefuseSetFlag = false;
            m_ResourceHelper = null;
            m_ReadOnlyPath = null;
            m_ReadWritePath = null;
            m_InitResourcesCompleteCallback = null;
            _aaAddressableResourceIniter = null;
        }

        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            _addressableResourceLoader.Update(elapseSeconds, realElapseSeconds);
        }

        internal override void Shutdown()
        {
            if (_addressableResourceLoader != null)
            {
                _addressableResourceLoader.ShutDown();
                _addressableResourceLoader = null;
            }

            if (_aaAddressableResourceIniter != null)
            {
                _aaAddressableResourceIniter.Shutdown();
                _aaAddressableResourceIniter = null;
            }
        }

        public void SetReadOnlyPath(string readOnlyPath)
        {
            if (string.IsNullOrEmpty(readOnlyPath))
            {
                throw new GameFrameworkException("Read-only path is invalid.");
            }

            if (m_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set read-only path at this time.");
            }

            m_ReadOnlyPath = readOnlyPath;
        }

        public void SetReadWritePath(string readWritePath)
        {
            if (string.IsNullOrEmpty(readWritePath))
            {
                throw new GameFrameworkException("Read-write path is invalid.");
            }

            if (m_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set read-write path at this time.");
            }

            m_ReadWritePath = readWritePath;
        }

        public void SetResourceMode(ResourceMode resourceMode)
        {
            if (resourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("Resource mode is invalid.");
            }

            if (m_RefuseSetFlag)
            {
                throw new GameFrameworkException("You can not set resource mode at this time.");
            }

            if (m_ResourceMode == ResourceMode.Unspecified)
            {
                m_ResourceMode = resourceMode;

                if (m_ResourceMode == ResourceMode.Addressable)
                {
                    _aaAddressableResourceIniter = new AddressableResourceIniter(this);
                    _aaAddressableResourceIniter.ResourceInitComplete += OnIniterResourceInitComplete;
                }
            }
            else if (m_ResourceMode != resourceMode)
            {
                throw new GameFrameworkException("You can not change resource mode at this time.");
            }
        }

        public void SetCurrentVariant(string currentVariant)
        {
        }

        public void SetObjectPoolManager(IObjectPoolManager objectPoolManager)
        {
            throw new NotImplementedException();
        }

        public void SetFileSystemManager(IFileSystemManager fileSystemManager)
        {
            throw new NotImplementedException();
        }

        public void SetDownloadManager(IDownloadManager downloadManager)
        {
            throw new NotImplementedException();
        }

        public void SetDecryptResourceCallback(DecryptResourceCallback decryptResourceCallback)
        {
            throw new NotImplementedException();
        }

        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            if (resourceHelper == null)
            {
                throw new GameFrameworkException("Resource helper is invalid.");
            }

            if (_addressableResourceLoader.TotalAgentCount > 0)
            {
                throw new GameFrameworkException("You must set resource helper before add load resource agent helper.");
            }

            m_ResourceHelper = resourceHelper;
        }

        public void AddLoadResourceAgentHelper(ILoadResourceAgentHelper loadResourceAgentHelper)
        {
            if (m_ResourceHelper == null)
            {
                throw new GameFrameworkException("Resource helper is invalid.");
            }


            _addressableResourceLoader.AddLoadResourceAgentHelper(loadResourceAgentHelper, m_ResourceHelper);
        }

        public void InitResources(InitResourcesCompleteCallback initResourcesCompleteCallback)
        {
            if (initResourcesCompleteCallback == null)
            {
                throw new GameFrameworkException("Init resources complete callback is invalid.");
            }

            if (m_ResourceMode == ResourceMode.Unspecified)
            {
                throw new GameFrameworkException("You must set resource mode first.");
            }

            if (m_ResourceMode != ResourceMode.Addressable)
            {
                throw new GameFrameworkException("You can not use InitResources without package resource mode.");
            }

            if (_aaAddressableResourceIniter == null)
            {
                throw new GameFrameworkException("You can not use InitResources at this time.");
            }

            m_RefuseSetFlag = true;
            m_InitResourcesCompleteCallback = initResourcesCompleteCallback;
            _aaAddressableResourceIniter.InitResources();
        }

        public CheckVersionListResult CheckVersionList(int latestInternalResourceVersion)
        {
            throw new NotImplementedException();
        }

        public void UpdateVersionList(int versionListLength, int versionListHashCode, int versionListCompressedLength,
            int versionListCompressedHashCode, UpdateVersionListCallbacks updateVersionListCallbacks)
        {
            throw new NotImplementedException();
        }

        public void VerifyResources(int verifyResourceLengthPerFrame,
            VerifyResourcesCompleteCallback verifyResourcesCompleteCallback)
        {
            throw new NotImplementedException();
        }

        public void CheckResources(bool ignoreOtherVariant,
            CheckResourcesCompleteCallback checkResourcesCompleteCallback)
        {
            throw new NotImplementedException();
        }

        public void ApplyResources(string resourcePackPath,
            ApplyResourcesCompleteCallback applyResourcesCompleteCallback)
        {
            throw new NotImplementedException();
        }

        public void UpdateResources(UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            throw new NotImplementedException();
        }

        public void UpdateResources(string resourceGroupName,
            UpdateResourcesCompleteCallback updateResourcesCompleteCallback)
        {
            throw new NotImplementedException();
        }

        public void StopUpdateResources()
        {
            throw new NotImplementedException();
        }

        public bool VerifyResourcePack(string resourcePackPath)
        {
            throw new NotImplementedException();
        }

        public TaskInfo[] GetAllLoadAssetInfos()
        {
            throw new NotImplementedException();
        }

        public void GetAllLoadAssetInfos(List<TaskInfo> results)
        {
            throw new NotImplementedException();
        }

        public HasAssetResult HasAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            return _addressableResourceLoader.HasAsset(assetName);
        }

        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }
        }

        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName,
                assetType,
                Constant.DefaultPriority, loadAssetCallbacks,
                null);
        }

        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName, null, priority, loadAssetCallbacks, null);
        }

        public void LoadAsset(string assetName, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName, null, Constant.DefaultPriority, loadAssetCallbacks,
                userData);
        }

        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, null);
        }

        public void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName, assetType, Constant.DefaultPriority, loadAssetCallbacks,
                userData);
        }

        public void LoadAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName, null, priority, loadAssetCallbacks, userData);
        }

        public void LoadAsset(string assetName, Type assetType, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            }

            _addressableResourceLoader.LoadAsset(assetName, assetType, priority, loadAssetCallbacks, userData);
        }


        public void InstantiateAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks,
            object userData)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Instantiate asset callbacks is invalid.");
            }

            _addressableResourceLoader.InstantiateAsset(assetName, priority, loadAssetCallbacks, userData);
        }

        public void InstantiateAsset(string assetName, int priority, LoadAssetCallbacks loadAssetCallbacks)
        {
            if (string.IsNullOrEmpty(assetName))
            {
                throw new GameFrameworkException("Asset name is invalid.");
            }

            if (loadAssetCallbacks == null)
            {
                throw new GameFrameworkException("Instantiate asset callbacks is invalid.");
            }

            _addressableResourceLoader.InstantiateAsset(assetName, priority, loadAssetCallbacks, null);
        }

        public void UnloadAsset(object asset)
        {
            if (asset == null)
            {
                throw new GameFrameworkException("Asset is invalid.");
            }

            if (_addressableResourceLoader == null)
            {
                return;
            }

            _addressableResourceLoader.UnloadAsset(asset);
        }

        public void LoadScene(string sceneAssetName, LoadSceneCallbacks loadSceneCallbacks)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _addressableResourceLoader.LoadScene(sceneAssetName, Constant.DefaultPriority, loadSceneCallbacks, null);
        }

        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _addressableResourceLoader.LoadScene(sceneAssetName, priority, loadSceneCallbacks, null);
        }

        public void LoadScene(string sceneAssetName, LoadSceneCallbacks loadSceneCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _addressableResourceLoader.LoadScene(sceneAssetName, Constant.DefaultPriority, loadSceneCallbacks,
                userData);
        }

        public void LoadScene(string sceneAssetName, int priority, LoadSceneCallbacks loadSceneCallbacks,
            object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (loadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            }

            _addressableResourceLoader.LoadScene(sceneAssetName, priority, loadSceneCallbacks, userData);
        }

        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (unloadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            }

            _addressableResourceLoader.UnloadScene(sceneAssetName, unloadSceneCallbacks, null);
        }

        public void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
            {
                throw new GameFrameworkException("Scene asset name is invalid.");
            }

            if (unloadSceneCallbacks == null)
            {
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            }

            if (_addressableResourceLoader == null)
            {
                return;
            }

            _addressableResourceLoader.UnloadScene(sceneAssetName, unloadSceneCallbacks, userData);
        }

        public string GetBinaryPath(string binaryAssetName)
        {
            throw new NotImplementedException();
        }

        public bool GetBinaryPath(string binaryAssetName, out bool storageInReadOnly, out bool storageInFileSystem,
            out string relativePath, out string fileName)
        {
            throw new NotImplementedException();
        }

        public int GetBinaryLength(string binaryAssetName)
        {
            throw new NotImplementedException();
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (loadBinaryCallbacks == null)
            {
                throw new GameFrameworkException("Load binary callbacks is invalid.");
            }

            _addressableResourceLoader.LoadBinary(binaryAssetName, loadBinaryCallbacks, null);
        }

        public void LoadBinary(string binaryAssetName, LoadBinaryCallbacks loadBinaryCallbacks, object userData)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
            {
                throw new GameFrameworkException("Binary asset name is invalid.");
            }

            if (loadBinaryCallbacks == null)
            {
                throw new GameFrameworkException("Load binary callbacks is invalid.");
            }

            _addressableResourceLoader.LoadBinary(binaryAssetName, loadBinaryCallbacks, userData);
        }

        public byte[] LoadBinaryFromFileSystem(string binaryAssetName)
        {
            throw new NotImplementedException();
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex)
        {
            throw new NotImplementedException();
        }

        public int LoadBinaryFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int length)
        {
            throw new NotImplementedException();
        }

        public byte[] LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, int length)
        {
            throw new NotImplementedException();
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int length)
        {
            throw new NotImplementedException();
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, byte[] buffer, int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer)
        {
            throw new NotImplementedException();
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int length)
        {
            throw new NotImplementedException();
        }

        public int LoadBinarySegmentFromFileSystem(string binaryAssetName, int offset, byte[] buffer, int startIndex,
            int length)
        {
            throw new NotImplementedException();
        }

        public bool HasResourceGroup(string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public IResourceGroup GetResourceGroup()
        {
            throw new NotImplementedException();
        }

        public IResourceGroup GetResourceGroup(string resourceGroupName)
        {
            throw new NotImplementedException();
        }

        public IResourceGroup[] GetAllResourceGroups()
        {
            throw new NotImplementedException();
        }

        public void GetAllResourceGroups(List<IResourceGroup> results)
        {
            throw new NotImplementedException();
        }

        public IResourceGroupCollection GetResourceGroupCollection(params string[] resourceGroupNames)
        {
            throw new NotImplementedException();
        }

        public IResourceGroupCollection GetResourceGroupCollection(List<string> resourceGroupNames)
        {
            throw new NotImplementedException();
        }


        private void OnIniterResourceInitComplete()
        {
            _aaAddressableResourceIniter.ResourceInitComplete -= OnIniterResourceInitComplete;
            _aaAddressableResourceIniter.Shutdown();
            _aaAddressableResourceIniter = null;

            m_InitResourcesCompleteCallback();
            m_InitResourcesCompleteCallback = null;
        }
    }
}