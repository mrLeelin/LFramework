using System.Collections;
using NUnit.Framework;
using LFramework.Runtime;
using UnityEngine;

namespace LFramework.Editor.Tests.SequenceLine
{
    public sealed class SequenceLineChunkGroupTests
    {
        [SetUp]
        public void SetUp()
        {
            SingletonManager.AddSingleton(new LFrameworkAspect());
        }

        [TearDown]
        public void TearDown()
        {
            SingletonManager.Close();
        }

        [Test]
        public void ComponentInsertBeforeTarget_ReturnsNewSerialIDWithoutSkippingFailedInsert()
        {
            var component = new LFramework.Runtime.SequenceLineComponent();
            var parent = new GameObject("SequenceLineComponentTest");
            component.Parent = parent;
            component.SetUpComponent();

            var firstID = component.Insert(new TestSequenceLineChunkSetting());
            var failedID = component.Insert(new TestSequenceLineChunkSetting(), 999, SequenceLineInsertPosition.After);
            var insertedID = component.Insert(new TestSequenceLineChunkSetting(), firstID, SequenceLineInsertPosition.After);

            Assert.That(firstID, Is.EqualTo(1));
            Assert.That(failedID, Is.EqualTo(0));
            Assert.That(insertedID, Is.EqualTo(2));

            component.ShutDown();
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void ComponentInsertIntoExplicitGroup_AssignsSerialID()
        {
            var component = new LFramework.Runtime.SequenceLineComponent();
            var parent = new GameObject("SequenceLineComponentTest");
            component.Parent = parent;
            component.SetUpComponent();
            var group = new TestSequenceLineChunkGroup();

            var serialID = component.Insert(group, new TestSequenceLineChunkSetting());

            Assert.That(serialID, Is.EqualTo(1));
            Assert.That(group.Chunks.First.Value.SerialID, Is.EqualTo(serialID));

            component.ShutDown();
            Object.DestroyImmediate(parent);
        }

        [Test]
        public void InsertAfterTargetSerialID_AddsChunkBehindTarget()
        {
            var group = new TestSequenceLineChunkGroup();
            var first = CreateChunk(1);
            var second = CreateChunk(2);
            var inserted = CreateChunk(3);

            group.Insert(first);
            group.Insert(second);

            var result = group.Insert(1, inserted, SequenceLineInsertPosition.After);

            Assert.That(result, Is.True);
            Assert.That(group.Chunks, Is.EqualTo(new[] { first, inserted, second }));
        }

        [Test]
        public void InsertBeforeTargetSerialID_AddsChunkAheadOfTarget()
        {
            var group = new TestSequenceLineChunkGroup();
            var first = CreateChunk(1);
            var second = CreateChunk(2);
            var inserted = CreateChunk(3);

            group.Insert(first);
            group.Insert(second);

            var result = group.Insert(2, inserted, SequenceLineInsertPosition.Before);

            Assert.That(result, Is.True);
            Assert.That(group.Chunks, Is.EqualTo(new[] { first, inserted, second }));
        }

        [Test]
        public void InsertBeforeRunningChunk_ReturnsFalse()
        {
            var group = new TestSequenceLineChunkGroup();
            var running = CreateChunk(1);
            var inserted = CreateChunk(2);
            group.Insert(running);
            group.SetRunningChunk(running);

            var result = group.Insert(1, inserted, SequenceLineInsertPosition.Before);

            Assert.That(result, Is.False);
            Assert.That(group.Chunks, Is.EqualTo(new[] { running }));
        }

        [Test]
        public void RemoveBeforeAnyChunkRuns_RemovesTarget()
        {
            var group = new TestSequenceLineChunkGroup();
            var first = CreateChunk(1);
            var second = CreateChunk(2);
            group.Insert(first);
            group.Insert(second);

            var result = group.Remove(1);

            Assert.That(result, Is.True);
            Assert.That(group.Chunks, Is.EqualTo(new[] { second }));
        }

        private static TestChunk CreateChunk(int serialID)
        {
            return new TestChunk(serialID);
        }

        private sealed class TestSequenceLineChunkGroup : SequenceLineChunkGroup
        {
            public TestSequenceLineChunkGroup() : base("test", null)
            {
            }

            public void SetRunningChunk(ISequenceLineChunk chunk)
            {
                _curRunningChunk = chunk;
            }
        }

        private sealed class TestChunk : ISequenceLineChunk
        {
            private readonly ISequenceLineChunkSetting _setting;

            public TestChunk(int serialID)
            {
                SerialID = serialID;
                _setting = new TestChunkSetting();
            }

            public int SerialID { get; set; }
            public SequenceLineChunkSortInGroup Sort => SequenceLineChunkSortInGroup.None;
            public bool CreateNewGroup => false;
            public ISequenceLineChunkGroup Group { get; private set; }
            public ISequenceLineChunkSetting Current => _setting;
            object IEnumerator.Current => Current;

            public void SetSetting(ISequenceLineChunkSetting setting)
            {
            }

            public void Init(ISequenceLineChunkGroup chunkGroup)
            {
                Group = chunkGroup;
            }

            public bool MoveNext()
            {
                return false;
            }

            public void Reset()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class TestChunkSetting : SequenceLineChunkSetting<TestChunk>
        {
            public TestChunkSetting() : base("test")
            {
            }
        }

        private sealed class TestSequenceLineChunkSetting : SequenceLineChunkSetting<TestSequenceLineChunk>
        {
            public TestSequenceLineChunkSetting() : base("test")
            {
            }
        }

        private sealed class TestSequenceLineChunk : SequenceLineChunk<TestSequenceLineChunkSetting>
        {
            public override bool MoveNext()
            {
                return false;
            }
        }
    }
}
