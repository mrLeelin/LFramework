using System;
using System.Collections.Generic;
using NUnit.Framework;
using LFramework.Editor.Settings;
using LFramework.Runtime.Settings;
using UnityEditor;
using UnityEngine;

namespace LFramework.Editor.Tests.Settings
{
    public class SettingSyncServiceTests
    {
        private const string TempRoot = "Assets/__TempSettingSyncServiceTests";

        [SetUp]
        public void SetUp()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }

            AssetDatabase.CreateFolder("Assets", "__TempSettingSyncServiceTests");
            AssetDatabase.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(TempRoot))
            {
                AssetDatabase.DeleteAsset(TempRoot);
            }

            AssetDatabase.Refresh();
        }

        [Test]
        public void Merge_UpdatesUnchangedLocalFieldFromTemplate()
        {
            var baseOld = ScriptableObject.CreateInstance<TestMergeSetting>();
            var baseNew = ScriptableObject.CreateInstance<TestMergeSetting>();
            var local = ScriptableObject.CreateInstance<TestMergeSetting>();

            baseOld.number = 1;
            baseNew.number = 2;
            local.number = 1;

            SettingMergeResult result = SettingMergeUtility.Merge(baseOld, baseNew, local);

            Assert.That(result.RequiresManualReview, Is.False);
            Assert.That(local.number, Is.EqualTo(2));
            Assert.That(result.FieldChanges, Has.Count.EqualTo(1));
            Assert.That(result.FieldChanges[0].action, Is.EqualTo(SettingFieldChangeAction.UpdatedFromTemplate));
        }

        [Test]
        public void Merge_PreservesLocalOverrideWhenTemplateAlsoChanged()
        {
            var baseOld = ScriptableObject.CreateInstance<TestMergeSetting>();
            var baseNew = ScriptableObject.CreateInstance<TestMergeSetting>();
            var local = ScriptableObject.CreateInstance<TestMergeSetting>();

            baseOld.number = 1;
            baseNew.number = 2;
            local.number = 9;

            SettingMergeResult result = SettingMergeUtility.Merge(baseOld, baseNew, local);

            Assert.That(result.RequiresManualReview, Is.False);
            Assert.That(local.number, Is.EqualTo(9));
            Assert.That(result.FieldChanges[0].action, Is.EqualTo(SettingFieldChangeAction.PreservedLocalOverride));
        }

        [Test]
        public void Merge_MarksManualReviewForBindTypeNameConflicts()
        {
            var baseOld = ScriptableObject.CreateInstance<TestMergeComponentSetting>();
            var baseNew = ScriptableObject.CreateInstance<TestMergeComponentSetting>();
            var local = ScriptableObject.CreateInstance<TestMergeComponentSetting>();

            baseOld.bindTypeName = "Old.Component";
            baseNew.bindTypeName = "New.Component";
            local.bindTypeName = "Local.Component";

            SettingMergeResult result = SettingMergeUtility.Merge(baseOld, baseNew, local);

            Assert.That(result.RequiresManualReview, Is.True);
            Assert.That(local.bindTypeName, Is.EqualTo("Local.Component"));
            Assert.That(result.FieldChanges[0].action, Is.EqualTo(SettingFieldChangeAction.ManualReview));
        }

        [Test]
        public void AnalyzeTemplates_ReportsNewTemplateWhenProjectAssetIsMissing()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var template = ScriptableObject.CreateInstance<TestMergeSetting>();
            template.EditorSetSettingId("tests.merge");

            SettingSyncReport report = SettingSyncService.AnalyzeTemplates(selector, syncState, new ScriptableObject[] { template });

            Assert.That(report.Items, Has.Count.EqualTo(1));
            Assert.That(report.Items[0].status, Is.EqualTo(SettingSyncStatus.New));
        }

        [Test]
        public void AnalyzeTemplates_ReportsManualReviewWhenConflictExists()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var template = ScriptableObject.CreateInstance<TestMergeComponentSetting>();
            template.EditorSetSettingId("tests.component");
            template.bindTypeName = "Template.Component";

            var local = ScriptableObject.CreateInstance<TestMergeComponentSetting>();
            local.EditorSetSettingId("tests.component");
            local.bindTypeName = "Local.Component";
            selector.SetComponentSetting(local);

            var baseOld = ScriptableObject.CreateInstance<TestMergeComponentSetting>();
            baseOld.EditorSetSettingId("tests.component");
            baseOld.bindTypeName = "Old.Component";

            syncState.UpsertRecord(new SettingSyncRecord
            {
                settingId = "tests.component",
                settingTypeName = typeof(TestMergeComponentSetting).AssemblyQualifiedName,
                localAsset = local,
                lastSnapshotJson = EditorJsonUtility.ToJson(baseOld, true),
                lastTemplateHash = SettingSyncService.ComputeTemplateHash(template),
                lastTemplateVersion = 1
            });

            SettingSyncReport report = SettingSyncService.AnalyzeTemplates(selector, syncState, new ScriptableObject[] { template });

            Assert.That(report.Items, Has.Count.EqualTo(1));
            Assert.That(report.Items[0].status, Is.EqualTo(SettingSyncStatus.ManualReview));
        }

        [Test]
        public void SyncTemplates_CreatesNewProjectOwnedAssetAndRecord()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();
            var template = ScriptableObject.CreateInstance<TestMergeSetting>();
            template.EditorSetSettingId("tests.new");
            template.number = 7;
            AssetDatabase.CreateAsset(template, $"{TempRoot}/Template.asset");

            SettingSyncReport report = SettingSyncService.SyncTemplates(
                selector,
                syncState,
                new ScriptableObject[] { template },
                setting => $"{TempRoot}/Project/{setting.name}.asset");

            Assert.That(report.Items, Has.Count.EqualTo(1));
            Assert.That(report.Items[0].status, Is.EqualTo(SettingSyncStatus.New));
            Assert.That(selector.GetSetting("tests.new"), Is.Not.Null);
            Assert.That(syncState.GetRecord("tests.new"), Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(selector.GetSetting("tests.new")), Is.EqualTo($"{TempRoot}/Project/Template.asset"));
        }

        [Test]
        public void SyncTemplates_UpdatesExistingLocalAssetWhenSafe()
        {
            var selector = ScriptableObject.CreateInstance<ProjectSettingSelector>();
            var syncState = ScriptableObject.CreateInstance<SettingSyncState>();

            var template = ScriptableObject.CreateInstance<TestMergeSetting>();
            template.EditorSetSettingId("tests.update");
            template.number = 2;
            AssetDatabase.CreateAsset(template, $"{TempRoot}/TemplateUpdate.asset");

            var local = ScriptableObject.CreateInstance<TestMergeSetting>();
            local.EditorSetSettingId("tests.update");
            local.number = 1;
            AssetDatabase.CreateAsset(local, $"{TempRoot}/ProjectLocal.asset");
            selector.SetSetting(local);

            var baseOld = ScriptableObject.CreateInstance<TestMergeSetting>();
            baseOld.EditorSetSettingId("tests.update");
            baseOld.number = 1;
            syncState.UpsertRecord(new SettingSyncRecord
            {
                settingId = "tests.update",
                settingTypeName = typeof(TestMergeSetting).AssemblyQualifiedName,
                localAsset = local,
                lastSnapshotJson = EditorJsonUtility.ToJson(baseOld, true),
                lastTemplateHash = SettingSyncService.ComputeTemplateHash(baseOld),
                lastTemplateVersion = 1
            });

            SettingSyncReport report = SettingSyncService.SyncTemplates(
                selector,
                syncState,
                new ScriptableObject[] { template },
                setting => $"{TempRoot}/Project/{setting.name}.asset");

            Assert.That(report.Items, Has.Count.EqualTo(1));
            Assert.That(report.Items[0].status, Is.EqualTo(SettingSyncStatus.Updatable));
            Assert.That(local.number, Is.EqualTo(2));
            Assert.That(syncState.GetRecord("tests.update").localAsset, Is.SameAs(local));
        }

        private sealed class TestMergeSetting : BaseSetting
        {
            public int number;
            public string text;
            public List<int> numbers = new();
        }

        private sealed class TestMergeComponentSetting : ComponentSetting
        {
            public string helperTypeName;
        }
    }
}
