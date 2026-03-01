using GameFramework.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// Addressable 资源辅助器（平台初始化 + 查询）
    /// </summary>
    public class AddressableResourceHelper : ResourceHelperBase
    {
        /// <summary>
        /// 初始化资源系统
        /// </summary>
        public override void InitializeResources(ResourceInitCallBack callback)
        {
            var handle = Addressables.InitializeAsync();
            handle.Completed += (op) =>
            {
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callback.ResourceInitSuccessCallBack?.Invoke();
                }
                else
                {
                    callback.ResourceInitFailureCallBack?.Invoke(
                        op.OperationException?.Message ?? "Addressable initialization failed.");
                }
            };
        }

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        public override HasAssetResult HasAsset(string assetName)
        {
            return HasAssetResult.Exist;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Release(object asset)
        {
            if (asset is AsyncOperationHandle handle)
            {
                Addressables.Release(handle);
            }
            else if (asset is GameObject go)
            {
                Addressables.ReleaseInstance(go);
            }
            else
            {
                Addressables.Release(asset);
            }
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        public override void UnloadScene(string sceneAssetName,
            UnloadSceneCallbacks callbacks, object userData)
        {
            var op = SceneManager.UnloadSceneAsync(sceneAssetName);
            if (op == null)
            {
                callbacks.UnloadSceneFailureCallback?.Invoke(sceneAssetName, userData);
                return;
            }
            op.completed += (_) =>
            {
                callbacks.UnloadSceneSuccessCallback?.Invoke(sceneAssetName, userData);
            };
        }
    }
}
