using System.Collections.Generic;
using LFramework.Editor.Builder.Builder;
using UnityEditor;

namespace LFramework.Editor.Builder.Pipeline
{
    /// <summary>
    /// 构建管线上下文
    /// 用于在任务之间共享数据和状态
    /// </summary>
    public class BuildPipelineContext
    {
        /// <summary>
        /// 构建设置
        /// </summary>
        public BuildSetting BuildSetting { get; private set; }

        /// <summary>
        /// 构建资源数据
        /// </summary>
        public BuildResourcesData BuildResourcesData { get; set; }

        /// <summary>
        /// 构建事件处理器列表
        /// </summary>
        public List<IBuildEventHandler> EventHandlers { get; private set; }

        /// <summary>
        /// 当前构建器引用
        /// </summary>
        public BaseBuilder Builder { get; private set; }

        /// <summary>
        /// 构建目标平台
        /// </summary>
        public BuildTarget BuildTarget { get; set; }

        /// <summary>
        /// 构建目标组
        /// </summary>
        public BuildTargetGroup BuildTargetGroup { get; set; }

        /// <summary>
        /// 构建输出目录
        /// </summary>
        public string OutputFolder { get; set; }

        /// <summary>
        /// 自定义数据字典(用于任务间传递额外数据)
        /// </summary>
        public Dictionary<string, object> CustomData { get; private set; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="buildSetting">构建设置</param>
        /// <param name="eventHandlers">事件处理器列表</param>
        /// <param name="builder">构建器引用</param>
        public BuildPipelineContext(BuildSetting buildSetting, List<IBuildEventHandler> eventHandlers, BaseBuilder builder)
        {
            BuildSetting = buildSetting;
            EventHandlers = eventHandlers ?? new List<IBuildEventHandler>();
            Builder = builder;
            CustomData = new Dictionary<string, object>();
        }

        /// <summary>
        /// 设置自定义数据
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
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
        /// 获取自定义数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        public T GetCustomData<T>(string key, T defaultValue = default)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// 检查是否包含自定义数据
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>true 表示包含,false 表示不包含</returns>
        public bool ContainsCustomData(string key)
        {
            return CustomData.ContainsKey(key);
        }

        /// <summary>
        /// 移除自定义数据
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>true 表示移除成功,false 表示键不存在</returns>
        public bool RemoveCustomData(string key)
        {
            return CustomData.Remove(key);
        }

        /// <summary>
        /// 清空所有自定义数据
        /// </summary>
        public void ClearCustomData()
        {
            CustomData.Clear();
        }
    }
}
