

using System;
using System.Collections.Generic;

namespace LFramework.Runtime
{
    public interface ISequenceLineChunkGroup :  IEnumerator<ISequenceLineChunkSetting>
    {

        
        event EventHandler<InsertChunkArgs> InsertChunkInGroup;
        
        /// <summary>
        /// 暂停一个组
        /// </summary>
        bool Pause { get; set; }
        
        int Count { get; }
        bool HasChunk { get; }
        /// <summary>
        /// 不能被删除
        /// </summary>
        bool NotBeDel { get; }
        /// <summary>
        /// 是否正在运行
        /// </summary>
        bool IsPlaying { get; }

        LinkedList<ISequenceLineChunk> Chunks { get; }

        string GroupType { get; }

        void Insert(ISequenceLineChunk chunk);
        bool Remove(int serialID);
        void Insert(IEnumerable<ISequenceLineChunk> chunks);

        void SetGroupHandle(SequenceLineChunkGroupHandle handle);
    }
}
