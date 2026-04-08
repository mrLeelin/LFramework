using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 工程侧正式配置入口，运行时应优先从该资源读取配置。
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectSettingSelector", menuName = "LFramework/Settings/ProjectSettingSelector")]
    public class ProjectSettingSelector : ScriptableObject
    {
        [Serializable]
        public class SettingEntry
        {
            [InlineEditor]
            public BaseSetting setting;
        }

        [Serializable]
        public class ComponentSettingEntry
        {
            [InlineEditor]
            public ComponentSetting setting;
        }

        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<SettingEntry> selectedSettings = new List<SettingEntry>();

        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<ComponentSettingEntry> selectedComponentSettings = new List<ComponentSettingEntry>();

        /// <summary>
        /// 获取指定类型的普通 Setting。
        /// </summary>
        public T GetSetting<T>() where T : BaseSetting
        {
            return selectedSettings
                .Select(entry => entry.setting)
                .OfType<T>()
                .FirstOrDefault();
        }

        /// <summary>
        /// 通过稳定标识获取普通 Setting。
        /// </summary>
        public BaseSetting GetSetting(string targetSettingId)
        {
            return selectedSettings
                .Select(entry => entry.setting)
                .FirstOrDefault(setting => setting != null && setting.SettingId == targetSettingId);
        }

        /// <summary>
        /// 设置指定类型的普通 Setting。
        /// </summary>
        public void SetSetting<T>(T setting) where T : BaseSetting
        {
            if (setting == null)
            {
                return;
            }

            var entry = selectedSettings.FirstOrDefault(item =>
                item.setting != null && item.setting.GetType() == typeof(T));

            if (entry != null)
            {
                entry.setting = setting;
                return;
            }

            selectedSettings.Add(new SettingEntry { setting = setting });
        }

        /// <summary>
        /// 获取所有普通 Setting。
        /// </summary>
        public List<BaseSetting> GetAllSettings()
        {
            return selectedSettings.Select(entry => entry.setting).Where(setting => setting != null).ToList();
        }

        /// <summary>
        /// 获取指定类型的组件 Setting。
        /// </summary>
        public T GetComponentSetting<T>() where T : ComponentSetting
        {
            return selectedComponentSettings
                .Select(entry => entry.setting)
                .OfType<T>()
                .FirstOrDefault();
        }

        /// <summary>
        /// 通过稳定标识获取组件 Setting。
        /// </summary>
        public ComponentSetting GetComponentSetting(string targetSettingId)
        {
            return selectedComponentSettings
                .Select(entry => entry.setting)
                .FirstOrDefault(setting => setting != null && setting.SettingId == targetSettingId);
        }

        /// <summary>
        /// 通过绑定类型名获取组件 Setting。
        /// </summary>
        public ComponentSetting GetComponentSettingByBindTypeName(string targetBindTypeName)
        {
            return selectedComponentSettings
                .Select(entry => entry.setting)
                .FirstOrDefault(setting => setting != null && setting.bindTypeName == targetBindTypeName);
        }

        /// <summary>
        /// 设置组件 Setting。
        /// </summary>
        public void SetComponentSetting(ComponentSetting setting)
        {
            if (setting == null)
            {
                return;
            }

            var entry = selectedComponentSettings.FirstOrDefault(item =>
                item.setting != null && item.setting.GetType() == setting.GetType());

            if (entry != null)
            {
                entry.setting = setting;
                return;
            }

            selectedComponentSettings.Add(new ComponentSettingEntry { setting = setting });
        }

        /// <summary>
        /// 获取所有组件 Setting。
        /// </summary>
        public List<ComponentSetting> GetAllComponentSettings()
        {
            return selectedComponentSettings.Select(entry => entry.setting).Where(setting => setting != null).ToList();
        }
    }
}
