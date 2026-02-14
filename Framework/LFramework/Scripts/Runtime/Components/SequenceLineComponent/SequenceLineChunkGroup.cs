using System;
using System.Collections;
using System.Collections.Generic;
using ModestTree;
using UniRx;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    public class SequenceLineChunkGroup : ISequenceLineChunkGroup
    {
#if UNITY_EDITOR

        public GameObject Root { get; private set; }
#endif

        private readonly SequenceLineComponent _sequenceLineComponent;
        protected ISequenceLineChunk _curRunningChunk;
        private bool _isAllChunkCompleted;
        private EventHandler<InsertChunkArgs> _insertChunkInGroup;
        private Coroutine _coroutine;
        public bool IsPlaying { get; private set; } = false;
        public LinkedList<ISequenceLineChunk> Chunks { get; } = new LinkedList<ISequenceLineChunk>();

        public string GroupType { get; }


        public event EventHandler<InsertChunkArgs> InsertChunkInGroup
        {
            add => _insertChunkInGroup += value;
            remove => _insertChunkInGroup -= value;
        }

        public bool Pause { get; set; }

        public int Count => Chunks.Count;
        public SequenceLineChunkGroupHandle GroupHandle { get; set; }
        public bool HasChunk => Count > 0;

        public bool NotBeDel => Current?.NotBeDel ?? false;
        public ISequenceLineChunkSetting Current => _curRunningChunk?.Current ?? null;

        object IEnumerator.Current => Current;

        public SequenceLineChunkGroup(string groupType, SequenceLineComponent sequenceLineComponent, ISequenceLineChunk cur = null)
        {
            GroupType = groupType;
            _sequenceLineComponent = sequenceLineComponent;
            _curRunningChunk = cur;
        }


        public void Insert(ISequenceLineChunk chunk)
        {
            if (chunk == null)
            {
                Log.Error("you insert Chunk is none");
                return;
            }

            chunk.Init(this);
            Chunks.AddLast(chunk);
            var arg = new InsertChunkArgs { Setting = chunk.Current };
            _insertChunkInGroup?.Invoke(this, arg);
        }

        public bool Remove(int serialID)
        {
            if (serialID <= 0)
            {
                return false;
            }

            var first = Chunks.First;
            while (first != null)
            {
                if (first.Value.SerialID == serialID)
                {
                    if (serialID == _curRunningChunk.SerialID)
                    {
                        return false;
                    }

                    if (serialID < _curRunningChunk.SerialID)
                    {
                        return false;
                    }

                    Chunks.Remove(first.Value);
                    return true;
                }

                first = first.Next;
            }

            return false;
        }

        public void Insert(IEnumerable<ISequenceLineChunk> chunks)
        {
            foreach (var chunk in chunks)
            {
                Insert(chunk);
            }
        }

        public void SetGroupHandle(SequenceLineChunkGroupHandle handle)
        {
            GroupHandle = handle;
        }

        public void Reset()
        {
#if UNITY_EDITOR
            Root = new GameObject($"[Type:{GroupType.ToString()}]");
            Root.transform.SetParent(GroupHandle.Root.transform);
#endif
            _curRunningChunk = null;
            _coroutine = MainThreadDispatcher.StartCoroutine(Coroutine());
            IsPlaying = true;
        }

        protected virtual IEnumerator Coroutine()
        {
            if (Chunks.Count <= 0)
            {
                _isAllChunkCompleted = true;
                yield break;
            }

            var first = Chunks.First;
            do
            {
                if (Pause)
                {
                    yield return null;
                    continue;
                }

                _curRunningChunk = first.Value;
                first.Value.Reset();
                yield return first.Value;
                first.Value.Dispose();
                if (Chunks.Count > 0)
                {
                    Chunks.RemoveFirst();
                }

                first = Chunks.First;
            } while (first != null);

            _isAllChunkCompleted = true;
        }


        public bool MoveNext() => !_isAllChunkCompleted;


        public void Dispose()
        {
            foreach (var chunk in Chunks)
            {
                chunk.Dispose();
            }

            Chunks.Clear();
#if UNITY_EDITOR
            if (Root)
            {
                Object.Destroy(Root);
            }
#endif
            if (_coroutine != null)
            {
                MainThreadDispatcher.StopCoroutine(_coroutine);
            }

            IsPlaying = false;
        }
    }
}
 