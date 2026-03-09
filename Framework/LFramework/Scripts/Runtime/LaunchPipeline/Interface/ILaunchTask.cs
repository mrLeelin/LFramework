using Cysharp.Threading.Tasks;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动任务接口
    /// 每个启动任务代表启动流程中的一个独立异步步骤
    /// </summary>
    public interface ILaunchTask
    {
        /// <summary>
        /// 任务名称
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// 任务描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 判断任务是否可以执行
        /// </summary>
        /// <param name="context">启动管线上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        bool CanExecute(LaunchContext context);

        /// <summary>
        /// 异步执行任务
        /// </summary>
        /// <param name="context">启动管线上下文</param>
        /// <returns>任务执行结果</returns>
        UniTask<LaunchTaskResult> ExecuteAsync(LaunchContext context);
    }
}
