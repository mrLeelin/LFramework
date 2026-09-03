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
        private sealed class PrepareBuildEnvironmentTask : IBuildTask
        {
            public string TaskName => "Prepare Build Environment";
            public string Description => "Synchronize and validate HybridCLR settings before resource generation";

            public bool CanExecute(BuildPipelineContext context)
            {
                return context?.BuildSetting != null;
            }

            public BuildTaskResult Execute(BuildPipelineContext context)
            {
                if (context?.BuildSetting == null)
                {
                    return BuildTaskResult.CreateFailed(TaskName, "BuildSetting is null.");
                }

                try
                {
                    BuildEnvironmentConfigurator.Prepare(context);
                    return BuildTaskResult.CreateSuccess(TaskName);
                }
                catch (System.Exception exception)
                {
                    return BuildTaskResult.CreateFailed(
                        TaskName,
                        $"Failed to prepare the build environment: {exception.Message}");
                }
            }
        }

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
                // 1. 同步并校验 HybridCLR 构建环境
                new PrepareBuildEnvironmentTask(),

                // 2. 构建 DLL（如果需要）
                new BuildDllTask(),

                // 3. 构建资源
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
