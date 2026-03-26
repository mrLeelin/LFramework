using System.Collections.Generic;

namespace LFramework.Runtime.LaunchPipeline
{
    public class ResourcesBuildInPipeline : ILaunchPipeline
    {
        public string PipelineName => nameof(ResourcesBuildInPipeline);
        public string Description => PipelineName;

        public List<ILaunchTask> GetTasks()
        {
            var list = new List<ILaunchTask>
            {
                new InitResourceTask(),
#if YOOASSET_SUPPORT
                new UpdateYooPackageManifestTask(),
#endif
                new LoadAssemblyTask(),
                new HotfixEntryTask()
            };

            return list;
        }
    }
}
