using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityGameFramework.Runtime;
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
        private bool _disposed;
        private ISequenceLineChunk _disposingChunk;
        private const int MaxChunksPerFrame = 64;
        public bool IsPlaying { get; private set; } = false;
        public bool SameFrameExecution { get; internal set; }
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

        public bool CanInsert(int targetSerialID, SequenceLineInsertPosition position)
        {
            if (targetSerialID <= 0)
            {
                return false;
            }

            var node = FindChunkNode(targetSerialID);
            if (node == null)
            {
                return false;
            }

            return position != SequenceLineInsertPosition.Before || !IsRunningChunk(node.Value);
        }

        public bool Insert(int targetSerialID, ISequenceLineChunk chunk, SequenceLineInsertPosition position)
        {
            if (chunk == null)
            {
                Log.Error("you insert Chunk is none");
                return false;
            }

            if (!CanInsert(targetSerialID, position))
            {
                return false;
            }

            var node = FindChunkNode(targetSerialID);
            chunk.Init(this);
            if (position == SequenceLineInsertPosition.Before)
            {
                Chunks.AddBefore(node, chunk);
            }
            else
            {
                Chunks.AddAfter(node, chunk);
            }

            var arg = new InsertChunkArgs { Setting = chunk.Current };
            _insertChunkInGroup?.Invoke(this, arg);
            return true;
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
                    if (IsRunningChunk(first.Value))
                    {
                        return false;
                    }

                    if (_curRunningChunk != null && serialID < _curRunningChunk.SerialID)
                    {
                        return false;
                    }

                    Chunks.Remove(first);
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
            _isAllChunkCompleted = false;
            _disposed = false;
            IsPlaying = true;
            _coroutine = MainThreadDispatcher.StartCoroutine(Coroutine());
        }

        protected virtual IEnumerator Coroutine()
        {
            var execution = RunChunks(false);
            // Drive the shared iterator directly: yielding it would add another scheduling boundary.
            while (execution.MoveNext())
            {
                yield return execution.Current;
            }

            _isAllChunkCompleted = true;
        }

        protected IEnumerator RunChunks(bool keepAlive)
        {
            var completedThisFrame = 0;
            while (!_disposed)
            {
                if (Chunks.Count == 0)
                {
                    if (!keepAlive) yield break;
                    completedThisFrame = 0;
                    yield return null;
                    continue;
                }

                if (Pause)
                {
                    completedThisFrame = 0;
                    yield return null;
                    continue;
                }

                var first = Chunks.First;
                var chunk = first.Value;
                _curRunningChunk = chunk;
                chunk.Reset();
                if (_disposed) yield break;

                if (SameFrameExecution)
                {
                    while (true)
                    {
                        var waiting = chunk.MoveNext();
                        if (_disposed) yield break;
                        if (!waiting) break;
                        completedThisFrame = 0;
                        yield return null;
                    }
                }
                else
                {
                    completedThisFrame = 0;
                    yield return chunk;
                }

                if (_disposed) yield break;
                var frameBoundary = chunk.Current is ISequenceLineChunkFrameBoundary boundary &&
                    boundary.WaitForNextFrameAfterCompletion;
                // Keep the node visible during Dispose, but do not dispose it twice if it clears the queue.
                _disposingChunk = chunk;
                try
                {
                    chunk.Dispose();
                }
                finally
                {
                    _disposingChunk = null;
                }
                if (_disposed) yield break;
                if (first.List != null) Chunks.Remove(first);

                completedThisFrame++;
                if (SameFrameExecution && (frameBoundary ||
                    (completedThisFrame >= MaxChunksPerFrame && Chunks.Count > 0)))
                {
                    completedThisFrame = 0;
                    yield return null;
                }
            }
        }

        private LinkedListNode<ISequenceLineChunk> FindChunkNode(int serialID)
        {
            var first = Chunks.First;
            while (first != null)
            {
                if (first.Value.SerialID == serialID)
                {
                    return first;
                }

                first = first.Next;
            }

            return null;
        }

        private bool IsRunningChunk(ISequenceLineChunk chunk)
        {
            return _curRunningChunk != null && _curRunningChunk.SerialID == chunk.SerialID;
        }


        public bool MoveNext() => !_isAllChunkCompleted;


        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _isAllChunkCompleted = true;
            IsPlaying = false;
            foreach (var chunk in Chunks)
            {
                if (!ReferenceEquals(chunk, _disposingChunk)) chunk.Dispose();
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
        }
    }
}
