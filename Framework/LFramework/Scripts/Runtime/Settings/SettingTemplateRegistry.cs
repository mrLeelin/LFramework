using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// Package 模板注册表
    /// </summary>
    [CreateAssetMenu(fileName = "SettingTemplateRegistry", menuName = "LFramework/Settings/SettingTemplateRegistry")]
    public class SettingTemplateRegistry : ScriptableObject
    {
        [SerializeField] private List<SettingTemplateEntry> entries = new List<SettingTemplateEntry>();

        /// <summary>
        /// 所有模板条目
        /// </summary>
        public IReadOnlyList<SettingTemplateEntry> Entries => entries;

        /// <summary>
        /// 设置注册表条目（主要用于编辑器工具或测试）
        /// </summary>
        public void SetEntries(IEnumerable<SettingTemplateEntry> newEntries)
        {
            entries = newEntries?.Where(entry => entry != null).ToList() ?? new List<SettingTemplateEntry>();
        }

        /// <summary>
        /// 根据稳定标识获取模板条目
        /// </summary>
        public SettingTemplateEntry GetEntry(string settingId)
        {
            return entries.FirstOrDefault(entry => entry != null && entry.settingId == settingId);
        }

        /// <summary>
        /// 获取必需模板
        /// </summary>
        public List<SettingTemplateEntry> GetRequiredEntries()
        {
            return entries.Where(entry => entry != null && entry.isRequired).ToList();
        }
    }
}
