using System.Collections.Generic;

namespace LFramework.Editor.Builder.Pipeline
{
    /// <summary>
    /// 构建管线接口
    /// 定义构建任务的执行流程
    /// </summary>
    public interface IBuildPipeline
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
        /// <returns>任务列表</returns>
        List<IBuildTask> GetTasks();

        /// <summary>
        /// 执行管线
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示执行成功,false 表示执行失败</returns>
        bool Execute(BuildPipelineContext context);
    }
}
