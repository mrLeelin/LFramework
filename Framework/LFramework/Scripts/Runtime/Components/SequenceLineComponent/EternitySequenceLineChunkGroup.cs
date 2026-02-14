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
            while (true)
            {
                if (Chunks.Count <= 0)
                {
                    yield return null;
                    continue;
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
            }
            // ReSharper disable once IteratorNeverReturns
        }
    }
}

