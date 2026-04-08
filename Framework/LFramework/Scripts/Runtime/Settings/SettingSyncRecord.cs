using System;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 单个 Setting 的同步状态
    /// </summary>
    [Serializable]
    public class SettingSyncRecord
    {
        public string settingId;
        public string settingTypeName;
        public ScriptableObject localAsset;
        public int lastTemplateVersion;
        public string lastTemplateHash;
        [TextArea] public string lastSnapshotJson;
        public string lastSyncTimeUtc;
        [TextArea] public string migrationNotes;
    }
}
