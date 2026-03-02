using System;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源管理器接口
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// 获取资源只读路径
        /// </summary>
        string ReadOnlyPath { get; }

        /// <summary>
        /// 获取资源读写路径
        /// </summary>
        string ReadWritePath { get; }

        /// <summary>
        /// 获取资源模式
        /// </summary>
        ResourceMode ResourceMode { get; }

        /// <summary>
        /// 设置资源只读路径
        /// </summary>
        void SetReadOnlyPath(string readOnlyPath);

        /// <summary>
        /// 设置资源读写路径
        /// </summary>
        void SetReadWritePath(string readWritePath);

        /// <summary>
        /// 设置资源模式
        /// </summary>
        void SetResourceMode(ResourceMode resourceMode);

        /// <summary>
        /// 设置资源辅助器
        /// </summary>
        void SetResourceHelper(IResourceHelper resourceHelper);

        /// <summary>
        /// 初始化资源
        /// </summary>
        void InitResources(InitResourcesCompleteCallback callback);

        /// <summary>
        /// 查询资源是否存在
        /// </summary>
        HasAssetResult HasAsset(string assetName);

        /// <summary>
        /// 加载资源
        /// </summary>
        void LoadAsset(string assetName, Type assetType, int priority,
                       LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 卸载资源
        /// </summary>
        void UnloadAsset(object asset);

        /// <summary>
        /// 加载场景
        /// </summary>
        void LoadScene(string sceneAssetName, int priority,
                       LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 卸载场景
        /// </summary>
        void UnloadScene(string sceneAssetName,
                         UnloadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        void LoadBinary(string binaryAssetName,
                        LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 实例化资源
        /// </summary>
        void InstantiateAsset(string assetName, int priority,
                              LoadAssetCallbacks callbacks, object userData);
    }
}
