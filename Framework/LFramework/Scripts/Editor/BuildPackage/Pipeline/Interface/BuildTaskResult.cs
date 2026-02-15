namespace LFramework.Editor.Builder.Pipeline
{
    /// <summary>
    /// 构建任务执行结果状态
    /// </summary>
    public enum BuildTaskStatus
    {
        /// <summary>
        /// 执行成功
        /// </summary>
        Success,

        /// <summary>
        /// 执行失败
        /// </summary>
        Failed,

        /// <summary>
        /// 跳过执行
        /// </summary>
        Skipped
    }

    /// <summary>
    /// 构建任务执行结果
    /// </summary>
    public class BuildTaskResult
    {
        /// <summary>
        /// 任务执行状态
        /// </summary>
        public BuildTaskStatus Status { get; private set; }

        /// <summary>
        /// 错误信息(仅在 Failed 状态时有效)
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// 任务名称
        /// </summary>
        public string TaskName { get; private set; }

        /// <summary>
        /// 是否成功
        /// </summary>
        public bool IsSuccess => Status == BuildTaskStatus.Success;

        /// <summary>
        /// 是否失败
        /// </summary>
        public bool IsFailed => Status == BuildTaskStatus.Failed;

        /// <summary>
        /// 是否跳过
        /// </summary>
        public bool IsSkipped => Status == BuildTaskStatus.Skipped;

        private BuildTaskResult(BuildTaskStatus status, string taskName, string errorMessage = null)
        {
            Status = status;
            TaskName = taskName;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <returns>成功结果</returns>
        public static BuildTaskResult CreateSuccess(string taskName)
        {
            return new BuildTaskResult(BuildTaskStatus.Success, taskName);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <param name="errorMessage">错误信息</param>
        /// <returns>失败结果</returns>
        public static BuildTaskResult CreateFailed(string taskName, string errorMessage)
        {
            return new BuildTaskResult(BuildTaskStatus.Failed, taskName, errorMessage);
        }

        /// <summary>
        /// 创建跳过结果
        /// </summary>
        /// <param name="taskName">任务名称</param>
        /// <returns>跳过结果</returns>
        public static BuildTaskResult CreateSkipped(string taskName)
        {
            return new BuildTaskResult(BuildTaskStatus.Skipped, taskName);
        }

        /// <summary>
        /// 获取结果描述
        /// </summary>
        /// <returns>结果描述字符串</returns>
        public override string ToString()
        {
            if (IsFailed)
            {
                return $"[{TaskName}] Failed: {ErrorMessage}";
            }

            return $"[{TaskName}] {Status}";
        }
    }
}
