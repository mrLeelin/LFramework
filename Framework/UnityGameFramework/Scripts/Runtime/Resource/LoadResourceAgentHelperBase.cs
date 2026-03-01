using System;
using UnityEngine;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 加载资源代理辅助器基类
    /// </summary>
    public abstract class LoadResourceAgentHelperBase : MonoBehaviour, GameFramework.Resource.ILoadResourceAgentHelper
    {
        /// <summary>
        /// 获取代理是否繁忙
        /// </summary>
        public abstract bool IsBusy { get; }

        /// <summary>
        /// 加载资源
        /// </summary>
        public abstract void LoadAsset(string assetName, Type assetType,
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
        /// 重置代理状态
        /// </summary>
        public abstract void Reset();
    }
}
