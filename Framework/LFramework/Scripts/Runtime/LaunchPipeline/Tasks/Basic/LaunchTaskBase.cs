using Cysharp.Threading.Tasks;

namespace LFramework.Runtime.LaunchPipeline.Basic
{
    public abstract class LaunchTaskBase : ILaunchTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        public abstract string TaskName { get; }
        /// <summary>
        /// 任务描述
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 判断任务是否可以执行。版本检查任务始终可以执行。
        /// </summary>
        /// <param name="context">启动管线上下文。</param>
        /// <returns>始终返回 <c>true</c>。</returns>
        public virtual bool CanExecute(LaunchContext context)
        {
            return true;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <returns>任务执行结果。</returns>
        public abstract UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context);

        /// <summary>
        /// 执行任务之前调用
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnTaskStarted(LaunchContext context)
        {
        }

        /// <summary>
        /// 执行任务之后调用
        /// </summary>
        /// <param name="context"></param>
        public virtual void OnTaskEnded(LaunchContext context)
        {
        }
    }
}