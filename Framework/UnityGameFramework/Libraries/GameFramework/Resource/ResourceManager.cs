using System;
using System.Collections.Generic;
using UnityEngine.Scripting;

namespace GameFramework.Resource
{
    /// <summary>
    /// 璧勬簮绠＄悊鍣紙鍞竴瀹炵幇锛?
    /// </summary>
    [Preserve]
    internal sealed class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private string _readOnlyPath;
        private string _readWritePath;
        private ResourceMode _resourceMode;
        private IResourceHelper _resourceHelper;

        /// <summary>
        /// 鍒濆鍖栬祫婧愮鐞嗗櫒
        /// </summary>
        public ResourceManager()
        {
            _readOnlyPath = null;
            _readWritePath = null;
            _resourceMode = ResourceMode.Unspecified;
            _resourceHelper = null;
        }

        /// <summary>
        /// 鑾峰彇璧勬簮鍙璺緞
        /// </summary>
        public string ReadOnlyPath => _readOnlyPath;

        /// <summary>
        /// 鑾峰彇璧勬簮璇诲啓璺緞
        /// </summary>
        public string ReadWritePath => _readWritePath;

        /// <summary>
        /// 鑾峰彇璧勬簮妯″紡
        /// </summary>
        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// 鑾峰彇妯″潡浼樺厛绾?
        /// </summary>
        internal override int Priority => 4;

        /// <summary>
        /// 璁剧疆璧勬簮鍙璺緞
        /// </summary>
        public void SetReadOnlyPath(string readOnlyPath)
        {
            if (string.IsNullOrEmpty(readOnlyPath))
                throw new GameFrameworkException("Read-only path is invalid.");
            _readOnlyPath = readOnlyPath;
        }

        /// <summary>
        /// 璁剧疆璧勬簮璇诲啓璺緞
        /// </summary>
        public void SetReadWritePath(string readWritePath)
        {
            if (string.IsNullOrEmpty(readWritePath))
                throw new GameFrameworkException("Read-write path is invalid.");
            _readWritePath = readWritePath;
        }

        /// <summary>
        /// 璁剧疆璧勬簮妯″紡
        /// </summary>
        public void SetResourceMode(ResourceMode resourceMode)
        {
            if (resourceMode == ResourceMode.Unspecified)
                throw new GameFrameworkException("Resource mode is invalid.");
            _resourceMode = resourceMode;
        }

        /// <summary>
        /// 璁剧疆璧勬簮杈呭姪鍣?
        /// </summary>
        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            _resourceHelper = resourceHelper ?? throw new GameFrameworkException("Resource helper is invalid.");
        }

        /// <summary>
        /// 鍒濆鍖栬祫婧?
        /// </summary>
        public void InitResources(InitResourcesCompleteCallback callback)
        {
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            var initCallBack = new ResourceInitCallBack(
                () => callback?.Invoke(),
                (errorMessage) => GameFrameworkLog.Error(errorMessage)
            );
            _resourceHelper.InitializeResources(initCallBack);
        }

        /// <summary>
        /// 鏌ヨ璧勬簮鏄惁瀛樺湪
        /// </summary>
        public HasAssetResult HasAsset(string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new GameFrameworkException("Asset name is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");
            return _resourceHelper.HasAsset(assetName);
        }

        /// <summary>
        /// 鍔犺浇璧勬簮
        /// </summary>
        public void LoadAsset(string assetName, Type assetType, int priority,
                              LoadAssetCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new GameFrameworkException("Asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            _resourceHelper.LoadAsset(assetName, assetType, callbacks, userData);
        }

        /// <summary>
        /// 鍗歌浇璧勬簮
        /// </summary>
        public void UnloadAsset(object asset)
        {
            if (asset == null)
                throw new GameFrameworkException("Asset is invalid.");
            if (_resourceHelper == null)
            {
                GameFrameworkLog.Warning("The resource helper is null.");
                return;
            }
            _resourceHelper.Release(asset);
        }

        /// <summary>
        /// 鍔犺浇鍦烘櫙
        /// </summary>
        public void LoadScene(string sceneAssetName, int priority,
                              LoadSceneCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
                throw new GameFrameworkException("Scene asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load scene callbacks is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            _resourceHelper.LoadScene(sceneAssetName, callbacks, userData);
        }

        /// <summary>
        /// 鍗歌浇鍦烘櫙
        /// </summary>
        public void UnloadScene(string sceneAssetName,
                                UnloadSceneCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(sceneAssetName))
                throw new GameFrameworkException("Scene asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Unload scene callbacks is invalid.");
            _resourceHelper.UnloadScene(sceneAssetName, callbacks, userData);
        }

        /// <summary>
        /// 鍔犺浇浜岃繘鍒?鍘熷鏂囦欢
        /// </summary>
        public void LoadBinary(string binaryAssetName,
                               LoadBinaryCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(binaryAssetName))
                throw new GameFrameworkException("Binary asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load binary callbacks is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            _resourceHelper.LoadBinary(binaryAssetName, callbacks, userData);
        }

        /// <summary>
        /// 瀹炰緥鍖栬祫婧?
        /// </summary>
        public void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new GameFrameworkException("Asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            _resourceHelper.InstantiateAsset(assetName, callbacks, userData);
        }

        /// <summary>
        /// 娓告垙妗嗘灦妯″潡杞
        /// </summary>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            // Agent 灞傚凡绉婚櫎锛屾棤闇€澶勭悊闃熷垪
        }

        /// <summary>
        /// 鍏抽棴骞舵竻鐞嗚祫婧愮鐞嗗櫒
        /// </summary>
        internal override void Shutdown()
        {
            _resourceHelper = null;
        }
    }
}
