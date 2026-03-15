using Cysharp.Threading.Tasks;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 单资源加载 Handle，用于 LoadAssetAsync / InstantiateAssetAsync
    /// </summary>
    public class ResourceAssetHandle<T> : ResourceHandleBase where T : UnityEngine.Object
    {
        private UniTaskCompletionSource<T> _tcs = new();

        /// <summary>
        /// 异步任务
        /// </summary>
        public UniTask<T> Task => _tcs.Task;

        /// <summary>
        /// 加载结果
        /// </summary>
        public T Result { get; private set; }

        /// <summary>
        /// 支持直接 await handle（返回 T），与 Addressable AsyncOperationHandle 行为一致
        /// </summary>
        public UniTask<T>.Awaiter GetAwaiter() => Task.GetAwaiter();

        /// <summary>
        /// 设置加载结果
        /// </summary>
        public void SetResult(T result)
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
            _tcs = new UniTaskCompletionSource<T>();
            Result = null;
        }
    }
}
