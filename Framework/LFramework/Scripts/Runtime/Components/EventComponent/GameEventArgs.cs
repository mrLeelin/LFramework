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
        
        public static int EventID => typeof(T).GetHashCode();
        
        
        
        public override int Id => EventID;
    }

}
