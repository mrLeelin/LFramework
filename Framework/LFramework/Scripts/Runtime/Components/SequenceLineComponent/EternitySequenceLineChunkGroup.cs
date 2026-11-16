using System.Collections;

namespace LFramework.Runtime
{
    public sealed class EternitySequenceLineChunkGroup : SequenceLineChunkGroup
    {
        public EternitySequenceLineChunkGroup(string groupType, SequenceLineComponent sequenceLineComponent) : base(groupType, sequenceLineComponent)
        {
        }

        protected override IEnumerator Coroutine()
        {
            var execution = RunChunks(true);
            while (execution.MoveNext())
            {
                yield return execution.Current;
            }
        }
    }
}

