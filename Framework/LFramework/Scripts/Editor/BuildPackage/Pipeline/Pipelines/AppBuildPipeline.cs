using System.Collections.Generic;
using LFramework.Editor.Builder.Pipeline.Tasks;

namespace LFramework.Editor.Builder.Pipeline.Pipelines
{
    /// <summary>
    /// App 构建管线
    /// 专门用于构建应用程序,包含完整的 APP 构建流程
    /// </summary>
    public class AppBuildPipeline : IBuildPipeline
    {
        /// <summary>
        /// 管线名称
        /// </summary>
        public string PipelineName => "App Build Pipeline";

        /// <summary>
        /// 管线描述
        /// </summary>
        public string Description => "Pipeline for building application with all APP-related tasks";

        /// <summary>
        /// 获取管线中的所有任务
        /// </summary>
        /// <returns>任务列表</returns>
        public List<IBuildTask> GetTasks()
        {
            return new List<IBuildTask>
            {
                // 1. 创建构建目录
                new CreateDirectoryTask(),

                // 2. 预处理
                new PreprocessBuildTask(),

                // 3. 设置宏定义
                new SetScriptingDefineSymbolsTask(),

                // 4. 构建前处理
                new BuildBeforeTask(),

                // 5. 创建 BuildResourcesData（如果需要构建资源）
                new CreateBuildResourcesDataTask(),

                // 6. 构建 DLL（如果需要）
                new BuildDllTask(),

                // 7. 构建资源（如果需要）
                new BuildResourcesTask(),

                // 8. 构建游戏设置
                new BuildGameSettingTask(),

                // 9. 构建玩家
                new BuildPlayerTask(),

                // 10. 后处理
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
