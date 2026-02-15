namespace LFramework.Editor.Builder.Pipeline
{
    /// <summary>
    /// 构建任务接口
    /// 每个构建任务代表构建流程中的一个独立步骤
    /// </summary>
    public interface IBuildTask
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
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示可以执行,false 表示跳过</returns>
        bool CanExecute(BuildPipelineContext context);

        /// <summary>
        /// 执行任务
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>任务执行结果</returns>
        BuildTaskResult Execute(BuildPipelineContext context);
    }
}
