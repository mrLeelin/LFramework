using Cysharp.Threading.Tasks;

namespace UnityGameFramework.Runtime
{
    /// <summary>
    /// 场景加载 Handle，用于 LoadSceneAsync
    /// </summary>
    public class ResourceSceneHandle : ResourceHandleBase
    {
        private UniTaskCompletionSource _tcs = new();

        /// <summary>
        /// 异步任务
        /// </summary>
        public UniTask Task => _tcs.Task;

        /// <summary>
        /// 场景名称
        /// </summary>
        public string SceneName { get; private set; }

        /// <summary>
        /// 支持直接 await handle
        /// </summary>
        public UniTask.Awaiter GetAwaiter() => Task.GetAwaiter();

        /// <summary>
        /// 默认构造函数（供 ReferencePool.Acquire 使用）
        /// </summary>
        public ResourceSceneHandle()
        {
        }

        /// <summary>
        /// 带场景名的构造函数（向后兼容）
        /// </summary>
        /// <param name="sceneName">场景名称</param>
        public ResourceSceneHandle(string sceneName)
        {
            SceneName = sceneName;
        }

        /// <summary>
        /// 设置场景名称（池化后重新赋值）
        /// </summary>
        public void SetSceneName(string sceneName) => SceneName = sceneName;

        /// <summary>
        /// 设置加载完成
        /// </summary>
        public void SetCompleted()
        {
            _isDone = true;
            _progress = 1f;
            _tcs.TrySetResult();
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
        /// 重置状态（由 ReferencePool 归还时调用）
        /// </summary>
        public override void Clear()
        {
            base.Clear();
            _tcs = new UniTaskCompletionSource();
            SceneName = null;
        }
    }
}
