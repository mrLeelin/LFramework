using YooAsset;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// YooAsset 资源句柄包装
    /// </summary>
    public class YooAssetResourceHandle : IResourceHandle
    {
        private AssetHandle _yooHandle;
        private bool _disposed;

        public object Asset => _yooHandle?.AssetObject;
        public string AssetName { get; }
        public bool IsValid => _yooHandle != null && !_disposed;

        internal AssetHandle InternalHandle => _yooHandle;

        public YooAssetResourceHandle(AssetHandle handle, string assetName)
        {
            _yooHandle = handle;
            AssetName = assetName;
        }

        public void Release()
        {
            if (_disposed) return;

            _yooHandle?.Release();
            _yooHandle = null;
            _disposed = true;
        }

        public void Dispose()
        {
            Release();
        }
    }
}
