using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UniRx;
using UnityEngine;
using UnityGameFramework.Runtime;
using Object = UnityEngine.Object;

namespace LFramework.Runtime
{
    public class SequenceLineChunkGroupHandle : IReference
    {
        private Action<string> _releaseGroupAc;
        private Coroutine _coroutine;

        public string GroupType { get; private set; }
        public LinkedList<ISequenceLineChunkGroup> Groups { get; private set; }

        public int Count
        {
            get => Groups.Count;
        }

        public GameObject Root { get; private set; }

        

        public void Init(string groupGroupType,
            Action<string> releaseGroupAc)
        {
            Groups = new LinkedList<ISequenceLineChunkGroup>();
            GroupType = groupGroupType;
            _releaseGroupAc = releaseGroupAc;
            _coroutine = MainThreadDispatcher.StartCoroutine(MoveNext());

        }

        public void SetRoot(Transform root)
        {
#if UNITY_EDITOR
            Root = new GameObject($"[{GroupType}]");
            Root.transform.SetParent(root);
#endif
        }

        public void Clear()
        {
            foreach (var g in Foreach())
            {
                g.Dispose();
            }
            
            Groups.Clear();
            Groups = null;
            if (_coroutine != null)
            {
                MainThreadDispatcher.StopCoroutine(_coroutine);
            }
#if UNITY_EDITOR
            Object.Destroy(Root.gameObject);
            Root = null;
#endif
        }

        public IEnumerable<ISequenceLineChunkGroup> Foreach()
        {
            var first = Groups.First;
            while (first != null)
            {
                yield return first.Value;
                first = first.Next;
            }
        }


        private IEnumerator MoveNext()
        {
            while (true)
            {
                if (Groups.Count <= 0)
                {
                    yield return null;
                    continue;
                }

                var first = Groups.First;
                while (first != null)
                {
                    IEnumerator moveNext = null;
                    try
                    {
                        first.Value.Reset();
                        moveNext = first.Value;
                    }
                    catch (Exception e)
                    {
                        Log.Error(
                            $"[SequenceLine Error]: Group:{first.Value.GroupType}  Chunk: {first.Value.Current.GetType().Name}" +
                            $"Extension :{e.Message}");
                    }

                    yield return moveNext;
                    //_releaseGroupAc(first.Value);
                    first.Value.Dispose();
                    Groups.RemoveFirst();
                    first = Groups.First;
                }
                // Null
                _releaseGroupAc(GroupType);
                break;
            }
        }
    }
}