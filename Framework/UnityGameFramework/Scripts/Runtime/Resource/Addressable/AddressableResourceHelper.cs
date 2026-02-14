using System.Collections;
using System.Collections.Generic;
using GameFramework.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace UnityGameFramework.Runtime
{
    public class AddressableResourceHelper : ResourceHelperBase
    {
        public override void LoadBytes(string fileUri, LoadBytesCallbacks loadBytesCallbacks, object userData)
        {
            throw new System.NotImplementedException();
        }

        public override void UnloadScene(string sceneAssetName, UnloadSceneCallbacks unloadSceneCallbacks,
            object userData)
        {
            var sceneInstance = (SceneInstance)userData;
            Addressables.UnloadSceneAsync(sceneInstance, true).Completed += (x) =>
            {
                if (x.Status == AsyncOperationStatus.Succeeded)
                {
                    unloadSceneCallbacks.UnloadSceneSuccessCallback(sceneAssetName, userData);
                }
                else
                {
                    unloadSceneCallbacks.UnloadSceneFailureCallback(sceneAssetName, x.OperationException.Message);
                }
            };
        }

        public override void Release(object objectToRelease)
        {
            if (objectToRelease is AsyncOperationHandle asyncOperationHandle)
            {
                Addressables.Release(asyncOperationHandle);
                return;
            }

            if (objectToRelease is GameObject go)
            {
                if (!Addressables.ReleaseInstance(go))
                {
                    Log.Error("Release instance failed");
                }

                return;
            }

            if (objectToRelease is AsyncOperationHandle<GameObject> asyncOperation)
            {
                if (!Addressables.ReleaseInstance(asyncOperation))
                {
                    Log.Error("Release instance failed");
                }

                return;
            }

            Addressables.Release(objectToRelease);
        }

        public override void InitializeResources(ResourceInitCallBack resourceInitCallBack)
        {
            Addressables.InitializeAsync(true).Completed += (x) =>
            {
                if (x.Status == AsyncOperationStatus.Succeeded)
                {
                    resourceInitCallBack.ResourceInitSuccessCallBack();
                }
                else
                {
                    resourceInitCallBack.ResourceInitFailureCallBack(x.OperationException.Message);
                }
            };
        }

        public override HasAssetResult HasAssets(string fileUrl)
        {
            return HasAssetResult.Addressable;
        }
    }
}