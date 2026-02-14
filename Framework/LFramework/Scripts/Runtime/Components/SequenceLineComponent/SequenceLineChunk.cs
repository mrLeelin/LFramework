 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public abstract class SequenceLineChunk<T> : ISequenceLineChunk
        where T : ISequenceLineChunkSetting

    {
#if UNITY_EDITOR
        private GameObject Root { get; set; }
#endif
        
     
        protected T Setting { get; private set; }
        public int SerialID { get; set; }
        public SequenceLineChunkSortInGroup Sort => Setting.Sort;
        public bool CreateNewGroup => Setting.CreateNewGroup;
        public ISequenceLineChunkGroup Group { get; private set; }


        public ISequenceLineChunkSetting Current => Setting;

        object IEnumerator.Current => Current;


        public void SetSetting(ISequenceLineChunkSetting setting)
        {
            Setting = (T) setting;
            if (Setting == null)
            {
                Log.Error("you Setting format is error");
            }
        }

        public void Init(ISequenceLineChunkGroup chunkGroup)
        {
            Group = chunkGroup;
        }


        public virtual void Reset()
        {
       
#if UNITY_EDITOR
            var str = Setting.Tag;
            if (string.IsNullOrEmpty(str))
            {
                str = GetType().Name;
            }
            Root = new GameObject($"[{str}]");
            Root.transform.SetParent(((SequenceLineChunkGroup) Group).Root.transform);
#endif
        }

        public abstract bool MoveNext();




        public virtual void Dispose()
        {
#if UNITY_EDITOR
            if (Root)
            {
                Object.Destroy(Root);
            }
#endif
        }
        
    }
}
