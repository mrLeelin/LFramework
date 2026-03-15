using System.Collections.Generic;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 资源辅助器基类
    /// </summary>
    public abstract class ResourceHelperBase : MonoBehaviour, GameFramework.Resource.IResourceHelper
    {
        /// <summary>
        /// 资源组件
        /// </summary>
        protected ResourceComponent ResourceComponent { get; private set; }

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
        public abstract void UnloadScene(string sceneAssetName, GameFramework.Resource.UnloadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载资源
        /// </summary>
        public abstract void LoadAsset(string assetName, System.Type assetType, GameFramework.Resource.LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 加载场景
        /// </summary>
        public abstract void LoadScene(string sceneAssetName, GameFramework.Resource.LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public abstract void LoadBinary(string binaryAssetName, GameFramework.Resource.LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 实例化资源
        /// </summary>
        public abstract void InstantiateAsset(string assetName, GameFramework.Resource.LoadAssetCallbacks callbacks, object userData);

        // ─── Handle 异步 API ───

        /// <summary>
        /// 异步加载资源（返回 Handle）
        /// </summary>
        public abstract ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName) where T : UnityEngine.Object;

        /// <summary>
        /// 异步实例化资源（返回 Handle）
        /// </summary>
        public abstract ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName);

        /// <summary>
        /// 异步加载场景（返回 Handle）
        /// </summary>
        public abstract ResourceSceneHandle LoadSceneHandle(string sceneAssetName);

        /// <summary>
        /// 异步加载二进制/原始文件（返回 Handle）
        /// </summary>
        public abstract ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName);

        /// <summary>
        /// 异步批量加载资源（通过标签，返回 Handle）
        /// </summary>
        public abstract ResourceBatchHandle<T> LoadAssetsByTagHandle<T>(string tag) where T : UnityEngine.Object;

        /// <summary>
        /// 设置资源组件引用
        /// </summary>
        public void SetResourceComponent(ResourceComponent resourceComponent)
        {
            ResourceComponent = resourceComponent;
            if (resourceComponent == null)
            {
                Log.Fatal("The ResourceComponent is null in 'ResourceHelperBase'.");
            }
        }
    }
}
