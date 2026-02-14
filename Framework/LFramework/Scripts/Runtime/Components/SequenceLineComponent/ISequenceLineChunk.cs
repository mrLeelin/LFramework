
using System.Collections.Generic;

namespace LFramework.Runtime
{
    public interface ISequenceLineChunk :  IEnumerator<ISequenceLineChunkSetting>
    {
        int SerialID { get; set; }
        SequenceLineChunkSortInGroup Sort { get; }
        bool CreateNewGroup { get; }
        ISequenceLineChunkGroup Group { get; }
        void SetSetting(ISequenceLineChunkSetting setting);
        void Init(ISequenceLineChunkGroup chunkGroup);
    }
}
