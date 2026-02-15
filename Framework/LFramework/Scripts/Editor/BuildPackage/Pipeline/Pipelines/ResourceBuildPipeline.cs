using System.Collections.Generic;
using LFramework.Editor.Builder.Pipeline.Tasks;

namespace LFramework.Editor.Builder.Pipeline.Pipelines
{
    /// <summary>
    /// 资源构建管线
    /// 专门用于构建和更新游戏资源,不包含 APP 构建任务
    /// </summary>
    public class ResourceBuildPipeline : IBuildPipeline
    {
        /// <summary>
        /// 管线名称
        /// </summary>
        public string PipelineName => "Resource Build Pipeline";

        /// <summary>
        /// 管线描述
        /// </summary>
        public string Description => "Pipeline for building game resources only, without APP building";

        /// <summary>
        /// 获取管线中的所有任务
        /// </summary>
        /// <returns>任务列表</returns>
        public List<IBuildTask> GetTasks()
        {
            return new List<IBuildTask>
            {
                // 1. 构建 DLL
                new BuildDllTask(),

                // 2. 构建资源
                new BuildResourcesTask()
            };
        }

        /// <summary>
        /// 执行管线
        /// </summary>
        /// <param name="context">构建上下文</param>
        /// <returns>true 表示执行成功,false 表示执行失败</returns>
        public bool Execute(BuildPipelineContext context)
        {
            return BuildPipelineRunner.Run(this, context);
        }
    }
}
