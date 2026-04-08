using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFramework.Editor.Settings
{
    public enum SettingSyncStatus
    {
        UpToDate = 0,
        New = 1,
        Updatable = 2,
        Missing = 3,
        Deprecated = 4,
        ManualReview = 5
    }

    public enum SettingFieldChangeAction
    {
        None = 0,
        UpdatedFromTemplate = 1,
        PreservedLocalOverride = 2,
        ManualReview = 3
    }

    public sealed class SettingFieldChange
    {
        public string fieldName;
        public SettingFieldChangeAction action;
    }

    public sealed class SettingSyncItemReport
    {
        public string settingId;
        public string settingTypeName;
        public SettingSyncStatus status;
        public ScriptableObject templateAsset;
        public ScriptableObject localAsset;
        public List<SettingFieldChange> fieldChanges = new();
    }

    public sealed class SettingSyncReport
    {
        public List<SettingSyncItemReport> Items { get; } = new();

        public SettingSyncItemReport GetItem(string settingId)
        {
            return Items.FirstOrDefault(item => item.settingId == settingId);
        }
    }
}
