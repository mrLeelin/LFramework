using System.Collections.Generic;
using LFramework.Editor.Builder.Pipeline.Tasks;

namespace LFramework.Editor.Builder.Pipeline.Pipelines
{
    /// <summary>
    /// 默认构建管线
    /// 包含完整的构建流程,支持 APP 构建和资源构建
    /// </summary>
    public class DefaultBuildPipeline : IBuildPipeline
    {
        /// <summary>
        /// 管线名称
        /// </summary>
        public string PipelineName => "Default Build Pipeline";

        /// <summary>
        /// 管线描述
        /// </summary>
        public string Description => "Complete build pipeline with all tasks for APP and resource building";

        /// <summary>
        /// 获取管线中的所有任务
        /// </summary>
        /// <returns>任务列表</returns>
        public List<IBuildTask> GetTasks()
        {
            return new List<IBuildTask>
            {
                // 1. 创建构建目录 (仅 APP)
                new CreateDirectoryTask(),

                // 2. 预处理 (仅 APP)
                new PreprocessBuildTask(),

                // 3. 设置宏定义 (仅 APP)
                new SetScriptingDefineSymbolsTask(),

                // 4. 构建前处理 (仅 APP)
                new BuildBeforeTask(),

                // 5. 构建 DLL (需要构建资源时)
                new BuildDllTask(),

                // 6. 构建资源 (需要构建资源时)
                new BuildResourcesTask(),

                // 7. 构建游戏设置 (仅 APP)
                new BuildGameSettingTask(),

                // 8. 构建玩家 (仅 APP)
                new BuildPlayerTask(),

                // 9. 后处理 (仅 APP)
                new PostprocessBuildTask()
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
