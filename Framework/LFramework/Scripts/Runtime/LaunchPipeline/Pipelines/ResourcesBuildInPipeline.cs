using System.Collections.Generic;

namespace LFramework.Runtime.LaunchPipeline
{
    public class ResourcesBuildInPipeline : ILaunchPipeline
    {
        public string PipelineName => nameof(ResourcesBuildInPipeline);
        public string Description => PipelineName;

        public List<ILaunchTask> GetTasks()
        {
            return new List<ILaunchTask>
            {
                new InitResourceTask(),

                new LoadAssemblyTask(),
                
                new HotfixEntryTask()
            };
        }
    }
}