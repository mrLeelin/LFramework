using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Sirenix.OdinInspector;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Setting 选择器，存储当前选择的各种 Setting
    /// </summary>
    [CreateAssetMenu(fileName = "SettingSelector", menuName = "LFramework/Settings/SettingSelector")]
    public class SettingSelector : ScriptableObject
    {
        [Serializable]
        public class SettingEntry
        {
            [InlineEditor]
            public BaseSetting setting;     // 选择的 Setting 实例
        }

        [SerializeField, ListDrawerSettings(ShowIndexLabels = true)]
        private List<SettingEntry> selectedSettings = new List<SettingEntry>();

        /// <summary>
        /// 获取指定类型的 Setting
        /// </summary>
        public T GetSetting<T>() where T : BaseSetting
        {
            var typeName = typeof(T).Name;
            var entry = selectedSettings.FirstOrDefault(e => e.setting.GetType().Name == typeName);
            return entry?.setting as T;
        }

        /// <summary>
        /// 设置指定类型的 Setting
        /// </summary>
        public void SetSetting<T>(T setting) where T : BaseSetting
        {
            var typeName = typeof(T).Name;
            var entry = selectedSettings.FirstOrDefault(e => e.setting.GetType().Name == typeName);

            if (entry != null)
            {
                entry.setting = setting;
            }
            else
            {
                selectedSettings.Add(new SettingEntry
                {
                    setting = setting
                });
            }
        }

        /// <summary>
        /// 获取所有选择的 Setting
        /// </summary>
        public List<BaseSetting> GetAllSettings()
        {
            return selectedSettings.Select(e => e.setting).Where(s => s != null).ToList();
        }
    }
}
