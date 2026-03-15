using Cysharp.Threading.Tasks;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 二进制/原始文件加载 Handle，用于 LoadBinaryAsync
    /// </summary>
    public class ResourceRawFileHandle : ResourceHandleBase
    {
        private UniTaskCompletionSource<byte[]> _tcs = new();

        /// <summary>
        /// 异步任务
        /// </summary>
        public UniTask<byte[]> Task => _tcs.Task;

        /// <summary>
        /// 加载结果
        /// </summary>
        public byte[] Result { get; private set; }

        /// <summary>
        /// 支持直接 await handle
        /// </summary>
        public UniTask<byte[]>.Awaiter GetAwaiter() => Task.GetAwaiter();

        /// <summary>
        /// 设置加载结果
        /// </summary>
        public void SetResult(byte[] result)
        {
            Result = result;
            _isDone = true;
            _progress = 1f;
            _tcs.TrySetResult(result);
        }

        /// <summary>
        /// 子类实现的错误处理逻辑
        /// </summary>
        protected override void SetErrorInternal(string errorMessage)
        {
            _tcs.TrySetException(
                new GameFramework.GameFrameworkException(errorMessage));
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            if (_isDisposed) return;
            Result = null;
            base.Dispose();
        }

        /// <summary>
        /// 重置状态（由 ReferencePool 归还时调用）
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            _tcs = new UniTaskCompletionSource<byte[]>();
            Result = null;
        }
    }
}
