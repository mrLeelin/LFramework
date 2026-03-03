using System;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 资源句柄接口（统一 YooAsset 和 Addressable）
    /// </summary>
    public interface IResourceHandle : IDisposable
    {
        /// <summary>
        /// 资源对象
        /// </summary>
        object Asset { get; }

        /// <summary>
        /// 资源名称
        /// </summary>
        string AssetName { get; }

        /// <summary>
        /// 是否有效（未被释放）
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// 释放资源
        /// </summary>
        void Release();
    }
}
