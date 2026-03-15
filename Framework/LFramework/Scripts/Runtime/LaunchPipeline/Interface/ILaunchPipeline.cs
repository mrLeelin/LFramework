using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动管线接口
    /// 定义启动任务的组合和异步执行流程
    /// </summary>
    public interface ILaunchPipeline
    {
        /// <summary>
        /// 管线名称
        /// </summary>
        string PipelineName { get; }

        /// <summary>
        /// 管线描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 获取管线中的所有任务
        /// </summary>
        /// <returns>启动任务列表</returns>
        List<ILaunchTask> GetTasks();
        
    }
}
