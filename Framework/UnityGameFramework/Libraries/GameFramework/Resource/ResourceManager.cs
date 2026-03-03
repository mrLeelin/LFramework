using System;
using System.Collections.Generic;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源管理器（唯一实现）
    /// </summary>
    internal sealed class ResourceManager : GameFrameworkModule, IResourceManager
    {
        private string _readOnlyPath;
        private string _readWritePath;
        private ResourceMode _resourceMode;
        private IResourceHelper _resourceHelper;

        /// <summary>
        /// 初始化资源管理器
        /// </summary>
        public ResourceManager()
        {
            _readOnlyPath = null;
            _readWritePath = null;
            _resourceMode = ResourceMode.Unspecified;
            _resourceHelper = null;
        }

        /// <summary>
        /// 获取资源只读路径
        /// </summary>
        public string ReadOnlyPath => _readOnlyPath;

        /// <summary>
        /// 获取资源读写路径
        /// </summary>
        public string ReadWritePath => _readWritePath;

        /// <summary>
        /// 获取资源模式
        /// </summary>
        public ResourceMode ResourceMode => _resourceMode;

        /// <summary>
        /// 获取模块优先级
        /// </summary>
        internal override int Priority => 4;

        /// <summary>
        /// 设置资源只读路径
        /// </summary>
        public void SetReadOnlyPath(string readOnlyPath)
        {
            if (string.IsNullOrEmpty(readOnlyPath))
                throw new GameFrameworkException("Read-only path is invalid.");
            _readOnlyPath = readOnlyPath;
        }

        /// <summary>
        /// 设置资源读写路径
        /// </summary>
        public void SetReadWritePath(string readWritePath)
        {
            if (string.IsNullOrEmpty(readWritePath))
                throw new GameFrameworkException("Read-write path is invalid.");
            _readWritePath = readWritePath;
        }

        /// <summary>
        /// 设置资源模式
        /// </summary>
        public void SetResourceMode(ResourceMode resourceMode)
        {
            if (resourceMode == ResourceMode.Unspecified)
                throw new GameFrameworkException("Resource mode is invalid.");
            _resourceMode = resourceMode;
        }

        /// <summary>
        /// 设置资源辅助器
        /// </summary>
        public void SetResourceHelper(IResourceHelper resourceHelper)
        {
            _resourceHelper = resourceHelper ?? throw new GameFrameworkException("Resource helper is invalid.");
        }

        /// <summary>
        /// 初始化资源
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
        /// 查询资源是否存在
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
        /// 加载资源
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
        /// 加载资源（V2 版本，返回 IResourceHandle）
        /// </summary>
        public void LoadAssetV2(string assetName, Type assetType,
                                LoadAssetCallbacksV2 callbacks, object userData)
        {
            if (string.IsNullOrEmpty(assetName))
                throw new GameFrameworkException("Asset name is invalid.");
            if (callbacks == null)
                throw new GameFrameworkException("Load asset callbacks is invalid.");
            if (_resourceHelper == null)
                throw new GameFrameworkException("Resource helper is not set.");

            _resourceHelper.LoadAssetV2(assetName, assetType, callbacks, userData);
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        public void UnloadAsset(object asset)
        {
            if (asset == null)
                throw new GameFrameworkException("Asset is invalid.");
            _resourceHelper.Release(asset);
        }

        /// <summary>
        /// 加载场景
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
        /// 卸载场景
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
        /// 加载二进制/原始文件
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
        /// 实例化资源
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
        /// 游戏框架模块轮询
        /// </summary>
        internal override void Update(float elapseSeconds, float realElapseSeconds)
        {
            // Agent 层已移除，无需处理队列
        }

        /// <summary>
        /// 关闭并清理资源管理器
        /// </summary>
        internal override void Shutdown()
        {
            _resourceHelper = null;
        }
    }
}
