using System.Collections.Generic;
using System.Threading;
using LFramework.Runtime;

namespace LFramework.Runtime.LaunchPipeline
{
    /// <summary>
    /// 启动管线上下文基类。
    /// 在任务之间共享数据和状态，支持子类继承以携带项目特定数据。
    /// 镜像 Editor 侧的 <c>BuildPipelineContext</c>。
    /// </summary>
    public partial class LaunchContext
    {
        
        /// <summary>
        /// 当前运行的任务
        /// </summary>
        public ILaunchTask CurrentRunTask { get; internal set; }
        
        /// <summary>
        /// 自定义数据字典，用于任务之间传递额外数据。
        /// </summary>
        public Dictionary<string, object> CustomData { get; private set; }

        /// <summary>
        /// 取消令牌，用于支持启动流程的取消操作。
        /// </summary>
        public CancellationToken CancellationToken { get; set; }

        /// <summary>
        /// 进度报告器，任务内部通过此对象汇报进度，外部通过事件监听。
        /// </summary>
        public LaunchProgressReporter ProgressReporter { get; private set; }

        /// <summary>
        /// 版本检查结果，用于在版本检查任务和后续任务之间传递版本检查结果。
        /// </summary>
        public VersionCheckResult VersionCheckResult { get; set; }

        /// <summary>
        /// 默认重试次数（不包含第一次执行）。
        /// </summary>
        public int DefaultRetryCount { get; set; } = 3;

        /// <summary>
        /// 默认重试间隔（秒）。
        /// </summary>
        public float DefaultRetryDelaySeconds { get; set; } = 1f;

        /// <summary>
        /// 是否使用指数退避。
        /// </summary>
        public bool UseExponentialBackoff { get; set; } = true;

        /// <summary>
        /// 上一次失败任务名称。
        /// </summary>
        public string LastFailedTaskName { get; set; }

        /// <summary>
        /// 上一次失败错误信息。
        /// </summary>
        public string LastErrorMessage { get; set; }

        /// <summary>
        /// 上一次失败错误分类。
        /// </summary>
        public LaunchErrorCategory LastErrorCategory { get; set; } = LaunchErrorCategory.None;

        /// <summary>
        /// 已执行的重试次数。
        /// </summary>
        public int LastRetryCount { get; set; }

        /// <summary>
        /// 最近一次失败对应的尝试索引（从 0 开始）。
        /// </summary>
        public int LastAttemptIndex { get; set; }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="cancellationToken">可选的取消令牌。</param>
        public LaunchContext(CancellationToken cancellationToken = default)
        {
            CancellationToken = cancellationToken;
            CustomData = new Dictionary<string, object>();
            ProgressReporter = new LaunchProgressReporter();
            CurrentRunTask = null;
        }

        /// <summary>
        /// 设置自定义数据。如果键已存在则覆盖，否则新增。
        /// </summary>
        /// <param name="key">键。</param>
        /// <param name="value">值。</param>
        public void SetCustomData(string key, object value)
        {
            if (CustomData.ContainsKey(key))
            {
                CustomData[key] = value;
            }
            else
            {
                CustomData.Add(key, value);
            }
        }

        /// <summary>
        /// 获取自定义数据，类型安全。
        /// </summary>
        /// <typeparam name="T">数据类型。</typeparam>
        /// <param name="key">键。</param>
        /// <param name="defaultValue">当键不存在或类型不匹配时返回的默认值。</param>
        /// <returns>与键关联的值，或默认值。</returns>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }
        

        /// <summary>
        /// 检查是否包含指定键的自定义数据。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns><c>true</c> 表示包含，<c>false</c> 表示不包含。</returns>
        public bool ContainsCustomData(string key)
        {
            return CustomData.ContainsKey(key);
        }

        /// <summary>
        /// 移除指定键的自定义数据。
        /// </summary>
        /// <param name="key">键。</param>
        /// <returns><c>true</c> 表示移除成功，<c>false</c> 表示键不存在。</returns>
        public bool RemoveCustomData(string key)
        {
            return CustomData.Remove(key);
        }
        
        
    }
}
