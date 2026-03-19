using System;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动管线进度信息。
    /// </summary>
    public struct LaunchProgressInfo
    {
        /// <summary>
        /// 当前任务索引（从 0 开始）。
        /// </summary>
        public int TaskIndex;

        /// <summary>
        /// 任务总数。
        /// </summary>
        public int TotalTasks;

        /// <summary>
        /// 当前任务名称。
        /// </summary>
        public string TaskName;

        /// <summary>
        /// 当前任务内部进度（0~1）。
        /// </summary>
        public float TaskProgress;

        /// <summary>
        /// 当前任务的进度描述文本。
        /// </summary>
        public string Message;

        /// <summary>
        /// 管线整体进度（0~1），综合任务索引和任务内部进度计算。
        /// </summary>
        public float TotalProgress
        {
            get
            {
                if (TotalTasks <= 0) return 0f;
                return (TaskIndex + TaskProgress) / TotalTasks;
            }
        }
    }

    /// <summary>
    /// 启动管线进度报告器。
    /// 任务内部通过此对象汇报进度，外部（UI）通过 OnProgress 事件监听。
    /// </summary>
    public class LaunchProgressReporter
    {
        /// <summary>
        /// 进度变化事件。
        /// </summary>
        public event Action<LaunchProgressInfo> OnProgress;

        private LaunchProgressInfo _current;

        /// <summary>
        /// 当前进度信息（只读快照）。
        /// </summary>
        public LaunchProgressInfo Current => _current;

        /// <summary>
        /// 由 Runner 调用，设置当前正在执行的任务。
        /// </summary>
        internal void SetCurrentTask(int taskIndex, int totalTasks, string taskName)
        {
            _current.TaskIndex = taskIndex;
            _current.TotalTasks = totalTasks;
            _current.TaskName = taskName;
            _current.TaskProgress = 0f;
            _current.Message = taskName;
            OnProgress?.Invoke(_current);
        }

        /// <summary>
        /// 由任务内部调用，汇报任务内部进度。
        /// </summary>
        /// <param name="progress">进度值（0~1）。</param>
        /// <param name="message">可选的进度描述。</param>
        public void ReportProgress(float progress, string message = null)
        {
            _current.TaskProgress = Math.Max(0f, Math.Min(1f, progress));
            if (message != null)
                _current.Message = message;
            OnProgress?.Invoke(_current);
        }

        /// <summary>
        /// 由 Runner 调用，标记当前任务完成。
        /// </summary>
        internal void CompleteCurrentTask()
        {
            _current.TaskProgress = 1f;
            OnProgress?.Invoke(_current);
        }
    }
}
