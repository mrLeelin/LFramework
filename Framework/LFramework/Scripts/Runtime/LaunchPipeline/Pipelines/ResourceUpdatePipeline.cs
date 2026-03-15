using System.Collections.Generic;

namespace LFramework.Runtime.LaunchPipeline
{
    public class ResourceUpdatePipeline : ILaunchPipeline
    {
        public string PipelineName => nameof(ResourceUpdatePipeline);
        public string Description => "资源更新管线";

        public List<ILaunchTask> GetTasks()
        {
            return new List<ILaunchTask>()
            {
                new CheckVersionTask(),
                new InitResourceTask(),
                new DownloadResourceTask(),
                new LoadAssemblyTask(),
                new HotfixEntryTask()
            };
        }
    }
}