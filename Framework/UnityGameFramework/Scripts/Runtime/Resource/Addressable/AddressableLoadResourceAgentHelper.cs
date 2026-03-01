using System;
using GameFramework.Resource;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// Addressable 加载资源代理辅助器
    /// </summary>
    public class AddressableLoadResourceAgentHelper : LoadResourceAgentHelperBase
    {
        private bool _isBusy;

        /// <summary>
        /// 获取代理是否繁忙
        /// </summary>
        public override bool IsBusy => _isBusy;

        /// <summary>
        /// 加载资源
        /// </summary>
        public override void LoadAsset(string assetName, Type assetType,
            LoadAssetCallbacks callbacks, object userData)
        {
            _isBusy = true;
            var handle = Addressables.LoadAssetAsync<UnityEngine.Object>(assetName);
            handle.Completed += (op) =>
            {
                _isBusy = false;
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, op.Result, 0f, userData);
                }
                else
                {
                    callbacks.LoadAssetFailureCallback?.Invoke(
                        assetName, LoadResourceStatus.NotExist,
                        op.OperationException?.Message ?? "Load failed.", userData);
                }
            };
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        public override void LoadScene(string sceneAssetName,
            LoadSceneCallbacks callbacks, object userData)
        {
            _isBusy = true;
            var handle = Addressables.LoadSceneAsync(sceneAssetName);
            handle.Completed += (op) =>
            {
                _isBusy = false;
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadSceneSuccessCallback?.Invoke(
                        sceneAssetName, 0f, userData);
                }
                else
                {
                    callbacks.LoadSceneFailureCallback?.Invoke(
                        sceneAssetName, LoadResourceStatus.NotExist,
                        op.OperationException?.Message ?? "Load scene failed.", userData);
                }
            };
        }

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        public override void LoadBinary(string binaryAssetName,
            LoadBinaryCallbacks callbacks, object userData)
        {
            _isBusy = true;
            var handle = Addressables.LoadAssetAsync<TextAsset>(binaryAssetName);
            handle.Completed += (op) =>
            {
                _isBusy = false;
                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    callbacks.LoadBinarySuccessCallback?.Invoke(
                        binaryAssetName, op.Result.bytes, 0f, userData);
                }
                else
                {
                    callbacks.LoadBinaryFailureCallback?.Invoke(
                        binaryAssetName, LoadResourceStatus.NotExist,
                        op.OperationException?.Message ?? "Load binary failed.", userData);
                }
            };
        }

        /// <summary>
        /// 重置代理状态
        /// </summary>
        public override void Reset()
        {
            _isBusy = false;
        }
    }
}
