using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.ObjectPool;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public class SpriteCollectionItemObject : ObjectBase 
    {
        private ResourceComponent m_ResourceComponent;

        public static SpriteCollectionItemObject Create(string collectionPath, SpriteCollection target,
            ResourceComponent resourceComponent)
        {
            SpriteCollectionItemObject item = ReferencePool.Acquire<SpriteCollectionItemObject>();
            item.Initialize(collectionPath, target);
            item.m_ResourceComponent = resourceComponent;
            return item;
        }

        protected override void OnUnspawn()
        {
            base.OnUnspawn();
        }

        protected override void Release(bool isShutdown)
        {
            SpriteCollection spriteCollection = (SpriteCollection)Target;
            if (spriteCollection == null)
            {
                return;
            }

            m_ResourceComponent.UnloadAsset(spriteCollection);
            m_ResourceComponent = null;
        }
    }
}