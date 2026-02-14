using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GameFramework;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    public sealed class SequenceLineComponent : GameFrameworkComponent
    {
        //private readonly LinkedList<ISequenceLineChunkGroup> _groups = new LinkedList<ISequenceLineChunkGroup>();

        private readonly Dictionary<string, SequenceLineChunkGroupHandle> _multiGroups =
            new();


        private readonly IDictionary<string, IList<ISequenceLineChunk>> _chunksBuffer
            = new Dictionary<string, IList<ISequenceLineChunk>>();

     
        private int _serialID;

        public int Count
        {
            get
            {
                var count = 0;
                foreach (var @group in _multiGroups)
                {
                    count += group.Value.Count;
                }

                return count;
            }
        }


        public bool HasRun => Count > 0;

#if UNITY_EDITOR
        public Transform Root => Instance;
#endif
        public override void SetUpComponent()
        {
            base.SetUpComponent();
            

#if UNITY_EDITOR
            CreateInstance("SequenceLine Root");
#endif
            _serialID = 0;
        }


        public override void ShutDown()
        {
            foreach (var g in _multiGroups)
            {
                ReferencePool.Release(g.Value);
            }

            _chunksBuffer.Clear();
            _multiGroups.Clear();
        }

        public int Insert(ISequenceLineChunkSetting setting) =>
            Insert(setting.GroupType, SettingToChunk(setting));


        public void Insert(ISequenceLineChunkGroup @group, ISequenceLineChunkSetting setting) =>
            group.Insert(SettingToChunk(setting));


        public void UseBufferWithGroupType(string groupType)
        {
            if (!_chunksBuffer.TryGetValue(groupType, out var chunks))
            {
                return;
            }

            //SortChunksFromDataTable(groupType, ref chunks);
            if (TryGetLastGroupFromGroups(groupType, out var @group))
            {
                group.Insert(chunks);
            }
            else
            {
                var chunk = chunks != null && chunks.Count > 0 ? chunks[0] : null;
                group = InternalDefaultGroup(groupType, false, chunk);
                group.Insert(chunks);
                AddGroupToHandle(group);
            }

            _chunksBuffer.Remove(groupType);
        }

        public void ClearAll()
        {
            foreach (var g in _multiGroups)
            {
                ReferencePool.Release(g.Value);
            }

            _multiGroups.Clear();
            _chunksBuffer.Clear();
        }

        public void ClearAllUnUsed()
        {
            var list = new List<ISequenceLineChunkGroup>();
            foreach (var g in _multiGroups)
            {
                foreach (var group in g.Value.Foreach())
                {
                    if (group.NotBeDel)
                    {
                        continue;
                    }

                    list.Add(group);
                }
            }

            if (list.Count <= 0)
            {
                return;
            }

            foreach (var l in list)
            {
                RemoveGroupFromHandle(l);
            }
        }
        
        public ISequenceLineChunkGroup DefaultGroup(string type)
        {
            var result = InternalDefaultGroup(type, true);
            result.Reset();
            return result;
        }


        public void RecycleGroup(ISequenceLineChunkGroup @group) => RemoveGroupFromHandle(group);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="chunk"></param>
        /// <param name="forceInsert"></param>
        /// 必须加入队列
        private int Insert(string type, ISequenceLineChunk chunk)
        {
            if (type == null)
            {
                Log.Error("you insert type is UnKnow.");
                return 0;
            }

            if (chunk == null)
            {
                Log.Error("you insert Chunk is none.");
                return 0;
            }

            chunk.SerialID = ++_serialID;

            if (chunk.CreateNewGroup)
            {
                var defaultGroup = InternalDefaultGroup(type, false);
                defaultGroup.Insert(chunk);
                AddGroupToHandle(defaultGroup);
                return chunk.SerialID;
            }

            if (!TryGetLastGroupFromGroups(type, out var @group))
            {
                var defaultGroup = InternalDefaultGroup(type, false);
                defaultGroup.Insert(chunk);
                AddGroupToHandle(defaultGroup);
                return chunk.SerialID;
            }


            group.Insert(chunk);


            //Level Group Add to Buffer
            if (!_chunksBuffer.TryGetValue(type, out var chunks))
            {
                chunks = new List<ISequenceLineChunk>();
            }

            chunks.Add(chunk);
            _chunksBuffer[type] = chunks;
            return chunk.SerialID;
        }


        private ISequenceLineChunk SettingToChunk(ISequenceLineChunkSetting setting)
        {
            if (setting == null)
            {
                Log.Error("you Setting is null");
                return null;
            }

            if (setting.GroupType == null)
            {
                Log.Error("you Group is UnKnow.");
                return null;
            }

            if (setting.ActionType == null)
            {
                Log.Error("you Setting Action is null.");
                return null;
            }

            if (!typeof(ISequenceLineChunk).IsAssignableFrom(setting.ActionType))
            {
                Log.Error("you Setting Action is Not Chunk.");
                return null;
            }

            if (!(Activator.CreateInstance(setting.ActionType) is ISequenceLineChunk chunkBase))
            {
                Log.Error("you ChunkBase  is  null.");
                return null;
            }

            LFrameworkAspect.Instance.DiContainer.Inject(chunkBase);
            chunkBase.SetSetting(setting);
            return chunkBase;
        }

        /*
        private void SortChunksFromDataTable(SequenceLineChunkGroupType groupType, ref IList<ISequenceLineChunk> chunks)
        {
            if (chunks.Count <= 0)
            {
                return;
            }

            var copy = chunks.ToList();
            chunks.Clear();
            var lineDates = DataTableService.GetAllDatas<ISequenceLineData>(data => groupType == data.Group);

            foreach (var data in lineDates)
            {
                for (var i = 0; i < copy.Count;)
                {
                    if (copy[i].Sort != data.Sort)
                    {
                        i++;
                        continue;
                    }

                    chunks.Add(copy[i]);
                    copy.RemoveAt(i);
                }
            }

            if (copy.Count > 0)
            {
                foreach (var chunk in copy)
                {
                    if (chunk.Sort != SequenceLineChunkSortInGroup.None)
                    {
                        continue;
                    }

                    chunks.Add(chunk);
                }
            }

            copy.Clear();
            copy = null;
        }
        */


        private ISequenceLineChunkGroup InternalDefaultGroup(string type, bool isEternity,
            ISequenceLineChunk chunk = null)
        {
            ISequenceLineChunkGroup @group = null;
            if (!isEternity)
            {
                group = new SequenceLineChunkGroup(type, this, chunk);
            }
            else
            {
                group = new EternitySequenceLineChunkGroup(type, this);
            }

            group.InsertChunkInGroup += OnAddNewChunkInGroup;
            return group;
        }

        private void DisposeGroup(ISequenceLineChunkGroup @group)
        {
            group.InsertChunkInGroup -= OnAddNewChunkInGroup;
            group.Dispose();
        }

        private bool TryGetLastGroupFromGroups(string type,
            out ISequenceLineChunkGroup @group)
        {
            group = null;
            if (_multiGroups.Count <= 0)
            {
                return false;
            }

            if (!_multiGroups.TryGetValue(type, out var groups))
            {
                return false;
            }

            var g = groups.Groups.Last;
            while (g != null)
            {
                if (g.Value.GroupType == type)
                {
                    break;
                }

                g = g.Previous;
            }

            if (g != null)
            {
                group = g.Value;
            }

            return group != null;
        }

        private void OnAddNewChunkInGroup(object sender, InsertChunkArgs insertChunkArgs)
        {
        }

        private void AddGroupToHandle(ISequenceLineChunkGroup @group)
        {
            if (@group == null)
            {
                Log.Error("you add group is null.");
                return;
            }

            if (!_multiGroups.TryGetValue(@group.GroupType, out var groups))
            {
                groups = ReferencePool.Acquire<SequenceLineChunkGroupHandle>();
                groups.Init(@group.GroupType, RemoveHandle);
#if UNITY_EDITOR
                groups.SetRoot(Root);
#endif
                _multiGroups.Add(@group.GroupType, groups);
            }

            group.SetGroupHandle(groups);

            groups.Groups.AddLast(@group);
        }

        private void RemoveHandle(string groupType)
        {
            if (!_multiGroups.TryGetValue(groupType, out var groups))
            {
                return;
            }

            ReferencePool.Release(groups);
            _multiGroups.Remove(groupType);
        }

        private void RemoveGroupFromHandle(ISequenceLineChunkGroup @group)
        {
            if (@group == null)
            {
                Log.Error("you remove group is null.");
                return;
            }

            if (!_multiGroups.TryGetValue(@group.GroupType, out var groups))
            {
                return;
            }

            DisposeGroup(group);
            groups.Groups.Remove(@group);
            if (groups.Groups.Count > 0)
            {
                return;
            }

            if (!_multiGroups.Remove(group.GroupType))
            {
                Log.Fatal("Remove Group From Handle Error.");
                return;
            }

            ReferencePool.Release(groups);
        }
    }
}