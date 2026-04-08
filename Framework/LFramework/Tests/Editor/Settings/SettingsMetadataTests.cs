using NUnit.Framework;
using LFramework.Runtime;
using LFramework.Runtime.Settings;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class SettingsMetadataTests
    {
        [Test]
        public void SettingSyncState_UpsertsRecordsBySettingId()
        {
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var first = new SettingSyncRecord
            {
                settingId = "core.game",
                settingTypeName = typeof(GameSetting).FullName
            };
            var second = new SettingSyncRecord
            {
                settingId = "core.game",
                settingTypeName = typeof(GameSetting).AssemblyQualifiedName
            };

            syncState.UpsertRecord(first);
            syncState.UpsertRecord(second);

            Assert.That(syncState.Records, Has.Count.EqualTo(1));
            Assert.That(syncState.GetRecord("core.game").settingTypeName, Is.EqualTo(typeof(GameSetting).AssemblyQualifiedName));
        }

        [Test]
        public void SettingTemplateRegistry_CanResolveEntriesBySettingId()
        {
            var registry = ScriptableObject.CreateInstance<SettingTemplateRegistry>();
            var template = ScriptableObject.CreateInstance<GameSetting>();
            var entry = new SettingTemplateEntry
            {
                settingId = "core.game",
                templateVersion = 1,
                templateAsset = template
            };

            registry.SetEntries(new[] { entry });

            Assert.That(registry.GetEntry("core.game"), Is.Not.Null);
            Assert.That(registry.GetEntry("core.game").templateAsset, Is.SameAs(template));
            Assert.That(registry.GetRequiredEntries(), Has.Count.EqualTo(1));
        }
    }
}
