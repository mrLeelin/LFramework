using System;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 单个模板条目
    /// </summary>
    [Serializable]
    public class SettingTemplateEntry
    {
        public string settingId;
        public string settingTypeName;
        public ScriptableObject templateAsset;
        public int templateVersion = 1;
        public SettingTemplateMetadata category = SettingTemplateMetadata.Base;
        public bool isRequired = true;
        [TextArea] public string description;
    }
}
