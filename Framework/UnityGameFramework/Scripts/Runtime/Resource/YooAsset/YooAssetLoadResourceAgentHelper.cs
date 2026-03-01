using System;
using GameFramework.Resource;
using UnityEngine;
using YooAsset;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// YooAsset 加载资源代理辅助器
    /// </summary>
    public class YooAssetLoadResourceAgentHelper : LoadResourceAgentHelperBase
    {
        private bool _isBusy;

        /// <summary>
        /// 获取代理是否繁忙
        /// </summary>
        public override bool IsBusy => _isBusy;

        /// <summary>
        /// YooAsset 包名称（通过 ResourceComponentSetting 配置）
        /// </summary>
        public string PackageName { get; set; } = "DefaultPackage";

        /// <summary>
        /// 加载资源
        /// </summary>
        public override void LoadAsset(string assetName, Type assetType,
            LoadAssetCallbacks callbacks, object userData)
        {
            _isBusy = true;
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadAssetAsync(assetName, assetType);
            handle.Completed += (op) =>
            {
                _isBusy = false;
                if (op.Status == EOperationStatus.Succeed)
                {
                    callbacks.LoadAssetSuccessCallback?.Invoke(
                        assetName, op.AssetObject, 0f, userData);
                }
                else
                {
                    callbacks.LoadAssetFailureCallback?.Invoke(
                        assetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load failed.", userData);
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
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadSceneAsync(sceneAssetName);
            handle.Completed += (op) =>
            {
                _isBusy = false;
                if (op.Status == EOperationStatus.Succeed)
                {
                    callbacks.LoadSceneSuccessCallback?.Invoke(
                        sceneAssetName, 0f, userData);
                }
                else
                {
                    callbacks.LoadSceneFailureCallback?.Invoke(
                        sceneAssetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load scene failed.", userData);
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
            var package = YooAssets.GetPackage(PackageName);
            var handle = package.LoadRawFileAsync(binaryAssetName);
            handle.Completed += (op) =>
            {
                _isBusy = false;
                if (op.Status == EOperationStatus.Succeed)
                {
                    callbacks.LoadBinarySuccessCallback?.Invoke(
                        binaryAssetName, op.GetRawFileData(), 0f, userData);
                }
                else
                {
                    callbacks.LoadBinaryFailureCallback?.Invoke(
                        binaryAssetName, LoadResourceStatus.NotExist,
                        op.LastError ?? "Load binary failed.", userData);
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
