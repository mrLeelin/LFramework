using Cysharp.Threading.Tasks;
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
        /// 加载资源（指定逻辑包）
        /// </summary>
        public virtual void LoadAsset(string assetName, System.Type assetType, string packageId,
            GameFramework.Resource.LoadAssetCallbacks callbacks, object userData)
        {
            LoadAsset(assetName, assetType, callbacks, userData);
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public abstract void LoadScene(string sceneAssetName, GameFramework.Resource.LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载场景（指定逻辑包）
        /// </summary>
        public virtual void LoadScene(string sceneAssetName, string packageId,
            GameFramework.Resource.LoadSceneCallbacks callbacks, object userData)
        {
            LoadScene(sceneAssetName, callbacks, userData);
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public abstract void LoadBinary(string binaryAssetName, GameFramework.Resource.LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 加载二进制/原始文件（指定逻辑包）
        /// </summary>
        public virtual void LoadBinary(string binaryAssetName, string packageId,
            GameFramework.Resource.LoadBinaryCallbacks callbacks, object userData)
        {
            LoadBinary(binaryAssetName, callbacks, userData);
        }

        /// <summary>
        /// 实例化资源
        /// </summary>
        public abstract void InstantiateAsset(string assetName, GameFramework.Resource.LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 实例化资源（指定逻辑包）
        /// </summary>
        public virtual void InstantiateAsset(string assetName, string packageId,
            GameFramework.Resource.LoadAssetCallbacks callbacks, object userData)
        {
            InstantiateAsset(assetName, callbacks, userData);
        }

        /// <summary>
        /// 异步加载资源（返回 Handle）
        /// </summary>
        public abstract ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName) where T : UnityEngine.Object;

        public virtual ResourceAssetHandle<T> LoadAssetHandle<T>(string assetName, string packageId) where T : UnityEngine.Object
        {
            return LoadAssetHandle<T>(assetName);
        }

        /// <summary>
        /// 异步实例化资源（返回 Handle）
        /// </summary>
        public abstract ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName);

        public virtual ResourceAssetHandle<GameObject> InstantiateAssetHandle(string assetName, string packageId)
        {
            return InstantiateAssetHandle(assetName);
        }

        /// <summary>
        /// 异步加载场景（返回 Handle）
        /// </summary>
        public abstract ResourceSceneHandle LoadSceneHandle(string sceneAssetName);

        public virtual ResourceSceneHandle LoadSceneHandle(string sceneAssetName, string packageId)
        {
            return LoadSceneHandle(sceneAssetName);
        }

        /// <summary>
        /// 异步加载二进制/原始文件（返回 Handle）
        /// </summary>
        public abstract ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName);

        public virtual ResourceRawFileHandle LoadRawFileHandle(string binaryAssetName, string packageId)
        {
            return LoadRawFileHandle(binaryAssetName);
        }

        /// <summary>
        /// 异步批量加载资源（通过标签，返回 Handle）
        /// </summary>
        public abstract ResourceBatchHandle<T> LoadAssetsByTagHandle<T>(string tag) where T : UnityEngine.Object;

        /// <summary>
        /// 刷新当前路由索引。默认对非 YooAsset 实现无操作。
        /// </summary>
        public virtual UniTask RefreshRouteIndexAsync()
        {
            return UniTask.CompletedTask;
        }

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
