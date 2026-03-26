namespace LFramework.Runtime.LaunchPipeline
{
    public delegate void OnTaskStarted(ILaunchTask launchTask);

    public delegate void OnTaskEnded(ILaunchTask launchTask);
    
    public partial class LaunchContext
    {
        /// <summary>
        /// 任务开始运行
        /// </summary>
        public OnTaskStarted OnTaskStarted;

        /// <summary>
        /// 任务结束运行
        /// </summary>
        public OnTaskEnded OnTaskEnded;


    }
}