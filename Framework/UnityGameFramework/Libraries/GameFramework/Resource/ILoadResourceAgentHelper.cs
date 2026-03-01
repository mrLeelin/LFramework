using System;

namespace GameFramework.Resource
{
    /// <summary>
    /// 加载资源代理辅助器接口
    /// </summary>
    public interface ILoadResourceAgentHelper
    {
        /// <summary>
        /// 获取代理是否繁忙
        /// </summary>
        bool IsBusy { get; }

        /// <summary>
        /// 加载资源
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <param name="assetType">资源类型</param>
        /// <param name="callbacks">加载资源回调</param>
        /// <param name="userData">用户自定义数据</param>
        void LoadAsset(string assetName, Type assetType,
                       LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称</param>
        /// <param name="callbacks">加载场景回调</param>
        /// <param name="userData">用户自定义数据</param>
        void LoadScene(string sceneAssetName,
                       LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        /// <param name="binaryAssetName">二进制资源名称</param>
        /// <param name="callbacks">加载二进制回调</param>
        /// <param name="userData">用户自定义数据</param>
        void LoadBinary(string binaryAssetName,
                        LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 重置代理状态
        /// </summary>
        void Reset();
    }
}
