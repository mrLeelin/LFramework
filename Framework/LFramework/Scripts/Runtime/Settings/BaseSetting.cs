using UnityEngine;
using System.Collections.Generic;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Setting 基类，所有游戏配置的抽象基类
    /// </summary>
    public abstract class BaseSetting : ScriptableObject
    {
        [SerializeField] protected string displayName;
        [SerializeField, TextArea] protected string description;
        [SerializeField] protected List<string> tags = new List<string>();

        /// <summary>
        /// 显示名称
        /// </summary>
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>
        /// Setting 类型名称
        /// </summary>
        public virtual string SettingTypeName => GetType().Name;

        /// <summary>
        /// 验证配置是否有效
        /// </summary>
        public virtual bool Validate(out string errorMessage)
        {
            errorMessage = null;
            return true;
        }

        /// <summary>
        /// 应用配置
        /// </summary>
        public virtual void Apply()
        {
            // 子类实现
        }
    }
}
