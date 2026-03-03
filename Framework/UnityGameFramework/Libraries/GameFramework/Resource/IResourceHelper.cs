using System;

namespace GameFramework.Resource
{
    /// <summary>
    /// 资源辅助器接口（平台初始化 + 查询）
    /// </summary>
    public interface IResourceHelper
    {
        /// <summary>
        /// 初始化资源系统
        /// </summary>
        /// <param name="callback">初始化完成回调</param>
        void InitializeResources(ResourceInitCallBack callback);

        /// <summary>
        /// 检查资源是否存在
        /// </summary>
        /// <param name="assetName">资源名称</param>
        /// <returns>资源查询结果</returns>
        HasAssetResult HasAsset(string assetName);

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="asset">要释放的资源对象</param>
        void Release(object asset);

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="sceneAssetName">场景资源名称</param>
        /// <param name="callbacks">卸载场景回调</param>
        /// <param name="userData">用户自定义数据</param>
        void UnloadScene(string sceneAssetName, UnloadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载资源
        /// </summary>
        void LoadAsset(string assetName, Type assetType, LoadAssetCallbacks callbacks, object userData);

        /// <summary>
        /// 加载资源（V2 版本，返回 IResourceHandle）
        /// </summary>
        void LoadAssetV2(string assetName, Type assetType, LoadAssetCallbacksV2 callbacks, object userData);

        /// <summary>
        /// 加载场景
        /// </summary>
        void LoadScene(string sceneAssetName, LoadSceneCallbacks callbacks, object userData);

        /// <summary>
        /// 加载二进制/原始文件
        /// </summary>
        void LoadBinary(string binaryAssetName, LoadBinaryCallbacks callbacks, object userData);

        /// <summary>
        /// 实例化资源
        /// </summary>
        void InstantiateAsset(string assetName, LoadAssetCallbacks callbacks, object userData);
    }
}
