using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.ObjectPool;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Runtime
{
    public partial class SpriteCollectionComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 散图集合对象池
        /// </summary>
        private IObjectPool<SpriteCollectionItemObject> m_SpriteCollectionPool;

        /// <summary>
        /// 检查是否可以释放间隔
        /// </summary>
        [SerializeField] private float m_CheckCanReleaseInterval = 30f;
        
        /// <summary>
        /// 对象池自动释放时间间隔
        /// </summary>
        [SerializeField] private float m_AutoReleaseInterval = 60f;
        
        private LinkedList<LoadSpriteObject> m_LoadSpriteObjectsLinkedList;
        private HashSet<string> m_SpriteCollectionBeingLoaded;
        private Dictionary<string, LinkedList<ISetSpriteObject>> m_WaitSetObjects;
        private float m_CheckCanReleaseTime = 0.0f;

        
        public override void AwakeComponent()
        {
            base.AwakeComponent();
            ObjectPoolComponent objectPoolComponent = LFrameworkAspect.Instance.Get<ObjectPoolComponent>();
            m_SpriteCollectionPool = objectPoolComponent.CreateMultiSpawnObjectPool<SpriteCollectionItemObject>(
                "SpriteCollection",
                m_AutoReleaseInterval, 16, 60, 0);
            m_LoadSpriteObjectsLinkedList = new LinkedList<LoadSpriteObject>();
            m_SpriteCollectionBeingLoaded = new HashSet<string>();
            m_WaitSetObjects = new Dictionary<string, LinkedList<ISetSpriteObject>>();
            InitializedResources();
        }

        public override void UpdateComponent(float elapseSeconds, float realElapseSeconds)
        {
            base.UpdateComponent(elapseSeconds, realElapseSeconds);
            m_CheckCanReleaseTime += Time.unscaledDeltaTime;
            if (m_CheckCanReleaseTime < (double)m_CheckCanReleaseInterval)
                return;
            ReleaseUnused();
        }


        private void ReleaseUnused()
        {
            LinkedListNode<LoadSpriteObject> current = m_LoadSpriteObjectsLinkedList.First;
            while (current != null)
            {
                var next = current.Next;
                if (current.Value.SpriteObject.IsCanRelease())
                {
                    m_SpriteCollectionPool.Unspawn(current.Value.Collection);
                    ReferencePool.Release(current.Value.SpriteObject);
                    m_LoadSpriteObjectsLinkedList.Remove(current);
                }

                current = next;
            }

            m_CheckCanReleaseTime = 0;
        }
    }
}