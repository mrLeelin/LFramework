using System;
using System.Collections;
using System.Collections.Generic;

namespace LFramework.Runtime
{
    public interface ISequenceLineChunkSetting : IEqualityComparer<ISequenceLineChunkSetting>
    {
        
        /// <summary>
        /// 类型排序
        /// </summary>
        SequenceLineChunkSortInGroup Sort { get; }
        /// <summary>
        /// 类型
        /// </summary>
        string GroupType { get; }
        Type ActionType { get; }
        bool CreateNewGroup { get; }
        string Tag { get;}

        /// <summary>
        /// bu可以被删除
        /// </summary>
        bool NotBeDel { get; }

    }
    
    public abstract class SequenceLineChunkSetting<T> : ISequenceLineChunkSetting
        where T : ISequenceLineChunk

    {
      
        public SequenceLineChunkSortInGroup Sort { get; set; } = SequenceLineChunkSortInGroup.None;
        public string GroupType { get; }
        public Type ActionType => typeof(T);

        /// <summary>
        /// 默认不创建新的组
        /// </summary>
        public bool CreateNewGroup { get; set; } = false;

        /// <summary>
        /// 不用判重 强制加入队列 目前只对 ShowUIFormChunk 起作用
        /// </summary>
        public bool ForceInsert { get; set; } = false;

        public string Tag { get; set; } = string.Empty;
        public bool NotBeDel { get; set; } = false;


        public SequenceLineChunkSetting(string type)
        {
            GroupType = type;
        }

        public virtual bool Equals(ISequenceLineChunkSetting x, ISequenceLineChunkSetting y)
        {
            if (x == null || y == null)
            {
                return false;
            }
            if (x.GetType() != y.GetType())
            {
                return false;
            }
            return true;
        }

        public virtual int GetHashCode(ISequenceLineChunkSetting obj)
        {
            return obj.GetHashCode();
        }
    }
}
