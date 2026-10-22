using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 批量资源加载 Handle，用于 LoadAssetsByTagAsync
    /// </summary>
    public class ResourceBatchHandle<T> : ResourceHandleBase where T : UnityEngine.Object
    {
        private UniTaskCompletionSource<List<T>> _tcs = new();

        /// <summary>
        /// 异步任务
        /// </summary>
        public UniTask<List<T>> Task => _tcs.Task;

        /// <summary>
        /// 加载结果
        /// </summary>
        public List<T> Result { get; private set; }

        /// <summary>
        /// 支持直接 await handle
        /// </summary>
        public UniTask<List<T>>.Awaiter GetAwaiter() => Task.GetAwaiter();

        /// <summary>
        /// 设置加载结果
        /// </summary>
        public void SetResult(List<T> result)
        {
            if (!TryBeginCompletion())
            {
                return;
            }

            Result = result;
            _progress = 1f;
            try
            {
                _tcs.TrySetResult(result);
            }
            finally
            {
                EndCompletion();
            }
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
        /// 取消尚未完成的等待者
        /// </summary>
        protected override void CancelWaiterInternal()
        {
            _tcs.TrySetCanceled();
        }

        /// <summary>
        /// 清除已完成的加载结果
        /// </summary>
        protected override void ReleaseResultInternal()
        {
            Result = null;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
        }

        /// <summary>
        /// 重置状态（由 ReferencePool 归还时调用）
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            _tcs = new UniTaskCompletionSource<List<T>>();
            Result = null;
        }
    }
}
