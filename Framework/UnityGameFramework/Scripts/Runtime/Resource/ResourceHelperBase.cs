using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 资源辅助器基类
    /// </summary>
    public abstract class ResourceHelperBase : MonoBehaviour, GameFramework.Resource.IResourceHelper
    {
        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public abstract void InitializeResources(GameFramework.Resource.ResourceInitCallBack callback);

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public abstract GameFramework.Resource.HasAssetResult HasAsset(string assetName);

        /// <summary>
        /// 释放资源
        /// </summary>
        public abstract void Release(object asset);

        /// <summary>
        /// 卸载场景
        /// </summary>
        public abstract void UnloadScene(string sceneAssetName,
            GameFramework.Resource.UnloadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载资源
        /// </summary>
        public abstract void LoadAsset(string assetName, System.Type assetType,
            GameFramework.Resource.LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 加载场景
        /// </summary>
        public abstract void LoadScene(string sceneAssetName,
            GameFramework.Resource.LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public abstract void LoadBinary(string binaryAssetName,
            GameFramework.Resource.LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 实例化资源
        /// </summary>
        public abstract void InstantiateAsset(string assetName,
            GameFramework.Resource.LoadAssetCallbacks callbacks, object userData);
    }
}
