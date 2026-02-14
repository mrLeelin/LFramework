using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Event;
using UnityEngine;

namespace LFramework.Runtime
{
    public class LoadSpriteCollectionCompleted : GameEventArgs
    {
        
        
        
        public static readonly int EventID = typeof(LoadSpriteCollectionCompleted).GetHashCode();

        public static LoadSpriteCollectionCompleted Create(string collectionPath)
        {
            var args = ReferencePool.Acquire<LoadSpriteCollectionCompleted>();
            args.CollectionPath = collectionPath;

            return args;

        }

        public string CollectionPath;

        public override void Clear()
        {
            CollectionPath = null;
        }

        public override int Id => EventID;
    }
}