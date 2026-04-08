using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LFramework.Runtime.Settings
{
    /// <summary>
    /// 工程侧 Setting 同步状态
    /// </summary>
    [CreateAssetMenu(fileName = "SettingSyncState", menuName = "LFramework/Settings/SettingSyncState")]
    public class SettingSyncState : ScriptableObject
    {
        [SerializeField] private List<SettingSyncRecord> records = new List<SettingSyncRecord>();

        /// <summary>
        /// 全部同步记录
        /// </summary>
        public IReadOnlyList<SettingSyncRecord> Records => records;

        /// <summary>
        /// 根据稳定标识获取记录
        /// </summary>
        public SettingSyncRecord GetRecord(string settingId)
        {
            return records.FirstOrDefault(record => record != null && record.settingId == settingId);
        }

        /// <summary>
        /// 新增或覆盖同步记录
        /// </summary>
        public void UpsertRecord(SettingSyncRecord record)
        {
            if (record == null || string.IsNullOrWhiteSpace(record.settingId))
            {
                return;
            }

            var existing = GetRecord(record.settingId);
            if (existing != null)
            {
                existing.settingTypeName = record.settingTypeName;
                existing.localAsset = record.localAsset;
                existing.lastTemplateVersion = record.lastTemplateVersion;
                existing.lastTemplateHash = record.lastTemplateHash;
                existing.lastSnapshotJson = record.lastSnapshotJson;
                existing.lastSyncTimeUtc = record.lastSyncTimeUtc;
                existing.migrationNotes = record.migrationNotes;
                return;
            }

            records.Add(record);
        }
    }
}
