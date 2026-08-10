using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Event;
using UnityEngine;

namespace LFramework.Runtime
{
    public abstract class GameEventArgs<T>
        : GameFramework.Event.GameEventArgs
        where T: GameEventArgs<T> , new()
    {

        public static T CreateEmpty()
        {
            return ReferencePool.Acquire<T>();
        }


        public static T Create(Action<T> callBack)
        {
            var result = CreateEmpty();
            callBack?.Invoke(result);
            return result;
        }
        
        private static readonly int CachedEventId = GetStableHashCode(typeof(T).FullName);

        public static int EventID => CachedEventId;
        
        
        
        public override int Id => EventID;

        private static int GetStableHashCode(string value)
        {
            unchecked
            {
                const uint offsetBasis = 2166136261;
                const uint prime = 16777619;
                uint hash = offsetBasis;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= prime;
                }

                return (int)hash;
            }
        }
    }

}
